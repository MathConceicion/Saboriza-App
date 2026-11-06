# Auth & Roles — Autenticação, Autorização e RBAC no ASP.NET Core 10

> **Stack:** ASP.NET Core 10 / JWT Bearer / Policy-Based + Resource-Based Authorization / BCrypt
> **Roles:** `Cliente` (default) → `Vendedor` (híbrido Cliente+Vendedor) → `Admin` (global)
> **Versão:** 1.0 — 2026-09-09

---

## 1. Modelo de Identidade e Papéis

### 1.1. Definição de Papéis (Enum + Tabela)

```csharp
// src/Domain/Enums/PapelUsuario.cs — linha 1
namespace GastronomiaMarketplace.Domain.Enums;

public enum PapelUsuario
{
    Cliente = 1,
    Vendedor = 2, // híbrido — ver §1.2
    Admin = 3
}
```

**Tabela `Usuarios` (fragmento relevante):**

| Coluna | Tipo Postgres | Notas |
|---|---|---|
| `id` | `uuid` PK | `gen_random_uuid()` |
| `email` | `varchar(254)` UNIQUE | normalizado lower |
| `senha_hash` | `varchar(255)` | BCrypt `$2a$` |
| `papel` | `smallint` | FK implícita para enum |
| `nome_completo` | `varchar(200)` | |
| `cpf` | `varchar(11)` UNIQUE | apenas dígitos |
| `criado_em` | `timestamptz` | UTC |

**Tabela `Vendedores` (1:1 com Usuario quando houver upgrade):**

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | `uuid` PK | também é o `VendorId` usado nas claims |
| `usuario_id` | `uuid` FK UNIQUE → `Usuarios.id` | `ON DELETE CASCADE` |
| `nome_loja` | `varchar(150)` | |
| `categoria_loja_id` | `uuid` FK → `CategoriasLoja` | |
| `status` | `smallint` | `PendenteAprovacao` / `Aprovado` / `Rejeitado` |
| `criado_em` | `timestamptz` | |

**Regra de negócio:** Todo `Usuario` nasce com `PapelUsuario.Cliente`. Ao fazer upgrade, cria-se linha em `Vendedores` e atualiza-se `Usuarios.papel = Vendedor`. O usuário **não perde** o papel de cliente — o enum `Vendedor` significa "Cliente com privilégios de vendedor". Admin é atribuído apenas via seed ou por outro Admin (endpoint `POST /api/admin/usuarios/{id}/promover-admin`).

### 1.2. Perfil Híbrido Vendedor/Cliente — Conceito

- **UX:** Após upgrade, o frontend exibe botão "Ir para o Painel do Vendedor" (chaveia contexto visual). Mas o **backend não tem "modo"** — toda requisição é autorizada pelas claims do token.
- **Token carrega ambos os contextos simultaneamente:** `role=Cliente` + `role=Vendedor` (quando aplicável) + `vendor_id` (quando aplicável). Assim, o mesmo token acessa `GET /api/lojas/proximas` (como cliente) e `GET /api/vendedor/pedidos` (como vendedor) sem re-login.
- **Isolamento:** Queries de cliente nunca filtram por `VendorId`. Queries de vendedor sempre filtram por `VendorId` (exceto Admin).

---

## 2. Mecanismo de Autenticação — JWT

### 2.1. Claims Obrigatórias do Token

Payload JWT (exemplo decodificado) para um **Cliente sem loja**:

```json
{
  "sub": "a3f1c2d4-e5f6-7890-abcd-ef1234567890",
  "email": "joao@exemplo.com",
  "name": "João Silva",
  "role": ["Cliente"],
  "jti": "d4e5f6a7-b8c9-0123-defa-bc2345678901",
  "iat": 1725888000,
  "exp": 1725891600,
  "iss": "gastronomia-marketplace",
  "aud": "gastronomia-marketplace-client"
}
```

Payload para **Vendedor (híbrido)** — note `vendor_id` e duplo `role`:

