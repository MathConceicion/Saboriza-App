# Architecture — Clean Architecture e ASP.NET Core 10 Deep Dive

> **Projeto:** Marketplace de Gastronomia Artesanal Multi-tenant (Comércio Local)
> **Stack:** C# 14 / .NET 10 / ASP.NET Core 10 / EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL / PostgreSQL 16+ / MediatR 12+ / FluentValidation 12+
> **Versão do documento:** 1.1 — 2026-09-09 — **Atualizado com Módulo Agenda/Cronograma de Produção (Aprovar ➔ Agenda atômica)**
> **Público:** Agente de codificação OpenCode + Muse Spark 1.2 — sem inferência, sem placeholders.
> **Changelog 1.1:** Adicionada entidade `ItemAgenda`, `IAppDbContext.ItensAgenda`, `ItemAgendaConfiguration`, fluxo atômico `AprovarPedidoCommandHandler` (estoque + Pedido.AprovadoEmUtc + ItemAgenda) e estrutura `Agenda/` em Application.

---

## 1. Visão Geral e Princípios Arquiteturais

### 1.1. Objetivos

1.  **Isolamento de domínio:** Regras de negócio alimentares, financeiras e geográficas vivem em C# puro, sem dependência de `Microsoft.EntityFrameworkCore`, `AspNetCore` ou `Npgsql`.
2.  **Inversão de dependência:** `Domain` não referencia ninguém. `Application` referencia apenas `Domain`. `Infrastructure` referencia `Domain` + `Application`. `WebAPI` referencia todos, mas resolve tudo via DI.
3.  **Testabilidade:** Domain e Application são 100% unit-testáveis sem banco.
4.  **Evolução:** Trocar PostgreSQL por outro provider, trocar JWT por Identity, ou trocar MediatR por outro mediator não exige reescrita de domínio.

### 1.2. Regras de Dependência (Strict)

```
Domain ───────────► (nenhuma dependência externa)
  ▲
  │
Application ─────► Domain
  ▲
  │
Infrastructure ──► Domain, Application
  ▲
  │
WebAPI ──────────► Domain, Application, Infrastructure
```

**Proibições verificáveis em build (`dotnet build` deve falhar se violadas):**

- `Domain.csproj` NÃO pode ter `PackageReference` para `Microsoft.EntityFrameworkCore*`, `MediatR`, `FluentValidation`, `Npgsql*`, `Microsoft.AspNetCore*`.
- `Application.csproj` NÃO pode referenciar `Infrastructure` nem `WebAPI`.
- `Infrastructure` NÃO pode conter lógica de negócio — apenas adaptadores (EF Core, e-mail, storage, geocoding).
- `WebAPI` NÃO pode conter regras de negócio — apenas composição, controllers thin, middlewares e filtros.

Enforce com `ArchUnitNET` ou teste de arquitetura no CI:

```csharp
// tests/ArchitectureTests/DependencyTests.cs
[Fact]
public void Domain_NaoDeve_DependerDeInfraestrutura()
{
    var domain = Types.InAssembly(typeof(Domain.Entities.Usuario).Assembly);
    var infraTypes = new[] { typeof(Microsoft.EntityFrameworkCore.DbContext).FullName!, "Npgsql", "MediatR" };
    // assert
}
```

---

## 2. Estrutura de Pastas Física — Árvore Completa da Solution

### 2.1. Arquivo `.slnx` e estrutura de diretórios no disco

