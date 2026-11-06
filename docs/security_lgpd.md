# Security LGPD — Conformidade com a Lei Geral de Proteção de Dados (Lei 13.709/2018)

> **Escopo:** Dados sensíveis de saúde (alergias severas, restrições alimentares) + dados pessoais de clientes
> **Stack:** EF Core 10 + Npgsql + ASP.NET Core DataProtection + PostgreSQL `pgcrypto` (opcional)
> **Versão:** 1.0 — 2026-09-09
> **Aviso:** Este documento é especificação técnica, não parecer jurídico. Valide com DPO/Encarregado.

---

## 1. Inventário de Dados Pessoais e Sensíveis

### 1.1. Classificação

| Categoria | Campos | Base legal LGPD (art. 7º) | Sensível (art. 11)? |
|---|---|---|---|
| **Identificação** | `nome_completo`, `email`, `cpf`, `telefone`, `endereco`, `cep` | Execução de contrato (compra) + consentimento | Não |
| **Credenciais** | `senha_hash` | Legítimo interesse (segurança) | Não (mas sigiloso) |
| **Saúde — crítico** | `alergias_severas`, `restricoes_alimentares`, `observacoes_saude` | **Consentimento específico e destacado** (art. 11, I) | **Sim — art. 11** |
| **Geolocalização** | `endereco_atual`, `lat/lng` | Consentimento (filtro por proximidade) | Não, mas pessoal |
| **Transacional** | `pedidos`, `itens`, `valores`, `comissoes` | Cumprimento obrigação legal/fiscal (5 anos) | Não |
| **Consentimento** | `consentimentos` (tabela) | Registro de consentimento | — |

**Princípio da minimização (art. 6º, III):** Coletar apenas o necessário. Alergias/restrições são **opcionais** no cadastro, mas se preenchidas, exigem consentimento explícito separado (checkbox não pré-marcado + texto claro).

### 1.2. Texto de consentimento (exemplo para frontend)

> "Autorizo o Marketplace a armazenar minhas alergias e restrições alimentares para exibir alertas de segurança ao vendedor e filtrar produtos. Posso revogar a qualquer momento em Meu Perfil → Privacidade. [Saiba mais sobre LGPD]"

Checkbox: `[ ] Li e autorizo o tratamento dos meus dados de saúde para fins de segurança alimentar.`

Sem este consentimento, os campos de saúde **não são persistidos** (ou são persistidos como `NULL`).

---

## 2. Proteção de Dados Sensíveis de Saúde — Criptografia em Nível de Coluna via EF Core

### 2.1. Estratégia: `HasConversion` + ASP.NET Core DataProtection

**Por que não criptografia transparente do Postgres (TDE)?** TDE protege disco, mas não protege acesso via SQL (`SELECT alergias FROM usuarios` ainda retorna plaintext para quem tem acesso ao banco). Criptografia em nível de aplicação garante que mesmo um `SELECT` direto no banco retorna ciphertext, e apenas a aplicação com a chave DataProtection consegue descriptografar.

**Alternativa com `pgcrypto`:** Válida, mas acopla ao Postgres e exige gerenciamento de chave no banco. Este projeto padroniza **DataProtection** (chave em filesystem/KeyVault) + `HasConversion`.

### 2.2. Implementação — `EncryptedConverter`

```csharp
// src/Infrastructure/Security/DataProtection/EncryptedConverter.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Security.DataProtection;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public sealed class EncryptedConverter : ValueConverter<string?, string?>
{
    public EncryptedConverter(IDataProtector protector, ConverterMappingHints? hints = null)
        : base(
            v => v == null ? null : protector.Protect(v),   // para o banco
            v => v == null ? null : protector.Unprotect(v), // do banco
            hints)
    { }
}

// Factory para criar protectors com propósito isolado por campo
public static class ProtectorFactory
{
    public static IDataProtector ParaAlergias(IDataProtectionProvider provider)
        => provider.CreateProtector("GastronomiaMarketplace.Alergias.v1");

    public static IDataProtector ParaRestricoes(IDataProtectionProvider provider)
        => provider.CreateProtector("GastronomiaMarketplace.Restricoes.v1");
}
```