```json
{
  "sub": "a3f1c2d4-e5f6-7890-abcd-ef1234567890",
  "email": "maria@docesartesanais.com",
  "name": "Maria Doces",
  "role": ["Cliente", "Vendedor"],
  "vendor_id": "b2c3d4e5-f6a7-8901-bcde-f23456789012",
  "loja_id": "c3d4e5f6-a7b8-9012-cdef-345678901234",
  "loja_status": "Aprovado",
  "jti": "e5f6a7b8-c9d0-1234-efab-c34567890123",
  "iat": 1725888000,
  "exp": 1725891600,
  "iss": "gastronomia-marketplace",
  "aud": "gastronomia-marketplace-client"
}
```

Payload para **Admin**:

```json
{
  "sub": "f1a2b3c4-d5e6-7890-fabc-def12345678901",
  "email": "admin@gastronomia.local",
  "name": "Admin Global",
  "role": ["Cliente", "Admin"],
  "is_admin": "true",
  "jti": "a6b7c8d9-e0f1-2345-fabc-d45678901234",
  "iat": 1725888000,
  "exp": 1725891600
}
```

**Tabela de claims normativas:**

| Claim | Tipo | Obrigatório | Quando presente | Uso |
|---|---|---|---|---|
| `sub` | `uuid` str | Sim | Sempre | `ICurrentUser.UserId` |
| `email` | string | Sim | Sempre | exibição, auditoria |
| `role` | `string[]` | Sim | Sempre | `User.IsInRole()` + Policies |
| `vendor_id` | `uuid` str | Condicional | Se `Vendedores` existe e `status=Aprovado` | `ICurrentUser.VendorId` — filtra recursos do vendedor |
| `loja_id` | `uuid` str | Condicional | Se vendedor tem loja | atalho para queries |
| `loja_status` | string | Condicional | Se vendedor | frontend decide se mostra painel |
| `is_admin` | `"true"` | Condicional | Se Admin | atalho para bypass de ownership |
| `jti` | `uuid` str | Sim | Sempre | revogação futura (blacklist) |
| `exp`/`iat` | int (unix) | Sim | Sempre | expiração |

**Decisão de design:** `vendor_id` é emitido **apenas se `Vendedores.status = Aprovado`**. Se `PendenteAprovacao`, o usuário ainda tem `role=Vendedor` mas sem `vendor_id` — o painel mostra "Aguardando aprovação do Admin" e endpoints de vendedor retornam `403` até aprovação. Isso evita que vendedor crie produtos antes da moderação.

### 2.2. Geração do Token — `TokenService`

```csharp
// src/Infrastructure/Identity/TokenService.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "gastronomia-marketplace";
    public string Audience { get; set; } = "gastronomia-marketplace-client";
    public string SecretKey { get; set; } = null!; // 32+ chars, via User Secrets / KeyVault
    public int ExpiracaoMinutos { get; set; } = 60;
    public int RefreshExpiracaoDias { get; set; } = 7;
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opts;
    private readonly SymmetricSecurityKey _key;
    private readonly SigningCredentials _creds;

    public TokenService(IOptions<JwtOptions> opts)
    {
        _opts = opts.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SecretKey));
        _creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
    }

    public string GerarToken(UsuarioTokenDto usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, usuario.NomeCompleto),
        };

        // Roles — sempre ao menos Cliente (claim curta "role" para compatibilidade com frontend decode payload.role)
        claims.Add(new("role", "Cliente"));
        if (usuario.Papel == PapelUsuario.Vendedor)
            claims.Add(new("role", "Vendedor"));
        if (usuario.Papel == PapelUsuario.Admin || usuario.IsAdmin)
        {
            claims.Add(new("role", "Admin"));
            claims.Add(new("is_admin", "true"));
        }

        // VendorId condicional — apenas se aprovado
        if (usuario.VendorId.HasValue && usuario.LojaStatus == "Aprovado")
        {
            claims.Add(new("vendor_id", usuario.VendorId.Value.ToString()));
            if (usuario.LojaId.HasValue)
                claims.Add(new("loja_id", usuario.LojaId.Value.ToString()));
            claims.Add(new("loja_status", usuario.LojaStatus!));
        }

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opts.ExpiracaoMinutos),
            signingCredentials: _creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // DTO interno — montado pela Application após login/upgrade
    public sealed record UsuarioTokenDto(
        Guid UsuarioId,
        string Email,
        string NomeCompleto,
        PapelUsuario Papel,
        bool IsAdmin,
        Guid? VendorId,
        Guid? LojaId,
        string? LojaStatus
    );
}
```