```
/
├── GastronomiaMarketplace.slnx
├── .editorconfig
├── .gitignore
├── docker-compose.yml
├── docker-compose.override.yml
├── Dockerfile
├── README.md
├── docs/                           # <- esta documentação
│   ├── architecture.md
│   ├── auth_roles.md
│   ├── security_lgpd.md
│   ├── secure_coding_practices.md
│   ├── features_spec.md
│   └── database_concurrency.md
├── src/
│   ├── Domain/
│   │   ├── GastronomiaMarketplace.Domain.csproj
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── DomainEvent.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   ├── Result.cs
│   │   │   └── Error.cs
│   │   ├── Enums/
│   │   │   ├── PapelUsuario.cs
│   │   │   ├── StatusPedido.cs
│   │   │   ├── StatusLoja.cs
│   │   │   └── StatusProduto.cs
│   │   ├── ValueObjects/
│   │   │   ├── Cpf.cs
│   │   │   ├── Email.cs
│   │   │   ├── Dinheiro.cs          // Value Object Preço/Monetário
│   │   │   ├── Endereco.cs
│   │   │   ├── Coordenada.cs        // Latitude/Longitude imutável
│   │   │   ├── RestricaoAlimentar.cs
│   │   │   └── HorarioFuncionamento.cs
│   │   ├── Entities/
│   │   │   ├── Usuario.cs
│   │   │   ├── Vendedor.cs
│   │   │   ├── Loja.cs
│   │   │   ├── CategoriaLoja.cs
│   │   │   ├── Produto.cs
│   │   │   ├── Pedido.cs
│   │   │   ├── ItemPedido.cs
│   │   │   ├── ItemAgenda.cs
│   │   │   ├── Comissao.cs
│   │   │   └── ConsentimentoLgpd.cs
│   │   ├── Events/
│   │   │   ├── PedidoCriadoEvent.cs
│   │   │   ├── PedidoAprovadoEvent.cs
│   │   │   └── ContaUpgradedParaVendedorEvent.cs
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   └── RegraDeNegocioException.cs
│   │   └── Interfaces/              // Portas que Infrastructure implementa
│   │       ├── IAggregateRoot.cs
│   │       └── ISoftDeletable.cs
│   │
│   ├── Application/
│   │   ├── GastronomiaMarketplace.Application.csproj
│   │   ├── DependencyInjection.cs
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── PerformanceBehavior.cs
│   │   │   │   └── TransactionBehavior.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── ValidationException.cs
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   └── ForbiddenException.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAppDbContext.cs
│   │   │   │   ├── ICurrentUser.cs
│   │   │   │   ├── ITokenService.cs
│   │   │   │   ├── IGeocodingService.cs
│   │   │   │   └── IClock.cs
│   │   │   ├── Models/
│   │   │   │   ├── PaginatedList.cs
│   │   │   │   └── Result.cs
│   │   │   └── Mappings/
│   │   │       └── MappingProfile.cs
│   │   ├── Pedidos/
│   │   │   ├── Commands/
│   │   │   │   ├── CriarPedido/
│   │   │   │   │   ├── CriarPedidoCommand.cs
│   │   │   │   │   ├── CriarPedidoCommandHandler.cs
│   │   │   │   │   └── CriarPedidoCommandValidator.cs
│   │   │   │   └── AprovarPedido/
│   │   │   │       ├── AprovarPedidoCommand.cs
│   │   │   │       ├── AprovarPedidoCommandHandler.cs
│   │   │   │       └── AprovarPedidoCommandValidator.cs
│   │   │   └── Queries/
│   │   │       ├── ListarPedidos/
│   │   │       └── ObterPedidoPorId/
│   │   ├── Agenda/
│   │   │   ├── Commands/
│   │   │   │   ├── AvancarStatusAgenda/
│   │   │   │   └── ReordenarAgenda/
│   │   │   └── Queries/
│   │   │       ├── ListarAgendaDoDia/
│   │   │       └── ObterResumoAgenda/
│   │   ├── Produtos/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Lojas/
│   │   │   ├── Commands/
│   │   │   │   └── UpgradeParaVendedor/
│   │   │   └── Queries/
│   │   │       └── ListarLojasProximas/
│   │   └── Vendedores/
│   │       └── Commands/
│   │
│   ├── Infrastructure/
│   │   ├── GastronomiaMarketplace.Infrastructure.csproj
│   │   ├── DependencyInjection.cs
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── AppDbContextFactory.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── UsuarioConfiguration.cs
│   │   │   │   ├── VendedorConfiguration.cs
│   │   │   │   ├── LojaConfiguration.cs
│   │   │   │   ├── ProdutoConfiguration.cs
│   │   │   │   ├── PedidoConfiguration.cs
│   │   │   │   ├── ItemPedidoConfiguration.cs
│   │   │   │   ├── ItemAgendaConfiguration.cs
│   │   │   │   └── AuditLogConfiguration.cs
│   │   │   ├── Interceptors/
│   │   │   │   ├── AuditableEntityInterceptor.cs
│   │   │   │   ├── SoftDeleteInterceptor.cs
│   │   │   │   └── DomainEventInterceptor.cs
│   │   │   ├── Migrations/                     // dotnet ef migrations add
│   │   │   │   └── 20260909_Initial.cs
│   │   │   └── Repositories/
│   │   │       └── (opcional, se não usar DbContext direto)
│   │   ├── Identity/
│   │   │   ├── TokenService.cs
│   │   │   └── PasswordHasher.cs
│   │   ├── Geolocation/
│   │   │   ├── PostgisGeocodingService.cs
│   │   │   └── HaversineCalculator.cs
│   │   ├── Payments/
│   │   │   └── PixFicticioService.cs
│   │   └── Security/
│   │       ├── DataProtection/
│   │       │   └── LgpdColumnEncryptionConverter.cs
│   │       └── Sanitization/
│   │           └── HtmlSanitizerService.cs
│   │
│   └── WebAPI/
│       ├── GastronomiaMarketplace.WebAPI.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── LojasController.cs
│       │   ├── ProdutosController.cs
│       │   ├── PedidosController.cs
│       │   ├── VendedorController.cs
│       │   └── AdminController.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs
│       │   ├── AuthenticationExtensions.cs
│       │   ├── AuthorizationExtensions.cs
│       │   ├── RateLimitingExtensions.cs
│       │   └── CorsExtensions.cs
│       ├── Middlewares/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── RequestLoggingMiddleware.cs
│       │   └── AuditMiddleware.cs
│       ├── Policies/
│       │   ├── VendorOwnerRequirement.cs
│       │   ├── VendorOwnerHandler.cs
│       │   └── AdminOnlyRequirement.cs
│       ├── Behaviors/
│       │   └── ValidateModelAttribute.cs
│       └── DTOs/
│           └── (evitar duplicação; preferir Application.Contracts)
│
└── tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    ├── Infrastructure.Tests/
    └── WebAPI.IntegrationTests/
```