### 2.3. Configuração no `AppDbContext` / `UsuarioConfiguration`

```csharp
// src/Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Infrastructure.Security.DataProtection;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    private readonly IDataProtectionProvider _dpProvider;

    // Injetado via DI — ver §2.4
    public UsuarioConfiguration(IDataProtectionProvider dpProvider) => _dpProvider = dpProvider;

    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        b.Property(u => u.Email).HasColumnType("varchar(254)").IsRequired();
        b.HasIndex(u => u.Email).IsUnique();

        b.Property(u => u.NomeCompleto).HasColumnType("varchar(200)").IsRequired();
        b.Property(u => u.Cpf).HasColumnType("varchar(11)");
        b.HasIndex(u => u.Cpf).IsUnique();

        // --- CAMPOS SENSÍVEIS — criptografados ---
        var alergiasProtector = ProtectorFactory.ParaAlergias(_dpProvider);
        b.Property(u => u.AlergiasSeveras)
            .HasColumnType("text") // ciphertext é maior que plaintext
            .HasConversion(new EncryptedConverter(alergiasProtector))
            .IsRequired(false);

        var restricoesProtector = ProtectorFactory.ParaRestricoes(_dpProvider);
        b.Property(u => u.RestricoesAlimentares)
            .HasColumnType("text")
            .HasConversion(new EncryptedConverter(restricoesProtector))
            .IsRequired(false);

        b.Property(u => u.ObservacoesSaude)
            .HasColumnType("text")
            .HasConversion(new EncryptedConverter(alergiasProtector))
            .IsRequired(false);

        // --- NÃO criptografados mas pessoais ---
        b.Property(u => u.EnderecoAtual).HasColumnType("varchar(500)");
        b.Property(u => u.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(u => u.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(u => u.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);

        b.HasQueryFilter(u => !u.IsDeleted); // soft delete global
    }
}
```

**Entidade `Usuario` (fragmento):**

```csharp
// src/Domain/Entities/Usuario.cs — linha 1 (fragmento)
public sealed class Usuario : AggregateRoot
{
    public string NomeCompleto { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;

    // Sensíveis — opcional, exige consentimento
    public string? AlergiasSeveras { get; private set; } // ex: "Amendoim (anafilaxia), Camarão"
    public string? RestricoesAlimentares { get; private set; } // ex: "Vegano, Sem glúten, Sem lactose"
    public string? ObservacoesSaude { get; private set; }

    public void AtualizarDadosSaude(string? alergias, string? restricoes, string? observacoes)
    {
        // Validação de domínio: tamanho máximo, sanitização básica
        if (alergias?.Length > 2000) throw new RegraDeNegocioException("Alergias excede 2000 caracteres.");
        AlergiasSeveras = alergias?.Trim();
        RestricoesAlimentares = restricoes?.Trim();
        ObservacoesSaude = observacoes?.Trim();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void RemoverDadosSaude()
    {
        AlergiasSeveras = null;
        RestricoesAlimentares = null;
        ObservacoesSaude = null;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
```

### 2.4. Registro do DataProtection em `Program.cs`

```csharp
// src/WebAPI/Program.cs — linha 1 (fragmento)
using Microsoft.AspNetCore.DataProtection;

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeyPath"] ?? "./keys"))
    // Em produção: .PersistKeysToAzureBlobStorage(...) ou .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("GastronomiaMarketplace")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Para rotação: chaves antigas continuam válidas para Unprotect; novas escritas usam chave mais recente.
// NUNCA apague a pasta ./keys sem re-encriptar dados existentes.

// Registro da configuration que precisa de IDataProtectionProvider
builder.Services.AddScoped<IEntityTypeConfiguration<Usuario>, UsuarioConfiguration>();
// Alternativa: construir AppDbContext com factory que resolve protector manualmente
```