### 2.2.1 Entrega via cookies HttpOnly (norma de segurança - substitui localStorage)

> **Proibição:** Frontend **nunca** armazena JWT em `localStorage`/`sessionStorage` (XSS exfiltra). Backend entrega tokens exclusivamente via cookies `HttpOnly`.

```csharp
// src/WebAPI/Controllers/AuthController.cs — trecho normativo após login
private void SetAuthCookies(string accessToken, string refreshToken)
{
    var isProd = !_env.IsDevelopment();
    Response.Cookies.Append("__Host-access_token", accessToken, new CookieOptions
    {
        HttpOnly = true, Secure = isProd, SameSite = SameSiteMode.Strict,
        Path = "/", MaxAge = TimeSpan.FromMinutes(_jwtOpts.ExpiracaoMinutos)
    });
    Response.Cookies.Append("__Host-refresh_token", refreshToken, new CookieOptions
    {
        HttpOnly = true, Secure = isProd, SameSite = SameSiteMode.Strict,
        Path = "/api/auth", MaxAge = TimeSpan.FromDays(_jwtOpts.RefreshExpiracaoDias)
    });
}

[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto) {
    // ... valida, gera tokens ...
    SetAuthCookies(accessToken, refreshToken);
    return Ok(new { user = new { id = usuario.Id, email = usuario.Email, nome = usuario.NomeCompleto, roles = roles, vendorId, lojaId, isAdmin } });
}

[HttpPost("refresh")]
public async Task<IActionResult> Refresh() {
    var refreshToken = Request.Cookies["__Host-refresh_token"];
    // ... valida, reemite access ...
    SetAuthCookies(novoAccess, refreshToken);
    return Ok(new { user });
}

[HttpPost("logout")]
public IActionResult Logout() {
    Response.Cookies.Delete("__Host-access_token");
    Response.Cookies.Delete("__Host-refresh_token", new CookieOptions{ Path="/api/auth"});
    return NoContent();
}

[HttpGet("me")]
[Authorize]
public async Task<IActionResult> Me() {
    // retorna user decodificado do ClaimsPrincipal (sem expor token)
    return Ok(new { id = _currentUser.UserId, email = _currentUser.Email, roles = _currentUser.Roles, vendorId = _currentUser.VendorId, lojaId = _currentUser.LojaId, isAdmin = _currentUser.IsAdmin, isVendedor = _currentUser.IsVendedor });
}
```

`JwtBearer` lê primeiro de `Authorization` header, depois de cookie `__Host-access_token` via `OnMessageReceived`:
```csharp
opts.Events = new JwtBearerEvents {
    OnMessageReceived = ctx => {
        if (ctx.Request.Cookies.TryGetValue("__Host-access_token", out var token))
            ctx.Token = token;
        return Task.CompletedTask;
    }
};
```
Frontend `apiClient` usa `withCredentials:true` e nunca toca em token. `AuthContext` busca `/api/auth/me` ao montar.

**Hash de senha (nunca MD5/SHA1):**

```csharp
// src/Infrastructure/Identity/PasswordHasher.cs — linha 1
using BCrypt.Net;
public static class PasswordHasher
{
    public static string Hash(string senha) => BCrypt.HashPassword(senha, workFactor: 12);
    public static bool Verify(string senha, string hash) => BCrypt.Verify(senha, hash);
}
```

### 2.3. `ICurrentUser` — Abstração de HttpContext

```csharp
// src/Application/Common/Interfaces/ICurrentUser.cs — linha 1
namespace GastronomiaMarketplace.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    Guid? VendorId { get; }
    Guid? LojaId { get; }
    bool IsAdmin { get; }
    bool IsVendedor { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}

// src/Infrastructure/Identity/CurrentUser.cs — linha 1
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub"), out var g) ? g : null;
    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
    public Guid? VendorId => Guid.TryParse(User?.FindFirstValue("vendor_id"), out var g) ? g : null;
    public Guid? LojaId => Guid.TryParse(User?.FindFirstValue("loja_id"), out var g) ? g : null;
    public bool IsAdmin => User?.IsInRole("Admin") == true || User?.FindFirstValue("is_admin") == "true";
    public bool IsVendedor => User?.IsInRole("Vendedor") == true;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public IReadOnlyList<string> Roles => User?.FindAll("role").Select(c => c.Value).ToList() ?? [];
}
```