### 2.2. Conteúdo dos `.csproj` (referências exatas)

**Domain.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

**Application.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="FluentValidation" Version="12.0.1" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.0.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Domain\GastronomiaMarketplace.Domain.csproj" />
  </ItemGroup>
</Project>
```

**Infrastructure.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="10.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="10.0.0" />
    <PackageReference Include="Ganss.XSS" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Domain\GastronomiaMarketplace.Domain.csproj" />
    <ProjectReference Include="..\Application\GastronomiaMarketplace.Application.csproj" />
  </ItemGroup>
</Project>
```

**WebAPI.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="12.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="10.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Application\GastronomiaMarketplace.Application.csproj" />
    <ProjectReference Include="..\Infrastructure\GastronomiaMarketplace.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

---

## 3. Responsabilidades de Camada — Fluxo de Dependência Invertido

### 3.1. Domain — Puro, Rico e Isolado

**Regra de ouro:** Nenhum `using Microsoft.EntityFrameworkCore` dentro de `src/Domain`.

**BaseEntity e AggregateRoot:**

```csharp
// src/Domain/Common/BaseEntity.cs — linha 1
namespace GastronomiaMarketplace.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CriadoEmUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEmUtc { get; protected set; }
    public bool IsDeleted { get; private set; }

    public void MarcarComoDeletado() => IsDeleted = true;
}

public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**Rich Domain Model — exemplo `Pedido.cs` (fragmento):**

```csharp
namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Events;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class Pedido : AggregateRoot
{
    public Guid ClienteId { get; private set; }
    public Guid LojaId { get; private set; }
    public StatusPedido Status { get; private set; }
    public Dinheiro ValorBruto { get; private set; } = Dinheiro.Zero("BRL");
    public Dinheiro TaxaComissao { get; private set; } = Dinheiro.Zero("BRL");
    public Dinheiro ValorLiquidoVendedor { get; private set; } = Dinheiro.Zero("BRL");
    public DateTime? AprovadoEmUtc { get; private set; }

    private readonly List<ItemPedido> _itens = [];
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    private Pedido() { }

    private Pedido(Guid clienteId, Guid lojaId, List<ItemPedido> itens)
    {
        ClienteId = clienteId;
        LojaId = lojaId;
        Status = StatusPedido.Pendente;
        _itens = itens;
        RecalcularTotais();
        AddDomainEvent(new PedidoCriadoEvent(Id, LojaId, ClienteId));
    }

    public static Pedido Criar(Guid clienteId, Guid lojaId, List<ItemPedido> itens)
    {
        if (itens.Count == 0) throw new RegraDeNegocioException("Pedido deve conter ao menos um item.");
        if (itens.Any(i => !i.ProdutoAtivo)) throw new RegraDeNegocioException("Produto inativo não pode ser pedido.");
        return new Pedido(clienteId, lojaId, itens);
    }

    public void Aprovar(Guid atorVendorId, DateTime aprovadoEmUtc)
    {
        if (Status != StatusPedido.Pendente)
            throw new RegraDeNegocioException($"Transição inválida: {Status} -> EmPreparo.");
        Status = StatusPedido.EmPreparo;
        AprovadoEmUtc = aprovadoEmUtc;
        AtualizadoEmUtc = aprovadoEmUtc;
        AddDomainEvent(new PedidoAprovadoEvent(Id, LojaId, aprovadoEmUtc));
    }

    public void MarcarProntoParaRetirada()
    {
        if (Status != StatusPedido.EmPreparo)
            throw new RegraDeNegocioException($"Transição inválida: {Status} -> ProntoParaRetirada.");
        Status = StatusPedido.ProntoParaRetirada;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Finalizar()
    {
        if (Status != StatusPedido.ProntoParaRetirada)
            throw new RegraDeNegocioException($"Transição inválida: {Status} -> Finalizado.");
        Status = StatusPedido.Finalizado;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    private void RecalcularTotais()
    {
        ValorBruto = new Dinheiro(_itens.Sum(i => i.Subtotal.Valor), "BRL");
    }
}
```

**Entidade `ItemAgenda.cs` — Agenda de Produção:**

```csharp
namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class ItemAgenda : BaseEntity
{
    public Guid PedidoId { get; private set; }
    public Guid LojaId { get; private set; }
    public Guid VendedorId { get; private set; }
    public StatusPedido Status { get; private set; }
    public DateTime AprovadoEmUtc { get; private set; }
    public DateTime? IniciadoEmUtc { get; private set; }
    public DateTime? ProntoEmUtc { get; private set; }

    private ItemAgenda() { }

    private ItemAgenda(Guid pedidoId, Guid lojaId, Guid vendedorId, DateTime aprovadoEmUtc)
    {
        PedidoId = pedidoId;
        LojaId = lojaId;
        VendedorId = vendedorId;
        Status = StatusPedido.EmPreparo;
        AprovadoEmUtc = aprovadoEmUtc;
        CriadoEmUtc = aprovadoEmUtc;
    }

    public static ItemAgenda Criar(Guid pedidoId, Guid lojaId, Guid vendedorId, DateTime aprovadoEmUtc)
    {
        if (pedidoId == Guid.Empty) throw new RegraDeNegocioException("PedidoId obrigatório para agenda.");
        if (lojaId == Guid.Empty) throw new RegraDeNegocioException("LojaId obrigatório para agenda.");
        return new ItemAgenda(pedidoId, lojaId, vendedorId, aprovadoEmUtc);
    }

    public void AvancarParaProntoRetirada(DateTime prontoEmUtc)
    {
        if (Status != StatusPedido.EmPreparo)
            throw new RegraDeNegocioException($"Agenda só avança de EmPreparo para ProntoParaRetirada. Atual: {Status}");
        Status = StatusPedido.ProntoParaRetirada;
        ProntoEmUtc = prontoEmUtc;
        AtualizadoEmUtc = prontoEmUtc;
    }

    public void Finalizar(DateTime finalizadoEmUtc)
    {
        if (Status != StatusPedido.ProntoParaRetirada)
            throw new RegraDeNegocioException($"Agenda só finaliza de ProntoParaRetirada. Atual: {Status}");
        Status = StatusPedido.Finalizado;
        AtualizadoEmUtc = finalizadoEmUtc;
    }
}
```

**Value Object `Dinheiro.cs`:**

```csharp
namespace GastronomiaMarketplace.Domain.ValueObjects;

public sealed class Dinheiro : ValueObject
{
    public decimal Valor { get; }
    public string Moeda { get; }

    public Dinheiro(decimal valor, string moeda)
    {
        if (valor < 0) throw new ArgumentException("Valor não pode ser negativo.", nameof(valor));
        if (string.IsNullOrWhiteSpace(moeda)) throw new ArgumentException("Moeda obrigatória.", nameof(moeda));
        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        Moeda = moeda.ToUpperInvariant();
    }

    public static Dinheiro Zero(string moeda) => new(0m, moeda);
    public Dinheiro Somar(Dinheiro outro) => Moeda == outro.Moeda ? new(Valor + outro.Valor, Moeda) : throw new InvalidOperationException("Moedas diferentes.");
    public Dinheiro Multiplicar(decimal fator) => new(Valor * fator, Moeda);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Moeda;
    }
}
```

**Por que `Domain` não referencia EF Core?** Porque configurações de persistência (`HasConversion`, `IsRowVersion`, `geography`) ficam em `Infrastructure/Persistence/Configurations`. O domínio expõe propriedades puras; a infraestrutura mapeia para colunas Postgres.

### 3.2. Application — Orquestração e Casos de Uso

- Contém **Commands/Queries** (CQRS), **Handlers**, **Validators** (FluentValidation), **Behaviors** (pipeline MediatR), **Interfaces** (portas).
- Não conhece `DbContext` concreto — conhece `IAppDbContext`.
- Não conhece `HttpContext` — conhece `ICurrentUser`.

```csharp
namespace GastronomiaMarketplace.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Domain.Entities;

public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Vendedor> Vendedores { get; }
    DbSet<Loja> Lojas { get; }
    DbSet<CategoriaLoja> CategoriasLoja { get; }
    DbSet<Produto> Produtos { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<ItemPedido> ItensPedido { get; }
    DbSet<ItemAgenda> ItensAgenda { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

### 3.3. Infrastructure — Adaptadores

- Implementa `IAppDbContext` via `AppDbContext : DbContext, IAppDbContext`.
- Configura Npgsql + NetTopologySuite + criptografia de coluna + interceptors.
- Implementa `ITokenService`, `IGeocodingService`, sanitização.

### 3.4. WebAPI — Composição

- Registra DI, middlewares, autenticação, autorização, rate limiting, Swagger, health checks.
- Controllers são **thin**: validam rota, chamam `mediator.Send`, retornam `IResult`/`ActionResult`.
- Nenhuma regra de negócio aqui.

**Fluxo de uma requisição (ex: `POST /api/pedidos/{id}/aprovar`):**

```
HTTP Request
  → ExceptionHandlingMiddleware (captura ValidationException → 400, NotFound → 404, Forbidden → 403, DbUpdateConcurrencyException → 409)
  → Authentication (JWT Bearer)
  → Authorization (Policy: VendorOwnerOrAdmin)
  → RateLimiting
  → Controller (PedidosController.Aprovar)
    → mediator.Send(new AprovarPedidoCommand(...))
      → ValidationBehavior (FluentValidation)
      → TransactionBehavior (abre transação se Command)
      → AprovarPedidoCommandHandler (carrega Pedido, debita estoque com xmin, cria ItemAgenda, chama pedido.Aprovar(agora), SaveChangesAsync atômico)
        → AuditableEntityInterceptor + DomainEventInterceptor
      → LoggingBehavior
  → Response 200 { pedidoId, novoStatus, agendaId, aprovadoEmUtc }
```

---

## 4. Implementação de CQRS com MediatR — Exemplo Completo: AprovarPedido

### 4.1. Pré-requisitos de registro (DI)

```csharp
// src/Application/DependencyInjection.cs — linha 1
namespace GastronomiaMarketplace.Application;

using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using GastronomiaMarketplace.Application.Common.Behaviors;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        // Ordem importa: Validation → Logging → Transaction → Handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}
```

```csharp
// src/Infrastructure/DependencyInjection.cs — linha 1 (fragmento)
services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseNpgsql(
        configuration.GetConnectionString("Default"),
        npgsql => npgsql.UseNetTopologySuite() // para geography(Point)
    );
    opts.AddInterceptors(
        sp.GetRequiredService<AuditableEntityInterceptor>(),
        sp.GetRequiredService<DomainEventInterceptor>()
    );
});
```

### 4.2. Command (contrato de entrada)

```csharp
// src/Application/Pedidos/Commands/AprovarPedido/AprovarPedidoCommand.cs — linha 1
namespace GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record AprovarPedidoCommand(
    Guid PedidoId,
    Guid AtorUsuarioId,   // extraído do JWT via ICurrentUser
    Guid? AtorVendorId,   // null se não for vendedor; preenchido via ICurrentUser
    bool IsAdmin          // true se claim role=Admin
) : IRequest<Result<Guid>>; // retorna Id do pedido aprovado

// Result<T> simples (Application)
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error) => new(false, default, error, null);
}
```

**Por que `AtorUsuarioId`/`AtorVendorId` no Command e não dentro do Handler via `ICurrentUser`?**
Para manter o Handler puro e testável (sem `HttpContext`). O Controller extrai do `ICurrentUser` e passa explicitamente. Alternativa válida é injetar `ICurrentUser` direto no Handler — ambas são aceitas, mas este doc padroniza **passagem explícita** para rastreabilidade em logs.

### 4.3. Validator (FluentValidation)

```csharp
// src/Application/Pedidos/Commands/AprovarPedido/AprovarPedidoCommandValidator.cs — linha 1
namespace GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

using FluentValidation;

public sealed class AprovarPedidoCommandValidator : AbstractValidator<AprovarPedidoCommand>
{
    public AprovarPedidoCommandValidator()
    {
        RuleFor(x => x.PedidoId)
            .NotEmpty().WithMessage("PedidoId é obrigatório.")
            .Must(id => id != Guid.Empty).WithMessage("PedidoId inválido.");

        RuleFor(x => x.AtorUsuarioId)
            .NotEmpty().WithMessage("Usuário autenticado é obrigatório.");

        // Regra de negócio de autorização NÃO vai no validator — vai no Handler/Policy.
        // Validator cuida apenas de forma (shape) do contrato.

        // Se quiser validar que ao menos um papel autoriza:
        RuleFor(x => x)
            .Must(cmd => cmd.IsAdmin || cmd.AtorVendorId.HasValue)
            .WithMessage("Apenas vendedores ou administradores podem aprovar pedidos.")
            .WithName("Autorização");
    }
}
```

### 4.4. Handler — Com Agenda Atômica

> **Regra nova:** `Aprovar` executa 3 mutações atômicas na mesma transação: `Produto.QuantidadeEmEstoque` (xmin) + `Pedido.Status/AprovadoEmUtc` + `INSERT ItemAgenda`. Se qualquer falhar, rollback total.

```csharp
namespace GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed class AprovarPedidoCommandHandler : IRequestHandler<AprovarPedidoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<AprovarPedidoCommandHandler> _logger;
    private readonly IClock _clock;

    public AprovarPedidoCommandHandler(IAppDbContext db, ILogger<AprovarPedidoCommandHandler> logger, IClock clock)
    {
        _db = db;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(AprovarPedidoCommand request, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Itens)
            .Include(p => p.Loja)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoId, ct);

        if (pedido is null)
            return Result<Guid>.Failure($"Pedido {request.PedidoId} não encontrado.");

        if (!request.IsAdmin)
        {
            if (request.AtorVendorId is null)
                return Result<Guid>.Failure("Usuário não possui vínculo de vendedor.");
            if (pedido.Loja.VendedorId != request.AtorVendorId.Value)
                return Result<Guid>.Failure("Vendedor não autorizado a aprovar pedidos de outra loja.");
        }

        if (pedido.Status != StatusPedido.Pendente)
            return Result<Guid>.Failure($"Pedido não está pendente. Status atual: {pedido.Status}.");

        var produtoIds = pedido.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var item in pedido.Itens)
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            if (!produto.Ativo)
                return Result<Guid>.Failure($"Produto '{produto.Nome}' está inativo.");
            if (produto.QuantidadeEmEstoque < item.Quantidade)
                return Result<Guid>.Failure($"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.QuantidadeEmEstoque}, solicitado: {item.Quantidade}.");
            produto.DebitarEstoque(item.Quantidade);
        }

        var agora = _clock.UtcNow;
        pedido.Aprovar(request.AtorVendorId ?? pedido.Loja.VendedorId, agora);

        var jaNaAgenda = await _db.ItensAgenda.AnyAsync(a => a.PedidoId == pedido.Id, ct);
        if (jaNaAgenda)
            return Result<Guid>.Failure("Pedido já está na agenda de produção.");

        var itemAgenda = ItemAgenda.Criar(
            pedidoId: pedido.Id,
            lojaId: pedido.LojaId,
            vendedorId: pedido.Loja.VendedorId,
            aprovadoEmUtc: agora
        );
        _db.ItensAgenda.Add(itemAgenda);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito de concorrência ao aprovar pedido {PedidoId}", request.PedidoId);
            return Result<Guid>.Failure("Conflito de concorrência: estoque foi modificado por outro processo. Tente novamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("IX_ItensAgenda_PedidoId"))
        {
            _logger.LogWarning(ex, "Pedido {PedidoId} já inserido na agenda por requisição concorrente", request.PedidoId);
            return Result<Guid>.Failure("Pedido já foi aprovado por outro processo.");
        }

        _logger.LogInformation("Pedido {PedidoId} aprovado e inserido na agenda {AgendaId} por {Ator}", pedido.Id, itemAgenda.Id, request.AtorUsuarioId);
        return Result<Guid>.Success(pedido.Id);
    }
}
```

**Métodos de domínio envolvidos:**

```csharp
namespace GastronomiaMarketplace.Domain.Entities;

public void DebitarEstoque(int quantidade)
{
    if (quantidade <= 0) throw new RegraDeNegocioException("Quantidade deve ser positiva.");
    if (QuantidadeEmEstoque < quantidade) throw new RegraDeNegocioException($"Estoque insuficiente. Disponível: {QuantidadeEmEstoque}.");
    QuantidadeEmEstoque -= quantidade;
    AtualizadoEmUtc = DateTime.UtcNow;
}

public void Aprovar(Guid atorVendorId, DateTime aprovadoEmUtc)
{
    if (Status != StatusPedido.Pendente)
        throw new RegraDeNegocioException($"Transição inválida: {Status} -> EmPreparo.");
    Status = StatusPedido.EmPreparo;
    AprovadoEmUtc = aprovadoEmUtc;
    AtualizadoEmUtc = aprovadoEmUtc;
    AddDomainEvent(new PedidoAprovadoEvent(Id, LojaId, aprovadoEmUtc));
}
```

### 4.5. Pipeline Behaviors

**ValidationBehavior — executa todos os validators antes do handler:**

```csharp
// src/Application/Common/Behaviors/ValidationBehavior.cs — linha 1
namespace GastronomiaMarketplace.Application.Common.Behaviors;

using FluentValidation;
using MediatR;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures); // capturada pelo middleware → 400

        return await next();
    }
}
```

**TransactionBehavior — garante atomicidade para Commands (opcional, mas recomendado):**

```csharp
// src/Application/Common/Behaviors/TransactionBehavior.cs — linha 1
namespace GastronomiaMarketplace.Application.Common.Behaviors;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAppDbContext _db;

    public TransactionBehavior(IAppDbContext db) => _db = db;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Apenas Commands (que alteram estado) — Queries não precisam de transação
        var isCommand = request.GetType().Name.EndsWith("Command");
        if (!isCommand) return await next();

        // Se o DbContext já estiver em transação (ex: handler aninhado), não abre outra
        if (_db is DbContext ef && ef.Database.CurrentTransaction is not null)
            return await next();

        await using var tx = await ((DbContext)_db).Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await tx.CommitAsync(ct);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
```

### 4.6. Tratamento Global de Exceções via Middleware

```csharp
// src/WebAPI/Middlewares/ExceptionHandlingMiddleware.cs — linha 1
namespace GastronomiaMarketplace.WebAPI.Middlewares;

using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Exceptions;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validação falhou: {Errors}", ex.Errors);
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "https://httpstatuses.com/400",
                title = "Erro de validação",
                status = 400,
                errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (NotFoundException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito de concorrência");
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict; // 409
            await ctx.Response.WriteAsJsonAsync(new { error = "Conflito de concorrência. Recarregue e tente novamente.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { error = "Erro interno. Tente novamente mais tarde." });
        }
    }
}
```

Registro em `Program.cs`:

```csharp
// src/WebAPI/Program.cs — linha 1 (fragmento)
var builder = WebApplication.CreateBuilder(args);
// ...
var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();
```

### 4.7. Controller Thin

```csharp
// src/WebAPI/Controllers/PedidosController.cs — linha 1
namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;
using GastronomiaMarketplace.Application.Common.Interfaces;

[ApiController]
[Route("api/pedidos")]
public sealed class PedidosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public PedidosController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("{id:guid}/aprovar")]
    [Authorize(Policy = "VendorOwnerOrAdmin")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        var cmd = new AprovarPedidoCommand(
            PedidoId: id,
            AtorUsuarioId: _currentUser.UserId!.Value,
            AtorVendorId: _currentUser.VendorId,
            IsAdmin: _currentUser.IsAdmin
        );

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess
            ? Ok(new { pedidoId = result.Value, status = "EmPreparo" })
            : BadRequest(new { error = result.Error });
    }
}
```

---

## 5. Migrations e Versionamento

```bash
# Criar migration inicial
dotnet ef migrations add Initial --project src/Infrastructure --startup-project src/WebAPI --output-dir Persistence/Migrations

# Aplicar
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI

# Em produção: app.ApplyMigrations() via extension no Program.cs (com retry)
```

```csharp
// src/WebAPI/Extensions/MigrationExtensions.cs — linha 1
public static async Task ApplyMigrationsAsync(this WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var strategy = db.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () => await db.Database.MigrateAsync());
}
```

---

## 6. Observabilidade e Convenções

- **Serilog** com sink para PostgreSQL ou OpenTelemetry.
- **Health checks:** `builder.Services.AddHealthChecks().AddNpgSql(connectionString)`.
- **Swagger/OpenAPI:** `AddEndpointsApiExplorer + AddSwaggerGen` com `AddSecurityDefinition("Bearer", ...)`.
- **C# 14 features obrigatórias:** `required` members, `file-scoped namespaces`, `primary constructors` onde fizer sentido, `collection expressions` (`[]`), `raw string literals` para SQL.

---

## 7. Checklist de Implementação para o Agente

- [ ] Criar solution e 4 projetos com referências conforme §2.2.
- [ ] Implementar `BaseEntity`, `AggregateRoot`, `ValueObject`, `Dinheiro`, `Endereco`, `Coordenada`, `ItemAgenda`.
- [ ] Implementar `IAppDbContext` com `ItensAgenda` + `AppDbContext` + Configurations (`ItemAgendaConfiguration`) + Interceptors.
- [ ] Registrar MediatR + FluentValidation + Behaviors em `Application.DependencyInjection`.
- [ ] Implementar `AprovarPedidoCommand` + Validator + Handler exatamente como §4.4 com `IClock` + criação atômica de `ItemAgenda` + tratamento `DbUpdateConcurrencyException` e UNIQUE.
- [ ] Implementar `ItemAgenda.AvancarParaProntoRetirada/Finalizar` + `ListarAgendaDoDia` ordenado por `AprovadoEmUtc`.
- [ ] Implementar `ExceptionHandlingMiddleware` e registrar antes de `UseAuthentication`.
- [ ] Criar `PedidosController` thin + `AgendaController` (`GET /api/vendedor/agenda`, `PATCH /avancar`).
- [ ] Rodar `dotnet ef migrations add Initial` e validar que `xmin`, `geography` e `ItensAgenda`/`AprovadoEmUtc` aparecem no snapshot.

> **Regra de ouro deste documento:** Se o agente precisar "adivinhar" qualquer regra de negócio, a especificação falhou. Todo comportamento acima é normativo e deve ser implementado literalmente.