**Migração gerada:**

```sql
-- Migrations/20260909_Initial.cs (fragmento)
CREATE TABLE "Usuarios" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Email" varchar(254) NOT NULL,
    "AlergiasSeveras" text NULL, -- armazena ciphertext base64
    "RestricoesAlimentares" text NULL,
    ...
);
```

**Teste de que `SELECT` direto retorna ciphertext:**

```sql
-- Conexão psql direta (sem passar pela app) — deve ver algo como:
SELECT "AlergiasSeveras" FROM "Usuarios" LIMIT 1;
-- CfDJ8N... (base64 protegido, não plaintext)
```

### 2.5. Mascaramento em Logs e Respostas

- **Nunca logar** `AlergiasSeveras` em plaintext. Se precisar logar, mascarar: `"Alergias: *** (presente)"`.
- **Resposta da API:** `GET /api/usuarios/me` retorna dados de saúde apenas para o próprio usuário ou Admin com justificativa auditada. Listagens (`GET /api/admin/usuarios`) **não retornam** campos sensíveis por padrão — exige `?includeSensitive=true` + policy `AdminOnly` + log de acesso.

```csharp
// DTO de resposta — separa campos sensíveis
public sealed record UsuarioDto(
    Guid Id, string Nome, string Email,
    string? AlergiasSeveras, string? RestricoesAlimentares // null se não autorizado
);
```

---

## 3. Ciclo de Vida do Dado e Direitos do Titular

### 3.1. Direito de Exclusão — "Esquecimento" (Art. 18, VI) com Anonimização Irreversível

**Problema:** Pedidos antigos precisam ser mantidos por 5 anos para obrigações fiscais/contábeis (Código Tributário Nacional, art. 173). Deletar pedidos quebra rastreabilidade financeira e relatórios de comissão do Admin.

**Solução: Anonimização (art. 12) — dado anonimizado deixa de ser dado pessoal.**

#### 3.1.1. Estratégia

1.  **Soft delete + anonimização do `Usuario`**, mas **preservação dos `Pedidos`** com dados anonimizados.
2.  Campos anonimizados: `nome → "Titular Anonimizado <hash>"`, `email → "anon_<hash>@anon.local"`, `cpf → NULL`, `telefone → NULL`, `endereco → NULL`, `alergias/restricoes → NULL`, `senha_hash → "ANONIMIZADO"`.
3.  `Pedidos` mantêm `cliente_id` apontando para o usuário anonimizado (para integridade referencial), mas snapshot de dados do pedido (ex: `cliente_nome_snapshot`) também é anonimizado.

#### 3.1.2. Código — `AnonimizarUsuarioCommand`