Registro:

```csharp
// src/WebAPI/Extensions/ServiceCollectionExtensions.cs — linha 1 (fragmento)
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUser, CurrentUser>();
```

---

## 3. Configuração de Segurança no ASP.NET Core 10 — `Program.cs`

### 3.1. `Program.cs` completo (trecho normativo)

```csharp
// src/WebAPI/Program.cs — linha 1
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using GastronomiaMarketplace.Application;
using GastronomiaMarketplace.Infrastructure;
using GastronomiaMarketplace.WebAPI.Extensions;
using GastronomiaMarketplace.WebAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Options tipadas
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuração Jwt ausente.");

// 2. Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Gastronomia Marketplace API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe: Bearer {seu token JWT}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// 3. Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Autenticação JWT — features modernas .NET 10
builder.Services
    .AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // true em prod
        opts.SaveToken = true;
        opts.MapInboundClaims = false; // .NET 10: desativa mapeamento legado ClaimTypes
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOpts.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2), // tolerância
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        opts.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                ctx.Response.Headers.Append("WWW-Authenticate", $"Bearer error=\"invalid_token\", error_description=\"{ctx.Exception.Message}\"");
                return Task.CompletedTask;
            }
        };
    });

// 5. Autorização baseada em políticas (Policy-Based)
builder.Services.AddAuthorization(opts =>
{
    // Cliente autenticado (qualquer role autenticada)
    opts.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());

    // Apenas Admin
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

    // Vendedor com loja aprovada (tem vendor_id)
    opts.AddPolicy("VendedorAprovado", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireRole("Vendedor");
        p.RequireClaim("vendor_id");
        p.RequireClaim("loja_status", "Aprovado");
    });

    // Híbrido: Vendedor dono do recurso OU Admin (resource-based — ver §4)
    // Esta policy exige autenticação; a verificação de ownership é feita no handler de recurso
    opts.AddPolicy("VendorOwnerOrAdmin", p =>
    {
        p.RequireAuthenticatedUser();
        // Não exige role aqui — handler decide: se IsAdmin → passa; se VendorId == recurso.VendorId → passa; senão 403
    });

    // Cliente pode editar próprio perfil (ex: alergias)
    opts.AddPolicy("SelfOrAdmin", p => p.RequireAuthenticatedUser());
});

// Handlers de autorização resource-based
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, VendorOwnerHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AdminBypassHandler>();

// 6. Rate limiting (ver secure_coding_practices.md)
builder.Services.AddRateLimitingPolicies();

// 7. CORS (frontend em localhost:5173 / vercel)
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Pipeline — ORDEM IMPORTA
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("default");
app.MapHealthChecks("/health");

await app.ApplyMigrationsAsync(); // extension

app.Run();
```

### 3.2. `appsettings.json` (modelo)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=gastronomia;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "gastronomia-marketplace",
    "Audience": "gastronomia-marketplace-client",
    "SecretKey": "CHANGE_ME_32+_CHARS_MIN_SUPER_SECRET_KEY_!@#",
    "ExpiracaoMinutos": 60,
    "RefreshExpiracaoDias": 7
  },
  "AllowedOrigins": ["http://localhost:5173", "https://seu-front.vercel.app"],
  "TaxaComissaoPadraoPercentual": 8.5
}
```

> **Segredo:** Em produção, `Jwt:SecretKey` vem de `dotnet user-secrets`, variável de ambiente `JWT__SECRETKEY` ou Azure Key Vault — nunca commitado.

---

## 4. Políticas de Autorização Customizadas — Resource-Based

### 4.1. Por que Policy + Resource Handler e não apenas `[Authorize(Roles="Vendedor")]`?

- `[Authorize(Roles="Vendedor")]` verifica **papel**, mas não verifica **dono do recurso**.
- Sem handler, um Vendedor A poderia chamar `GET /api/vendedor/pedidos/{id}` de um pedido da Loja B e ver dados de outro tenant.
- **Resource-based authorization** valida `recurso.VendorId == token.vendor_id` ou `token.is_admin == true`.

### 4.2. Requirement e Handler — `VendorOwner`

```csharp
// src/WebAPI/Policies/VendorOwnerRequirement.cs — linha 1
namespace GastronomiaMarketplace.WebAPI.Policies;