```csharp
// src/Application/Usuarios/Commands/AnonimizarUsuario/AnonimizarUsuarioCommand.cs — linha 1
namespace GastronomiaMarketplace.Application.Usuarios.Commands.AnonimizarUsuario;

using MediatR;

public sealed record AnonimizarUsuarioCommand(Guid UsuarioId, Guid SolicitanteId, bool IsAdmin) : IRequest<Result>;

public sealed class AnonimizarUsuarioCommandHandler : IRequestHandler<AnonimizarUsuarioCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AnonimizarUsuarioCommandHandler(IAppDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result> Handle(AnonimizarUsuarioCommand req, CancellationToken ct)
    {
        // Autorização: próprio titular OU Admin
        if (req.UsuarioId != req.SolicitanteId && !req.IsAdmin)
            return Result.Failure("Apenas o titular ou Admin pode solicitar anonimização.");

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == req.UsuarioId, ct);
        if (usuario is null) return Result.Failure("Usuário não encontrado.");
        if (usuario.IsAnonimizado) return Result.Failure("Usuário já anonimizado.");

        // Verificar pedidos pendentes/em preparo — não anonimizar se houver pedido ativo
        var temPedidoAtivo = await _db.Pedidos.AnyAsync(p =>
            p.ClienteId == req.UsuarioId &&
            (p.Status == StatusPedido.Pendente || p.Status == StatusPedido.EmPreparo || p.Status == StatusPedido.ProntoParaRetirada), ct);
        if (temPedidoAtivo)
            return Result.Failure("Não é possível anonimizar enquanto houver pedidos em andamento. Finalize ou cancele os pedidos.");

        // Hash irreversível para rastreabilidade sem identificação
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(usuario.Id.ToString())))[..8].ToLower();

        // Anonimização irreversível
        usuario.Anonimizar(hash); // domain method abaixo

        // Anonimizar snapshots em pedidos (se houver coluna snapshot)
        var pedidos = await _db.Pedidos.Where(p => p.ClienteId == req.UsuarioId).ToListAsync(ct);
        foreach (var p in pedidos)
            p.AnonimizarSnapshotCliente(hash);

        // Revogar consentimentos
        var consentimentos = await _db.ConsentimentosLgpd.Where(c => c.UsuarioId == req.UsuarioId).ToListAsync(ct);
        foreach (var c in consentimentos) c.Revogar("Titular solicitou anonimização");

        // Registrar auditoria LGPD
        _db.AuditLogs.Add(new AuditLog(
            usuarioId: req.SolicitanteId,
            acao: "ANONIMIZACAO_LGPD",
            entidade: "Usuario",
            entidadeId: usuario.Id.ToString(),
            detalhes: $"Usuario {usuario.Id} anonimizado (hash={hash}) por {req.SolicitanteId} (IsAdmin={req.IsAdmin})"
        ));

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// Domain
public void Anonimizar(string hash)
{
    NomeCompleto = $"Titular Anonimizado {hash}";
    Email = $"anon_{hash}@anon.local";
    Cpf = null;
    Telefone = null;
    EnderecoAtual = null;
    AlergiasSeveras = null;
    RestricoesAlimentares = null;
    ObservacoesSaude = null;
    SenhaHash = "ANONIMIZADO";
    IsAnonimizado = true;
    IsDeleted = true; // soft delete — não aparece em queries normais
    AtualizadoEmUtc = DateTime.UtcNow;
    AddDomainEvent(new UsuarioAnonimizadoEvent(Id, hash));
}
```

#### 3.1.3. Endpoint

```csharp
[HttpDelete("api/usuarios/{id:guid}/anonimizar")]
[Authorize(Policy = "SelfOrAdmin")]
public async Task<IActionResult> Anonimizar(Guid id, CancellationToken ct)
{
    var cmd = new AnonimizarUsuarioCommand(id, _currentUser.UserId!.Value, _currentUser.IsAdmin);
    var result = await _mediator.Send(cmd, ct);
    return result.IsSuccess ? NoContent() : BadRequest(result);
}
```

> **Retenção fiscal:** Pedidos anonimizados mantêm `valor_bruto`, `taxa_comissao`, `valor_liquido`, `data`, `loja_id` — suficiente para relatórios fiscais sem identificar o titular. Após 5 anos, pedidos também podem ser expurgados ou arquivados.

### 3.2. Direito de Portabilidade (Art. 18, V) — Exportação JSON Estruturado

**Requisito:** Fornecer ao titular, em formato estruturado e interoperável, todos os dados pessoais que lhe concernem.

#### 3.2.1. Estrutura do JSON de portabilidade