using Microsoft.AspNetCore.Authorization;

public sealed class VendorOwnerRequirement : IAuthorizationRequirement { }

// Marker para recursos que têm VendorId
public interface IOwnedByVendor
{
    Guid VendorId { get; }
}

// Exemplos de recursos
public sealed record PedidoResource(Guid PedidoId, Guid VendorId) : IOwnedByVendor;
public sealed record ProdutoResource(Guid ProdutoId, Guid VendorId) : IOwnedByVendor;
```

```csharp
// src/WebAPI/Policies/VendorOwnerHandler.cs — linha 1
namespace GastronomiaMarketplace.WebAPI.Policies;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public sealed class VendorOwnerHandler : AuthorizationHandler<VendorOwnerRequirement, IOwnedByVendor>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VendorOwnerRequirement requirement,
        IOwnedByVendor resource)
    {
        // 1. Admin bypass global — ignora validação de ownership
        if (context.User.IsInRole("Admin") || context.User.FindFirstValue("is_admin") == "true")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Vendedor: compara claim vendor_id com resource.VendorId
        var vendorIdClaim = context.User.FindFirstValue("vendor_id");
        if (Guid.TryParse(vendorIdClaim, out var vendorId) && vendorId == resource.VendorId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 3. Não autorizado — não chama Succeed → resulta em 403
        return Task.CompletedTask;
    }
}