```json
{
  "exportadoEm": "2026-09-09T14:30:00Z",
  "titular": {
    "id": "a3f1c2d4-...",
    "nomeCompleto": "João Silva",
    "email": "joao@exemplo.com",
    "cpf": "12345678901",
    "telefone": "+55 11 99999-9999",
    "enderecoAtual": "Rua Exemplo, 123 - São Paulo/SP - 01310-100",
    "criadoEm": "2025-03-15T10:00:00Z"
  },
  "dadosSaude": {
    "alergiasSeveras": "Amendoim (anafilaxia)",
    "restricoesAlimentares": "Vegano, Sem glúten",
    "observacoesSaude": null
  },
  "consentimentos": [
    {
      "tipo": "DadosSaude",
      "concedidoEm": "2025-03-15T10:05:00Z",
      "revogadoEm": null,
      "ativo": true,
      "textoConsentimento": "Autorizo o tratamento de dados de saúde para segurança alimentar..."
    }
  ],
  "pedidos": [
    {
      "id": "b2c3d4e5-...",
      "loja": "Cantina da Maria",
      "status": "Finalizado",
      "criadoEm": "2026-08-20T18:30:00Z",
      "valorBruto": 89.90,
      "itens": [
        { "produto": "Pizza Margherita", "quantidade": 1, "precoUnitario": 59.90 },
        { "produto": "Suco Natural", "quantidade": 2, "precoUnitario": 15.00 }
      ]
    }
  ],
  "vendedor": null
}
```
*Se o usuário for vendedor, inclui bloco `vendedor: { nomeLoja, categoria, enderecoComercial, status }`.*

#### 3.2.2. Código — `ExportarDadosPortabilidadeQuery`

```csharp
// src/Application/Usuarios/Queries/ExportarDados/ExportarDadosPortabilidadeQuery.cs — linha 1
namespace GastronomiaMarketplace.Application.Usuarios.Queries.ExportarDados;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed record ExportarDadosPortabilidadeQuery(Guid UsuarioId, Guid SolicitanteId, bool IsAdmin) : IRequest<Result<string>>;

public sealed class ExportarDadosPortabilidadeQueryHandler : IRequestHandler<ExportarDadosPortabilidadeQuery, Result<string>>
{
    private readonly IAppDbContext _db;

    public ExportarDadosPortabilidadeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Result<string>> Handle(ExportarDadosPortabilidadeQuery req, CancellationToken ct)
    {
        if (req.UsuarioId != req.SolicitanteId && !req.IsAdmin)
            return Result<string>.Failure("Apenas titular ou Admin.");

        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Vendedor)
            .FirstOrDefaultAsync(u => u.Id == req.UsuarioId, ct);
        if (usuario is null) return Result<string>.Failure("Usuário não encontrado.");
        if (usuario.IsAnonimizado) return Result<string>.Failure("Dados já anonimizados.");

        var consentimentos = await _db.ConsentimentosLgpd
            .AsNoTracking().Where(c => c.UsuarioId == req.UsuarioId).ToListAsync(ct);

        var pedidos = await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == req.UsuarioId)
            .Include(p => p.Itens).ThenInclude(i => i.Produto)
            .Include(p => p.Loja)
            .OrderByDescending(p => p.CriadoEmUtc)
            .ToListAsync(ct);

        var export = new
        {
            exportadoEm = DateTime.UtcNow,
            titular = new
            {
                id = usuario.Id,
                nomeCompleto = usuario.NomeCompleto,
                email = usuario.Email,
                cpf = usuario.Cpf, // descriptografado automaticamente via HasConversion
                telefone = usuario.Telefone,
                enderecoAtual = usuario.EnderecoAtual,
                criadoEm = usuario.CriadoEmUtc
            },
            dadosSaude = new
            {
                alergiasSeveras = usuario.AlergiasSeveras,
                restricoesAlimentares = usuario.RestricoesAlimentares,
                observacoesSaude = usuario.ObservacoesSaude
            },
            consentimentos = consentimentos.Select(c => new
            {
                tipo = c.Tipo.ToString(),
                concedidoEm = c.ConcedidoEmUtc,
                revogadoEm = c.RevogadoEmUtc,
                ativo = c.Ativo,
                textoConsentimento = c.TextoConsentimento
            }),
            pedidos = pedidos.Select(p => new
            {
                id = p.Id,
                loja = p.Loja.Nome,
                status = p.Status.ToString(),
                criadoEm = p.CriadoEmUtc,
                valorBruto = p.ValorBruto.Valor,
                itens = p.Itens.Select(i => new
                {
                    produto = i.ProdutoNomeSnapshot, // snapshot para caso produto seja deletado
                    quantidade = i.Quantidade,
                    precoUnitario = i.PrecoUnitario.Valor
                })
            }),
            vendedor = usuario.Vendedor is null ? null : new
            {
                nomeLoja = usuario.Vendedor.NomeLoja,
                categoria = usuario.Vendedor.CategoriaLoja.Nome,
                enderecoComercial = usuario.Vendedor.EnderecoComercial,
                status = usuario.Vendedor.Status.ToString()
            }
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        return Result<string>.Success(json);
    }
}
```

**Controller:**

```csharp
[HttpGet("api/usuarios/{id:guid}/exportar-dados")]
[Authorize(Policy = "SelfOrAdmin")]
public async Task<IActionResult> Exportar(Guid id, CancellationToken ct)
{
    var query = new ExportarDadosPortabilidadeQuery(id, _currentUser.UserId!.Value, _currentUser.IsAdmin);
    var result = await _mediator.Send(query, ct);
    if (!result.IsSuccess) return BadRequest(result);
    return File(
        System.Text.Encoding.UTF8.GetBytes(result.Value!),
        "application/json",
        $"dados-portabilidade-{id}-{DateTime.UtcNow:yyyyMMdd}.json"
    );
}
```

**Requisitos adicionais:**
- Logar toda exportação em `AuditLogs` com `acao = "EXPORTACAO_PORTABILIDADE"`.
- Rate limit: máximo 3 exportações por usuário por dia (ver `secure_coding_practices.md`).
- Se o usuário for Admin exportando dados de outro titular, exigir justificativa (`?motivo=`) e registrar.

### 3.3. Gestão de Consentimento — Registro Auditável e Revogação

#### 3.3.1. Modelo de Dados — `ConsentimentosLgpd`

```csharp
// src/Domain/Entities/ConsentimentoLgpd.cs — linha 1
namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;

public enum TipoConsentimento { DadosSaude = 1, Geolocalizacao = 2, Marketing = 3 }

public sealed class ConsentimentoLgpd : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public TipoConsentimento Tipo { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime ConcedidoEmUtc { get; private set; }
    public DateTime? RevogadoEmUtc { get; private set; }
    public string TextoConsentimento { get; private set; } = null!; // snapshot do texto exibido
    public string? IpOrigem { get; private set; }
    public string? UserAgent { get; private set; }
    public string? MotivoRevogacao { get; private set; }

    private ConsentimentoLgpd() { }

    public static ConsentimentoLgpd Conceder(Guid usuarioId, TipoConsentimento tipo, string texto, string? ip, string? ua)
        => new()
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Ativo = true,
            ConcedidoEmUtc = DateTime.UtcNow,
            TextoConsentimento = texto,
            IpOrigem = ip,
            UserAgent = ua
        };

    public void Revogar(string? motivo = null)
    {
        if (!Ativo) return;
        Ativo = false;
        RevogadoEmUtc = DateTime.UtcNow;
        MotivoRevogacao = motivo;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Reconceder(string novoTexto, string? ip, string? ua)
    {
        Ativo = true;
        RevogadoEmUtc = null;
        TextoConsentimento = novoTexto;
        IpOrigem = ip;
        UserAgent = ua;
        ConcedidoEmUtc = DateTime.UtcNow;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
```

**Tabela Postgres:**