// Handler adicional que libera Admin para qualquer resource (mesmo sem IOwnedByVendor)
public sealed class AdminBypassHandler : AuthorizationHandler<VendorOwnerRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, VendorOwnerRequirement requirement)
    {
        if (context.User.IsInRole("Admin") || context.User.FindFirstValue("is_admin") == "true")
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

### 4.3. Uso no Controller — duas formas

**Forma A — via `IAuthorizationService` (recomendada, explícita):**

```csharp
// src/WebAPI/Controllers/PedidosController.cs — linha 1 (fragmento)
[HttpGet("{id:guid}")]
[Authorize(Policy = "VendorOwnerOrAdmin")]
public async Task<IActionResult> ObterPorId(
    Guid id,
    [FromServices] IAuthorizationService auth,
    CancellationToken ct)
{
    var pedido = await _mediator.Send(new ObterPedidoPorIdQuery(id), ct);
    if (pedido is null) return NotFound();

    // Monta recurso para validação
    var resource = new PedidoResource(pedido.Id, pedido.VendorId);
    var authResult = await auth.AuthorizeAsync(User, resource, "VendorOwnerOrAdmin");
    if (!authResult.Succeeded) return Forbid();

    return Ok(pedido);
}

[HttpPost("{id:guid}/aprovar")]
[Authorize(Policy = "VendorOwnerOrAdmin")]
public async Task<IActionResult> Aprovar(Guid id, [FromServices] IAuthorizationService auth, CancellationToken ct)
{
    // Carrega pedido para obter VendorId (ou use query otimizada que retorna só VendorId)
    var pedido = await _db.Pedidos.AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new PedidoResource(p.Id, p.Loja.VendedorId))
        .FirstOrDefaultAsync(ct);
    if (pedido is null) return NotFound();

    var authResult = await auth.AuthorizeAsync(User, pedido, "VendorOwnerOrAdmin");
    if (!authResult.Succeeded) return Forbid();

    var result = await _mediator.Send(new AprovarPedidoCommand(id, _currentUser.UserId!.Value, _currentUser.VendorId, _currentUser.IsAdmin), ct);
    return result.IsSuccess ? Ok(result) : BadRequest(result);
}
```

**Forma B — via atributo + `AuthorizeAsync` implícito (quando o resource é o próprio endpoint):**

```csharp
[HttpPut("produtos/{produtoId:guid}")]
[Authorize(Policy = "VendedorAprovado")]
public async Task<IActionResult> AtualizarProduto(Guid produtoId, [FromBody] AtualizarProdutoDto dto, CancellationToken ct)
{
    // Handler do command já revalida ownership (defesa em profundidade)
    var cmd = new AtualizarProdutoCommand(produtoId, dto.Nome, dto.Preco, dto.Descricao, _currentUser.VendorId!.Value);
    var result = await _mediator.Send(cmd, ct);
    return result.IsSuccess ? NoContent() : BadRequest(result);
}
```

### 4.4. Defesa em Profundidade — Handler também valida

Mesmo com Policy no controller, o **CommandHandler revalida ownership** (ver `architecture.md` §4.4). Isso protege contra:
- Chamadas internas (ex: outro handler chama `AprovarPedidoCommand` diretamente).
- Esquecimento de `AuthorizeAsync` no controller.
- Testes de integração que chamam MediatR sem passar por `Authorization`.

---

## 5. Fluxo de Transição — Upgrade de Conta (Cliente → Vendedor)

### 5.1. Diagrama de sequência

```
Cliente (autenticado, role=Cliente)
  │
  ├─► POST /api/vendedor/solicitar-upgrade
  │     Body: { nomeLoja, categoriaLojaId, enderecoComercial, cnpj?, telefone }
  │     Auth: Bearer <token sem vendor_id>
  │
  ▼
WebAPI: VendedorController.SolicitarUpgrade
  │
  ├─► MediatR: SolicitarUpgradeCommand
  │     ├─ Valida: nomeLoja único na cidade? categoria existe? endereço geocodificável?
  │     ├─ Cria: Vendedor { UsuarioId, NomeLoja, CategoriaId, Endereco, Status=PendenteAprovacao }
  │     ├─ Atualiza: Usuario.Papel = Vendedor (mantém Cliente implícito)
  │     └─ SaveChanges + Auditoria
  │
  ◄─ 202 Accepted { vendedorId, status: "PendenteAprovacao", mensagem: "Aguardando aprovação do Admin" }
  │
  │  (Token ainda NÃO contém vendor_id — painel mostra "em análise")
  │
Admin
  │
  ├─► PATCH /api/admin/vendedores/{vendedorId}/aprovar
  │     Auth: Bearer <token Admin>
  │
  ▼
WebAPI: AdminController.AprovarVendedor
  │
  ├─► MediatR: AprovarVendedorCommand
  │     ├─ Valida: AdminOnly
  │     ├─ Atualiza: Vendedor.Status = Aprovado
  │     ├─ Cria: Loja { VendedorId, Nome, Categoria, Endereco, Localização geography(Point) }
  │     └─ SaveChanges
  │
  ◄─ 200 OK { vendedorId, lojaId, status: "Aprovado" }

Cliente (agora Vendedor aprovado)
  │
  ├─► POST /api/auth/refresh  OU  POST /api/auth/login (reemissão)
  │     Auth: Bearer <token antigo>  ou  { email, senha }
  │
  ▼
TokenService.GerarToken  (agora inclui vendor_id + loja_id + loja_status=Aprovado)
  │
  ◄─ 200 OK { accessToken: "eyJ...", refreshToken: "...", expiresIn: 3600 }

Frontend: armazena novo token → exibe botão "Ir para o Painel do Vendedor"
```

### 5.2. Código — `SolicitarUpgradeCommand`

```csharp
// src/Application/Vendedores/Commands/SolicitarUpgrade/SolicitarUpgradeCommand.cs — linha 1
namespace GastronomiaMarketplace.Application.Vendedores.Commands.SolicitarUpgrade;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record SolicitarUpgradeCommand(
    Guid UsuarioId,
    string NomeLoja,
    Guid CategoriaLojaId,
    string EnderecoComercial,
    string Cep,
    string Telefone,
    string? Cnpj
) : IRequest<Result<Guid>>;

// Handler
public sealed class SolicitarUpgradeCommandHandler : IRequestHandler<SolicitarUpgradeCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IGeocodingService _geo;

    public SolicitarUpgradeCommandHandler(IAppDbContext db, IGeocodingService geo) { _db = db; _geo = geo; }

    public async Task<Result<Guid>> Handle(SolicitarUpgradeCommand req, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == req.UsuarioId, ct);
        if (usuario is null) return Result<Guid>.Failure("Usuário não encontrado.");
        if (usuario.Papel == PapelUsuario.Vendedor) return Result<Guid>.Failure("Usuário já é vendedor.");
        if (usuario.Papel == PapelUsuario.Admin) return Result<Guid>.Failure("Admin não pode ser vendedor.");

        // Valida categoria
        var categoriaExiste = await _db.CategoriasLoja.AnyAsync(c => c.Id == req.CategoriaLojaId, ct);
        if (!categoriaExiste) return Result<Guid>.Failure("Categoria inválida.");

        // Valida nome de loja único (case-insensitive) na mesma cidade — extrai cidade do CEP/endereço
        // Simplificado: único global
        var nomeExiste = await _db.Vendedores.AnyAsync(v => v.NomeLoja.ToLower() == req.NomeLoja.ToLower(), ct);
        if (nomeExiste) return Result<Guid>.Failure("Já existe loja com este nome.");

        // Geocodifica endereço → lat/lng (para cálculo de proximidade)
        var coord = await _geo.GeocodificarAsync(req.EnderecoComercial, req.Cep, ct);
        if (coord is null) return Result<Guid>.Failure("Endereço não pôde ser geocodificado.");

        var vendedor = new Vendedor(
            usuarioId: req.UsuarioId,
            nomeLoja: req.NomeLoja,
            categoriaLojaId: req.CategoriaLojaId,
            enderecoComercial: req.EnderecoComercial,
            cep: req.Cep,
            telefone: req.Telefone,
            cnpj: req.Cnpj,
            latitude: coord.Latitude,
            longitude: coord.Longitude
        );

        _db.Vendedores.Add(vendedor);
        usuario.PromoverParaVendedor(); // domain: Papel = Vendedor

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(vendedor.Id);
    }
}
```

```csharp
// src/Domain/Entities/Usuario.cs — linha 1 (fragmento)
public void PromoverParaVendedor()
{
    if (Papel == PapelUsuario.Admin) throw new RegraDeNegocioException("Admin não pode virar vendedor.");
    if (Papel == PapelUsuario.Vendedor) throw new RegraDeNegocioException("Usuário já é vendedor.");
    Papel = PapelUsuario.Vendedor;
    AtualizadoEmUtc = DateTime.UtcNow;
    AddDomainEvent(new ContaUpgradedParaVendedorEvent(Id));
}
```

### 5.3. Reemissão do Token — por que é obrigatória?

JWT é **stateless** — claims são assinadas no momento da emissão. Após o Admin aprovar a loja, o token antigo **ainda não contém `vendor_id`**. O cliente precisa obter um novo token. Opções:

| Estratégia | Endpoint | Quando usar |
|---|---|---|
| **Refresh token** (recomendado) | `POST /api/auth/refresh` com `refreshToken` | Fluxo padrão — frontend chama automaticamente após receber 200 do Admin (via polling ou websocket) |
| **Re-login** | `POST /api/auth/login` com email/senha | Fallback simples |
| **Token de curta duração** | `exp` em 15 min | Força refresh rápido, mas aumenta chamadas |

**Implementação do refresh:**

```csharp
// src/WebAPI/Controllers/AuthController.cs — linha 1 (fragmento)
[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshDto dto, CancellationToken ct)
{
    var principal = _tokenService.ValidarRefreshToken(dto.RefreshToken);
    if (principal is null) return Unauthorized(new { error = "Refresh token inválido." });

    var userId = Guid.Parse(principal.FindFirstValue("sub")!);
    // Recarrega do banco para pegar VendorId/LojaId atualizados
    var usuario = await _db.Usuarios
        .Include(u => u.Vendedor)
        .ThenInclude(v => v.Loja)
        .FirstOrDefaultAsync(u => u.Id == userId, ct);
    if (usuario is null) return Unauthorized();

    var tokenDto = new TokenService.UsuarioTokenDto(
        usuario.Id, usuario.Email, usuario.NomeCompleto, usuario.Papel,
        usuario.Papel == PapelUsuario.Admin,
        usuario.Vendedor?.Id,
        usuario.Vendedor?.Loja?.Id,
        usuario.Vendedor?.Status.ToString()
    );

    var novoAccessToken = _tokenService.GerarToken(tokenDto);
    var novoRefreshToken = _tokenService.GerarRefreshToken(userId);
    return Ok(new { accessToken = novoAccessToken, refreshToken = novoRefreshToken });
}
```

### 5.4. Mutação no Banco — resumo transacional

```sql
-- Transação 1: solicitar upgrade (Cliente → Vendedor pendente)
BEGIN;
  INSERT INTO "Vendedores" (id, usuario_id, nome_loja, categoria_loja_id, status, endereco, lat, lng, criado_em)
  VALUES (gen_random_uuid(), :usuarioId, :nomeLoja, :categoriaId, 1 /*Pendente*/, :endereco, :lat, :lng, now());
  UPDATE "Usuarios" SET papel = 2 /*Vendedor*/, atualizado_em = now() WHERE id = :usuarioId;
  INSERT INTO "AuditLogs" (...) VALUES (...); -- via interceptor
COMMIT;

-- Transação 2: Admin aprova
BEGIN;
  UPDATE "Vendedores" SET status = 2 /*Aprovado*/, atualizado_em = now() WHERE id = :vendedorId;
  INSERT INTO "Lojas" (id, vendedor_id, nome, categoria_loja_id, endereco, localizacao, ativo, criado_em)
  VALUES (gen_random_uuid(), :vendedorId, :nomeLoja, :categoriaId, :endereco, ST_SetSRID(ST_MakePoint(:lng, :lat), 4326), true, now());
  INSERT INTO "AuditLogs" (...) VALUES (...);
COMMIT;

-- Após commit, cliente chama /auth/refresh → novo JWT com vendor_id + loja_id
```

---

## 6. Tabela de Matriz de Acesso (RBAC)

| Recurso / Endpoint | Cliente | Vendedor (dono) | Vendedor (outra loja) | Admin |
|---|---|---|---|---|
| `GET /api/lojas/proximas` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/pedidos` (criar) | ✅ | ✅ | ✅ | ✅ |
| `GET /api/pedidos/meus` | ✅ (só seus) | ✅ (só seus como cliente) | ✅ | ✅ (todos) |
| `GET /api/vendedor/pedidos` | ❌ 403 | ✅ (só da sua loja) | ❌ 403 | ✅ (todos) |
| `POST /api/vendedor/pedidos/{id}/aprovar` | ❌ 403 | ✅ (só da sua loja) | ❌ 403 | ✅ |
| `PUT /api/vendedor/produtos/{id}` | ❌ 403 | ✅ (só seus) | ❌ 403 | ✅ |
| `POST /api/vendedor/produtos` | ❌ 403 | ✅ (cria pendente) | ❌ | ✅ |
| `PATCH /api/admin/vendedores/{id}/aprovar` | ❌ 403 | ❌ 403 | ❌ 403 | ✅ |
| `PATCH /api/admin/produtos/{id}/aprovar` | ❌ 403 | ❌ 403 | ❌ 403 | ✅ |
| `GET /api/admin/relatorios/faturamento` | ❌ 403 | ❌ 403 | ❌ 403 | ✅ |
| `GET /api/admin/audit-logs` | ❌ 403 | ❌ 403 | ❌ 403 | ✅ |

---

## 7. Checklist para o Agente

- [ ] Implementar `JwtOptions`, `TokenService`, `CurrentUser` exatamente como §2.2–2.3.
- [ ] Configurar `AddAuthentication().AddJwtBearer()` + `AddAuthorization` com 5 policies em `Program.cs` (§3.1).
- [ ] Criar `VendorOwnerRequirement` + `VendorOwnerHandler` + `AdminBypassHandler` (§4.2).
- [ ] Em todo controller de vendedor, chamar `IAuthorizationService.AuthorizeAsync(User, resource, "VendorOwnerOrAdmin")` + revalidar no Handler.
- [ ] Implementar `SolicitarUpgradeCommand` + `AprovarVendedorCommand` com geocodificação.
- [ ] Garantir que `vendor_id` só entra no JWT quando `Vendedor.Status == Aprovado`.
- [ ] Implementar `POST /api/auth/refresh` que recarrega `VendorId`/`LojaId` do banco.
- [ ] Testar matriz RBAC com testes de integração: Cliente não acessa `/api/vendedor/*`, Vendedor A não aprova pedido da Loja B, Admin aprova tudo.