```sql
CREATE TABLE "ConsentimentosLgpd" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UsuarioId" uuid NOT NULL REFERENCES "Usuarios"("Id") ON DELETE CASCADE,
    "Tipo" smallint NOT NULL,
    "Ativo" boolean NOT NULL,
    "ConcedidoEmUtc" timestamptz NOT NULL,
    "RevogadoEmUtc" timestamptz NULL,
    "TextoConsentimento" text NOT NULL,
    "IpOrigem" varchar(45) NULL,
    "UserAgent" varchar(500) NULL,
    "MotivoRevogacao" varchar(500) NULL,
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL
);
CREATE INDEX "IX_Consentimentos_Usuario_Tipo" ON "ConsentimentosLgpd" ("UsuarioId", "Tipo");
```

#### 3.3.2. Fluxo de Concessão (no cadastro/atualização de perfil)

```csharp
// src/Application/Usuarios/Commands/AtualizarPerfil/AtualizarPerfilCommandHandler.cs — linha 1 (fragmento)
public async Task<Result> Handle(AtualizarPerfilCommand req, CancellationToken ct)
{
    var usuario = await _db.Usuarios.FirstAsync(u => u.Id == req.UsuarioId, ct);

    // Se enviou dados de saúde, exige consentimento explícito
    var enviouDadosSaude = req.AlergiasSeveras is not null || req.RestricoesAlimentares is not null;
    if (enviouDadosSaude && req.ConsentimentoDadosSaude != true)
        return Result.Failure("É necessário consentir explicitamente para armazenar dados de saúde.");

    if (enviouDadosSaude)
    {
        usuario.AtualizarDadosSaude(req.AlergiasSeveras, req.RestricoesAlimentares, req.ObservacoesSaude);

        // Registra ou atualiza consentimento
        var consent = await _db.ConsentimentosLgpd
            .FirstOrDefaultAsync(c => c.UsuarioId == req.UsuarioId && c.Tipo == TipoConsentimento.DadosSaude, ct);

        if (consent is null)
        {
            consent = ConsentimentoLgpd.Conceder(req.UsuarioId, TipoConsentimento.DadosSaude, TextoConsentimentoAtual.DadosSaude, req.IpOrigem, req.UserAgent);
            _db.ConsentimentosLgpd.Add(consent);
        }
        else if (!consent.Ativo)
        {
            consent.Reconceder(TextoConsentimentoAtual.DadosSaude, req.IpOrigem, req.UserAgent);
        }
    }

    await _db.SaveChangesAsync(ct);
    return Result.Success();
}
```

**DTO de entrada:**

```csharp
public sealed record AtualizarPerfilCommand(
    Guid UsuarioId,
    string? NomeCompleto,
    string? Telefone,
    string? AlergiasSeveras,
    string? RestricoesAlimentares,
    string? ObservacoesSaude,
    bool? ConsentimentoDadosSaude, // deve ser true se Alergias/Restricoes preenchidos
    string? IpOrigem,
    string? UserAgent
);
```

#### 3.3.3. Fluxo de Revogação

```csharp
// src/Application/Usuarios/Commands/RevogarConsentimento/RevogarConsentimentoCommand.cs — linha 1
public sealed record RevogarConsentimentoCommand(Guid UsuarioId, TipoConsentimento Tipo, string? Motivo) : IRequest<Result>;

public sealed class RevogarConsentimentoCommandHandler : IRequestHandler<RevogarConsentimentoCommand, Result>
{
    private readonly IAppDbContext _db;
    public RevogarConsentimentoCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(RevogarConsentimentoCommand req, CancellationToken ct)
    {
        var consent = await _db.ConsentimentosLgpd
            .FirstOrDefaultAsync(c => c.UsuarioId == req.UsuarioId && c.Tipo == req.Tipo && c.Ativo, ct);
        if (consent is null) return Result.Failure("Consentimento ativo não encontrado.");

        consent.Revogar(req.Motivo);

        // Se revogou DadosSaude, apagar dados de saúde imediatamente (art. 8º, §5º)
        if (req.Tipo == TipoConsentimento.DadosSaude)
        {
            var usuario = await _db.Usuarios.FirstAsync(u => u.Id == req.UsuarioId, ct);
            usuario.RemoverDadosSaude();
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Endpoint:**

```csharp
[HttpPost("api/usuarios/me/consentimentos/{tipo}/revogar")]
[Authorize]
public async Task<IActionResult> Revogar(string tipo, [FromBody] RevogarDto dto, CancellationToken ct)
{
    if (!Enum.TryParse<TipoConsentimento>(tipo, true, out var tipoEnum))
        return BadRequest(new { error = "Tipo inválido. Use: DadosSaude, Geolocalizacao, Marketing" });

    var cmd = new RevogarConsentimentoCommand(_currentUser.UserId!.Value, tipoEnum, dto.Motivo);
    var result = await _mediator.Send(cmd, ct);
    return result.IsSuccess ? NoContent() : BadRequest(result);
}
```

**Efeitos da revogação de `DadosSaude`:**
- `Usuario.AlergiasSeveras` e `RestricoesAlimentares` são setados para `NULL` imediatamente.
- Pedidos já criados mantêm snapshot anonimizado se necessário, mas novos pedidos não exibirão alerta de alergia ao vendedor.
- Log em `AuditLogs` com `acao = "REVOGACAO_CONSENTIMENTO"`.

---

## 4. Retenção e Descarte

| Dado | Retenção | Após retenção |
|---|---|---|
| `Usuarios` (ativos) | Enquanto conta existir | Anonimização sob demanda |
| `Usuarios` (anonimizados) | 6 meses após anonimização (para defesa jurídica) | `DELETE` físico ou `HARD_DELETE` |
| `Pedidos` + `Itens` | 5 anos (fiscal) | Arquivamento frio ou expurgo |
| `AuditLogs` | 5 anos | Arquivamento |
| `ConsentimentosLgpd` | Enquanto usuário existir + 5 anos após anonimização | Expurgo |
| `DataProtection Keys` | Enquanto houver dados criptografados com a chave | Nunca deletar antes de re-encriptar |

**Job de expurgo (opcional, via `IHostedService`):**

```csharp
// src/Infrastructure/Jobs/ExpurgoJob.cs — linha 1
public sealed class ExpurgoJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromDays(1), ct);
            // DELETE FROM "Usuarios" WHERE "IsAnonimizado" = true AND "AtualizadoEmUtc" < now() - interval '6 months'
            // DELETE FROM "Pedidos" WHERE "CriadoEmUtc" < now() - interval '5 years' AND "Status" = Finalizado
        }
    }
}
```

---

## 5. Checklist para o Agente

- [ ] Implementar `EncryptedConverter` + `ProtectorFactory` + `UsuarioConfiguration` com `HasConversion` para 3 campos sensíveis.
- [ ] Configurar `AddDataProtection().PersistKeysToFileSystem()` em `Program.cs`.
- [ ] Implementar `ConsentimentoLgpd` (entidade + tabela + `IEntityTypeConfiguration`).
- [ ] Implementar `AnonimizarUsuarioCommand` com anonimização irreversível + preservação de pedidos para 5 anos.
- [ ] Implementar `ExportarDadosPortabilidadeQuery` retornando JSON camelCase + `File()` no controller.
- [ ] Implementar `RevogarConsentimentoCommand` que apaga dados de saúde imediatamente.
- [ ] Garantir que `GET /api/usuarios/*` nunca retorna `alergias` sem `includeSensitive` + `AdminOnly`.
- [ ] Nunca logar dados sensíveis em plaintext; mascarar em Serilog.
- [ ] Criar migration que cria `text` para campos criptografados (não `varchar` limitado).
