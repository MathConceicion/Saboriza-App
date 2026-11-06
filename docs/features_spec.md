# Features Spec — Especificações Técnicas de Funcionalidades Locais e Fluxos

> **Foco:** Busca por proximidade (estilo Trinks) + Categorias + Motor de comissões e PIX fictício + **Agenda/Cronograma de Produção**
> **Stack:** EF Core 10 + Npgsql + NetTopologySuite + PostgreSQL PostGIS (`geography`)
> **Versão:** 1.1 — 2026-09-09 — **Atualizado com Módulo Agenda (Aprovação ➔ Entrada na Agenda)**
> **Changelog 1.1:** Nova seção 4 com entidade `ItemAgenda`, fluxo de automação atômico, queries `ListarAgendaDoDia`, endpoints `/api/vendedor/agenda` e ordenação cronológica.

---

## 1. Busca por Proximidade Geográfica — Estilo Trinks

### 1.1. Requisito de Negócio

- Cliente informa **endereço atual ou CEP** na home (ex: "Av. Paulista, 1000" ou "01310-100").
- Sistema **geocodifica** o endereço para `lat/lng` (via serviço externo ou cache local).
- Lista **lojas ordenadas por distância** (mais próximas primeiro), com distância em km exibida.
- Filtros opcionais: `categoria` (ex: Pizzarias), `raioKm` (padrão 5km, máx 50km), `apenasAberto` (horário funcionamento).
- Paginação: `page`/`pageSize` (default 20, máx 50).

**Inspiração Trinks:** O Trinks (marketplace de beleza) pede CEP e mostra salões por distância. Aqui, mesma UX para gastronomia artesanal.

### 1.2. Modelagem Espacial — Por que `geography` e não `geometry`?

| Tipo | Sistema | Unidade | Cálculo | Uso |
|---|---|---|---|---|
| `geometry(Point, 4326)` | Plano cartesiano | graus | Euclidiano (inexato para distâncias reais) | Mapas projetados |
| `geography(Point, 4326)` | Esferoide WGS84 | metros | Geodésico (Haversine/Andoyer) | **Distâncias reais na Terra** |

Este projeto usa **`geography(Point, 4326)`** para que `ST_Distance` retorne metros com precisão geodésica, sem precisar implementar Haversine manualmente em todos os lugares. Haversine continua documentado como fallback para queries sem PostGIS.

### 1.3. Entidade `Loja` — localização

```csharp
// src/Domain/Entities/Loja.cs — linha 1 (fragmento)
using NetTopologySuite.Geometries;

public sealed class Loja : AggregateRoot
{
    public Guid VendedorId { get; private set; }
    public string Nome { get; private set; } = null!;
    public Guid CategoriaLojaId { get; private set; }
    public CategoriaLoja CategoriaLoja { get; private set; } = null!;
    public string EnderecoComercial { get; private set; } = null!;
    public string Cep { get; private set; } = null!;
    public Point Localizacao { get; private set; } = null!; // geography(Point,4326) — X=lng, Y=lat
    public bool Ativo { get; private set; }
    public bool Aprovado { get; private set; } // moderação Admin
    public string? Descricao { get; private set; }
    public string? HorarioFuncionamentoJson { get; private set; } // ex: {"seg":["08:00-18:00"],...}
    public decimal TaxaComissaoPercentual { get; private set; } // override por loja, ou null usa global

    private Loja() { }

    public Loja(Guid vendedorId, string nome, Guid categoriaId, string endereco, string cep, Point localizacao)
    {
        VendedorId = vendedorId;
        Nome = nome;
        CategoriaLojaId = categoriaId;
        EnderecoComercial = endereco;
        Cep = cep;
        Localizacao = localizacao;
        Ativo = true;
        Aprovado = false; // Admin aprova
    }

    public void Aprovar() { Aprovado = true; AtualizadoEmUtc = DateTime.UtcNow; }
    public void Rejeitar() { Aprovado = false; Ativo = false; AtualizadoEmUtc = DateTime.UtcNow; }
}
```

**Configuration:**

```csharp
// src/Infrastructure/Persistence/Configurations/LojaConfiguration.cs — linha 1
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using GastronomiaMarketplace.Domain.Entities;

public sealed class LojaConfiguration : IEntityTypeConfiguration<Loja>
{
    public void Configure(EntityTypeBuilder<Loja> b)
    {
        b.ToTable("Lojas");
        b.HasKey(l => l.Id);
        b.Property(l => l.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(l => l.Nome).HasColumnType("varchar(150)").IsRequired();
        b.Property(l => l.EnderecoComercial).HasColumnType("varchar(500)").IsRequired();
        b.Property(l => l.Cep).HasColumnType("varchar(9)").IsRequired();
        b.Property(l => l.Localizacao)
            .HasColumnType("geography (point)") // Postgres + PostGIS
            .IsRequired();
        b.HasIndex(l => l.Localizacao).HasMethod("GIST"); // índice espacial para ST_DWithin
        b.Property(l => l.Ativo).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(l => l.Aprovado).HasColumnType("boolean").HasDefaultValue(false);
        b.HasOne(l => l.CategoriaLoja).WithMany(c => c.Lojas).HasForeignKey(l => l.CategoriaLojaId);
        b.HasOne<Vendedor>().WithOne(v => v.Loja).HasForeignKey<Loja>(l => l.VendedorId);
        b.Property(l => l.HorarioFuncionamentoJson).HasColumnType("jsonb");
        b.HasQueryFilter(l => l.Ativo && l.Aprovado); // apenas lojas ativas/aprovadas aparecem para clientes
    }
}
```

**Migração — habilitar PostGIS:**

```sql
-- Migrations/20260909_Initial.cs (fragmento)
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE TABLE "Lojas" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "VendedorId" uuid NOT NULL UNIQUE REFERENCES "Vendedores"("Id") ON DELETE CASCADE,
    "Nome" varchar(150) NOT NULL,
    "Localizacao" geography(point, 4326) NOT NULL,
    ...
);
CREATE INDEX "IX_Lojas_Localizacao" ON "Lojas" USING GIST ("Localizacao");
```

### 1.4. Lógica Matemática — Fórmula de Haversine (fallback e documentação)

Quando PostGIS não está disponível ou para cálculo em memória, usar Haversine:

```
a = sin²(Δφ/2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ/2)
c = 2 ⋅ atan2(√a, √(1−a))
d = R ⋅ c   onde R = 6371.0088 km (raio médio da Terra)
```

`φ` = latitude em radianos, `λ` = longitude em radianos.

```csharp
// src/Infrastructure/Geolocation/HaversineCalculator.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Geolocation;

public static class HaversineCalculator
{
    private const double RaioTerraKm = 6371.0088;

    public static double CalcularKm(double lat1, double lon1, double lat2, double lon2)
    {
        double ToRad(double deg) => deg * Math.PI / 180.0;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var rLat1 = ToRad(lat1);
        var rLat2 = ToRad(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return RaioTerraKm * c;
    }

    // Para LINQ to Objects (pós-carregamento) — não traduz para SQL
    public static double CalcularKm((double Lat, double Lng) origem, (double Lat, double Lng) destino)
        => CalcularKm(origem.Lat, origem.Lng, destino.Lat, destino.Lng);
}
```

**Aviso:** Haversine em LINQ `Where` que roda no banco **não é traduzível** para SQL sem função mapeada. Use PostGIS (`ST_DWithin`/`ST_Distance`) para queries no banco; Haversine apenas para validação em memória ou testes.

### 1.5. Query Otimizada — `ListarLojasProximas`

#### 1.5.1. Contrato

```csharp
// src/Application/Lojas/Queries/ListarLojasProximas/ListarLojasProximasQuery.cs — linha 1
namespace GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasProximas;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarLojasProximasQuery(
    double Latitude,      // lat do cliente (geocodificado)
    double Longitude,     // lng do cliente
    double RaioKm = 5.0,  // padrão 5km
    Guid? CategoriaLojaId = null,
    int Pagina = 1,
    int TamanhoPagina = 20
) : IRequest<PaginatedList<LojaProximaDto>>;

public sealed record LojaProximaDto(
    Guid LojaId,
    string Nome,
    string Categoria,
    string Endereco,
    double DistanciaKm,   // calculado
    double Latitude,
    double Longitude,
    bool AbertoAgora
);
```

#### 1.5.2. Handler — PostGIS (`ST_DWithin` + `ST_Distance`) — RECOMENDADO

```csharp
// src/Application/Lojas/Queries/ListarLojasProximas/ListarLojasProximasQueryHandler.cs — linha 1
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed class ListarLojasProximasQueryHandler : IRequestHandler<ListarLojasProximasQuery, PaginatedList<LojaProximaDto>>
{
    private readonly IAppDbContext _db;

    public ListarLojasProximasQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedList<LojaProximaDto>> Handle(ListarLojasProximasQuery req, CancellationToken ct)
    {
        if (req.RaioKm <= 0 || req.RaioKm > 50)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("RaioKm", "Raio deve estar entre 0 e 50 km.")]);

        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var pontoCliente = geometryFactory.CreatePoint(new Coordinate(req.Longitude, req.Latitude)); // X=lng, Y=lat!

        var raioMetros = req.RaioKm * 1000;

        // Query traduzida para SQL PostGIS:
        // SELECT ..., ST_Distance("Localizacao", @pontoCliente) / 1000 AS DistanciaKm
        // FROM "Lojas"
        // WHERE ST_DWithin("Localizacao", @pontoCliente, @raioMetros) AND "Ativo" AND "Aprovado"
        // ORDER BY "Localizacao" <-> @pontoCliente  (operador KNN — usa índice GIST)
        var query = _db.Lojas
            .AsNoTracking()
            .Include(l => l.CategoriaLoja)
            .Where(l => l.Ativo && l.Aprovado)
            .Where(l => l.Localizacao.IsWithinDistance(pontoCliente, raioMetros)) // ST_DWithin
            .Where(l => req.CategoriaLojaId == null || l.CategoriaLojaId == req.CategoriaLojaId.Value);

        // Projeção com distância calculada no banco (geography → metros)
        var projetado = query
            .Select(l => new LojaProximaDto(
                l.Id,
                l.Nome,
                l.CategoriaLoja.Nome,
                l.EnderecoComercial,
                l.Localizacao.Distance(pontoCliente) / 1000.0, // ST_Distance → km
                l.Localizacao.Y, // lat
                l.Localizacao.X, // lng
                true // AbertoAgora: em produção calcular via HorarioFuncionamentoJson + DateTime.UtcNow com TimeZoneInfo para America/Sao_Paulo; MVP retorna true e frontend filtra
            ))
            .OrderBy(dto => dto.DistanciaKm);

        return await PaginatedList<LojaProximaDto>.CreateAsync(projetado, req.Pagina, req.TamanhoPagina, ct);
    }
}
```

**SQL gerado (aproximado):**

```sql
SELECT l."Id", l."Nome", c."Nome" AS "Categoria",
       ST_Distance(l."Localizacao", ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography) / 1000 AS "DistanciaKm",
       ST_Y(l."Localizacao"::geometry) AS "Latitude",
       ST_X(l."Localizacao"::geometry) AS "Longitude"
FROM "Lojas" AS l
JOIN "CategoriasLoja" AS c ON l."CategoriaLojaId" = c."Id"
WHERE l."Ativo" AND l."Aprovado"
  AND ST_DWithin(l."Localizacao", ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography, @raioMetros)
  AND (@categoriaId IS NULL OR l."CategoriaLojaId" = @categoriaId)
ORDER BY l."Localizacao" <-> ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography
LIMIT @take OFFSET @skip;
```

**Índice KNN (`<->`) garante ordenação por proximidade usando índice GIST — sem `ORDER BY ST_Distance` full scan.**

#### 1.5.3. Fallback — Haversine via `FromSqlRaw` (se PostGIS não estiver habilitado)

```csharp
// Alternativa sem NetTopologySuite — calcula Haversine direto no Postgres
var sql = """
    SELECT l."Id", l."Nome", c."Nome" AS "Categoria", l."EnderecoComercial",
           (6371 * acos(cos(radians({0})) * cos(radians(ST_Y(l."Localizacao"::geometry))) *
            cos(radians(ST_X(l."Localizacao"::geometry)) - radians({1})) +
            sin(radians({0})) * sin(radians(ST_Y(l."Localizacao"::geometry))))) AS "DistanciaKm",
           ST_Y(l."Localizacao"::geometry) AS "Latitude",
           ST_X(l."Localizacao"::geometry) AS "Longitude"
    FROM "Lojas" l
    JOIN "CategoriasLoja" c ON l."CategoriaLojaId" = c."Id"
    WHERE l."Ativo" AND l."Aprovado"
      AND (6371 * acos(...)) <= {2}
    ORDER BY "DistanciaKm"
    LIMIT {3} OFFSET {4}
    """;
// Usar FromSqlInterpolated para parametrizar
```

> **Decisão normativa:** Usar **PostGIS + `IsWithinDistance` + `Distance`** como padrão. Haversine documentado como fallback e para testes unitários.

### 1.6. Geocodificação do Endereço do Cliente

```csharp
// src/Application/Common/Interfaces/IGeocodingService.cs — linha 1
public interface IGeocodingService
{
    Task<Coordenada?> GeocodificarAsync(string endereco, string cep, CancellationToken ct);
    Task<Coordenada?> GeocodificarCepAsync(string cep, CancellationToken ct);
}
public sealed record Coordenada(double Latitude, double Longitude);

// src/Infrastructure/Geolocation/PostgisGeocodingService.cs — linha 1 (fragmento)
// Em MVP acadêmico: tabela local de CEPs + fallback mock para demo localhost offline
// Cache em memória para CEPs já geocodificados
// Para apresentação localhost, usar MockGeocodingService com 5 CEPs fixos de SP (ver §1.6.1) para não depender de internet/Nominatim
```

### 1.6.1 Mock para demo localhost (sem internet)

Para apresentação, `IGeocodingService` tem implementação `MockGeocodingService` com dicionário fixo (sem chamada externa):

| CEP | Endereço | Lat | Lng |
|---|---|---|---|
| 01310-100 | Av. Paulista, 1000 - SP | -23.561414 | -46.656013 |
| 01311-000 | Rua Augusta, 2000 - SP | -23.556580 | -46.662321 |
| 04567-000 | Vila Mariana - SP | -23.588050 | -46.632070 |
| 05001-000 | Perdizes - SP | -23.534050 | -46.682070 |
| 05410-002 | Pinheiros - SP | -23.567120 | -46.687500 |

Se `cep` estiver no dicionário, retorna coordenada mockada. Se não, tenta ViaCEP+Nominatim; se falhar (offline), retorna `null` com mensagem "CEP não encontrado no mock de demo - use um dos 5 CEPs acima". Isso garante demo 100% offline.

**Controller — endpoint de busca:**

```csharp
// src/WebAPI/Controllers/LojasController.cs — linha 1 (fragmento)
[HttpGet("proximas")]
[AllowAnonymous] // cliente não precisa estar logado para ver lojas próximas
public async Task<IActionResult> ListarProximas(
    [FromQuery] string? cep,
    [FromQuery] string? endereco,
    [FromQuery] double? lat,
    [FromQuery] double? lng,
    [FromQuery] double raioKm = 5,
    [FromQuery] Guid? categoriaId,
    [FromQuery] int pagina = 1,
    [FromQuery] int tamanho = 20,
    CancellationToken ct = default)
{
    double qLat, qLng;

    if (lat.HasValue && lng.HasValue)
    {
        qLat = lat.Value; qLng = lng.Value;
    }
    else if (!string.IsNullOrWhiteSpace(cep) || !string.IsNullOrWhiteSpace(endereco))
    {
        var coord = await _geocoding.GeocodificarAsync(endereco ?? "", cep ?? "", ct);
        if (coord is null) return BadRequest(new { error = "Endereço/CEP não pôde ser geocodificado." });
        qLat = coord.Latitude; qLng = coord.Longitude;
    }
    else
    {
        return BadRequest(new { error = "Informe lat/lng ou cep/endereco." });
    }

    var query = new ListarLojasProximasQuery(qLat, qLng, raioKm, categoriaId, pagina, tamanho);
    var result = await _mediator.Send(query, ct);
    return Ok(result);
}
```

---

## 2. Organização por Categorias e Catálogo

### 2.1. Categorias Gastronômicas — Seed Fixo

```csharp
// src/Domain/Entities/CategoriaLoja.cs — linha 1
public sealed class CategoriaLoja : BaseEntity
{
    public string Nome { get; private set; } = null!; // ex: "Pizzarias"
    public string Slug { get; private set; } = null!; // ex: "pizzarias" — para URL /categoria/pizzarias
    public string? IconeUrl { get; private set; }
    public int OrdemExibicao { get; private set; }
    public bool Ativo { get; private set; } = true;

    public ICollection<Loja> Lojas { get; private set; } = [];
}
```

**Seed — `Infrastructure/Persistence/Seed/CategoriaSeed.cs`:**

```csharp
public static class CategoriaSeed
{
    public static readonly (string Nome, string Slug, int Ordem)[] Categorias =
    [
        ("Pizzarias", "pizzarias", 1),
        ("Hambúrgueres", "hamburgueres", 2),
        ("Comida Japonesa", "comida-japonesa", 3),
        ("Sorveterias", "sorveterias", 4),
        ("Docerias", "docerias", 5),
        ("Padarias Artesanais", "padarias-artesanais", 6),
        ("Comida Brasileira", "comida-brasileira", 7),
        ("Saudável / Vegana", "saudavel-vegana", 8),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.CategoriasLoja.AnyAsync(ct)) return;
        foreach (var (nome, slug, ordem) in Categorias)
            db.CategoriasLoja.Add(new CategoriaLoja { Nome = nome, Slug = slug, OrdemExibicao = ordem });
        await db.SaveChangesAsync(ct);
    }
}
```

### 2.2. Roteamento e Indexação — Tela Inicial

**Rota da home (frontend):**

```
GET /                          → lista categorias (carrossel) + lojas próximas (se CEP informado)
GET /categoria/{slug}          → lista lojas da categoria (com filtro de proximidade opcional)
GET /loja/{lojaId}             → catálogo da loja (produtos ativos, agrupados por categoria de produto)
GET /loja/{lojaId}/produto/{produtoId} → detalhe do produto
```

**Endpoints da API:**

```
GET /api/categorias                    → lista todas as categorias ativas (ordenadas por OrdemExibicao)
GET /api/categorias/{slug}/lojas      → lojas de uma categoria (com ?lat&lng&raioKm para ordenar por distância)
GET /api/lojas/{lojaId}/produtos      → catálogo da loja (apenas produtos Ativo=true e Aprovado=true)
GET /api/lojas/proximas?lat=&lng=&categoriaId= → busca por proximidade (ver §1.5)
```

**Query — `ListarLojasPorCategoria`:**

```csharp
public sealed record ListarLojasPorCategoriaQuery(string Slug, double? Lat, double? Lng, double RaioKm = 50, int Pagina = 1, int Tamanho = 20)
    : IRequest<PaginatedList<LojaProximaDto>>;

// Handler: filtra por Categoria.Slug, opcionalmente ordena por distância se lat/lng fornecidos
// Se lat/lng não fornecidos, ordena por Nome ou por Avaliação (quando houver)
```

**Produto — visibilidade:**

| `Produto.Ativo` | `Produto.Aprovado` | Visível para cliente? | Visível para vendedor dono? | Visível para Admin? |
|---|---|---|---|---|
| true | true | ✅ | ✅ | ✅ |
| true | false (pendente) | ❌ | ✅ (com badge "Pendente aprovação") | ✅ (fila moderação) |
| false | true | ❌ | ✅ (com badge "Inativo") | ✅ |
| false | false | ❌ | ✅ | ✅ |

**Query de catálogo filtra `Ativo && Aprovado` para clientes; painel do vendedor mostra todos com filtros.**

---

## 3. Motor de Comissões e PIX Fictício

### 3.1. Modelagem Matemática — Cálculo de Comissão

**Conceito:** Admin define **taxa de comissão global** (ex: 8,5%) cobrada sobre o **valor bruto** de cada pedido finalizado. O vendedor recebe o **valor líquido** (`bruto - comissão`). O marketplace retém a comissão.

Opcionalmente, cada `Loja` pode ter `TaxaComissaoPercentual` override (ex: loja premium negocia 5%). Se `NULL`, usa global.

**Fórmulas (com arredondamento bancário):**

```
comissao = Arredondar(bruto * (taxaPercentual / 100), 2)
liquido  = bruto - comissao
```

**Invariante:** `bruto == comissao + liquido` (sempre, sem perda de centavos).

**Exemplo:**

```
Pedido: 2x Pizza (59.90) + 1x Suco (15.00) = 134.80 bruto
Taxa: 8.5%
Comissão = 134.80 * 0.085 = 11.458 → 11.46
Líquido  = 134.80 - 11.46 = 123.34
Verificação: 11.46 + 123.34 = 134.80 ✅
```

### 3.2. Onde e quando calcular

| Momento | O que acontece |
|---|---|
| **Criação do pedido (Pendente)** | Calcula `ValorBruto` (soma dos itens). `TaxaComissao` e `ValorLiquido` ficam **NULL/zero** — ainda não há compromisso financeiro. |
| **Aprovação (Em Preparo)** | Vendedor aprova → decrementa estoque. Ainda não calcula comissão (pedido pode ser cancelado). |
| **Simulação PIX fictício (após Em Preparo → Pronto)** | Cliente "paga" via PIX fictício → sistema calcula e persiste `TaxaComissao` + `ValorLiquido` atomicamente. Se falhar, rollback. |
| **Finalização** | Pedido `Finalizado` → valores já persistidos, imutáveis. |

> **Decisão normativa para demo localhost:** Comissão calculada exclusivamente no **PIX fictício** (após `EmPreparo`). `Pendente` não persiste comissão. Garante atomicidade e invariante `bruto == comissao + liquido` com `MidpointRounding.AwayFromZero`.

### 3.3. Tabela `Pedidos` — campos financeiros

```sql
-- Fragmento
"ValorBruto" numeric(10,2) NOT NULL,        -- ex: 134.80
"TaxaComissaoPercentual" numeric(5,2) NULL,  -- ex: 8.50 (snapshot da taxa no momento do cálculo)
"TaxaComissaoValor" numeric(10,2) NULL,      -- ex: 11.46
"ValorLiquidoVendedor" numeric(10,2) NULL,   -- ex: 123.34
"PixPayload" text NULL,                      -- JSON do payload PIX fictício (para auditoria)
"PixSimuladoEm" timestamptz NULL
```

**Entidade `Pedido` (fragmento financeiro):**

```csharp
public sealed class Pedido : AggregateRoot
{
    public Dinheiro ValorBruto { get; private set; } = Dinheiro.Zero("BRL");
    public decimal? TaxaComissaoPercentual { get; private set; }
    public Dinheiro? TaxaComissaoValor { get; private set; }
    public Dinheiro? ValorLiquidoVendedor { get; private set; }
    public string? PixPayload { get; private set; }
    public DateTime? PixSimuladoEmUtc { get; private set; }

    public void RegistrarPagamentoPix(decimal taxaPercentual, string pixPayload)
    {
        if (Status != StatusPedido.EmPreparo && Status != StatusPedido.Pendente)
            throw new RegraDeNegocioException("PIX só pode ser simulado para pedido Pendente ou Em Preparo.");
        if (TaxaComissaoValor is not null)
            throw new RegraDeNegocioException("Comissão já calculada.");

        var bruto = ValorBruto.Valor;
        var comissaoValor = decimal.Round(bruto * (taxaPercentual / 100m), 2, MidpointRounding.AwayFromZero);
        var liquido = bruto - comissaoValor;

        TaxaComissaoPercentual = taxaPercentual;
        TaxaComissaoValor = new Dinheiro(comissaoValor, "BRL");
        ValorLiquidoVendedor = new Dinheiro(liquido, "BRL");
        PixPayload = pixPayload;
        PixSimuladoEmUtc = DateTime.UtcNow;

        // Validação de invariante
        if (TaxaComissaoValor.Valor + ValorLiquidoVendedor.Valor != ValorBruto.Valor)
            throw new InvalidOperationException("Invariante financeiro violado: bruto != comissao + liquido");
    }
}
```

### 3.4. Configuração Global — `TaxaComissao`

```csharp
// src/Domain/Entities/ConfiguracaoGlobal.cs — linha 1 (ou tabela TaxasComissao)
public sealed class ConfiguracaoGlobal : BaseEntity
{
    public decimal TaxaComissaoPadraoPercentual { get; private set; } // ex: 8.5
    public DateTime VigenciaDesde { get; private set; }

    public void AtualizarTaxa(decimal novaTaxa, Guid adminId)
    {
        if (novaTaxa < 0 || novaTaxa > 30) throw new RegraDeNegocioException("Taxa deve estar entre 0 e 30%.");
        TaxaComissaoPadraoPercentual = novaTaxa;
        AtualizadoEmUtc = DateTime.UtcNow;
        AddDomainEvent(new TaxaComissaoAlteradaEvent(novaTaxa, adminId));
    }
}
```

**Tabela:**

```sql
CREATE TABLE "ConfiguracoesGlobais" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "TaxaComissaoPadraoPercentual" numeric(5,2) NOT NULL DEFAULT 8.50,
    "VigenciaDesde" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL
);
-- Single row — seed com 8.50
```

**Endpoint Admin:**

```csharp
[HttpPatch("api/admin/configuracoes/taxa-comissao")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> AtualizarTaxa([FromBody] AtualizarTaxaDto dto, CancellationToken ct)
{
    var cmd = new AtualizarTaxaComissaoCommand(dto.NovaTaxaPercentual, _currentUser.UserId!.Value);
    var result = await _mediator.Send(cmd, ct);
    return result.IsSuccess ? NoContent() : BadRequest(result);
}
```

### 3.5. PIX Fictício — Payload Acadêmico

**Não é integração real com PSP.** Gera JSON simulado para fins acadêmicos/demonstração.

```csharp
// src/Application/Pedidos/Commands/SimularPagamentoPix/SimularPagamentoPixCommand.cs — linha 1
public sealed record SimularPagamentoPixCommand(Guid PedidoId, Guid ClienteId) : IRequest<Result<PixSimuladoDto>>;

public sealed record PixSimuladoDto(
    string PixCopiaECola,      // ex: "00020126580014BR.GOV.BCB.PIX0136..."
    string QrCodeBase64,       // placeholder base64
    decimal ValorBruto,
    decimal TaxaComissao,
    decimal ValorLiquido,
    string Status // "PAGO_FICTICIO"
);

// Handler
public sealed class SimularPagamentoPixCommandHandler : IRequestHandler<SimularPagamentoPixCommand, Result<PixSimuladoDto>>
{
    private readonly IAppDbContext _db;

    public async Task<Result<PixSimuladoDto>> Handle(SimularPagamentoPixCommand req, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Loja)
            .FirstOrDefaultAsync(p => p.Id == req.PedidoId, ct);
        if (pedido is null) return Result<PixSimuladoDto>.Failure("Pedido não encontrado.");
        if (pedido.ClienteId != req.ClienteId) return Result<PixSimuladoDto>.Failure("Pedido não pertence ao cliente.");
        if (pedido.PixSimuladoEmUtc is not null) return Result<PixSimuladoDto>.Failure("PIX já simulado.");

        // Resolve taxa: loja override ou global
        var taxaGlobal = await _db.ConfiguracoesGlobais.AsNoTracking().FirstAsync(ct);
        var taxa = pedido.Loja.TaxaComissaoPercentual ?? taxaGlobal.TaxaComissaoPadraoPercentual;

        // Gera payload fictício (BR Code simplificado)
        var pixPayload = GerarBrCodeFicticio(pedido.Id, pedido.ValorBruto.Valor);

        // Transação atômica — calcula e persiste
        await using var tx = await ((DbContext)_db).Database.BeginTransactionAsync(ct);
        try
        {
            pedido.RegistrarPagamentoPix(taxa, pixPayload);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return Result<PixSimuladoDto>.Success(new PixSimuladoDto(
            PixCopiaECola: pixPayload,
            QrCodeBase64: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pixPayload)), // mock
            ValorBruto: pedido.ValorBruto.Valor,
            TaxaComissao: pedido.TaxaComissaoValor!.Valor,
            ValorLiquido: pedido.ValorLiquidoVendedor!.Valor,
            Status: "PAGO_FICTICIO"
        ));
    }

    private static string GerarBrCodeFicticio(Guid pedidoId, decimal valor)
    {
        // Formato BR Code (PIX) simplificado — suficiente para demonstração
        // 000201 = Payload Format, 26 = Merchant Account, 59 = Merchant Name, 54 = Amount, 62 = Reference
        var valorStr = valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var refId = pedidoId.ToString("N")[..12].ToUpper();
        return $"00020126580014BR.GOV.BCB.PIX0136{refId}52040000530398654{valorStr}5802BR5913GASTRONOMIA6009SAO PAULO6207{refId}6304ABCD";
    }
}
```

**Atomicidade:** `RegistrarPagamentoPix` + `SaveChanges` dentro de transação. Se `DbUpdateConcurrencyException` ou falha de invariante, rollback — nenhum valor parcial é persistido.

**Endpoint:**

```csharp
[HttpPost("api/pedidos/{pedidoId:guid}/simular-pix")]
[Authorize] // qualquer cliente autenticado (dono do pedido)
[EnableRateLimiting("pix")]
public async Task<IActionResult> SimularPix(Guid pedidoId, CancellationToken ct)
{
    var cmd = new SimularPagamentoPixCommand(pedidoId, _currentUser.UserId!.Value);
    var result = await _mediator.Send(cmd, ct);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result);
}
```

### 3.6. Relatórios Globais — Admin

**Faturamento por período:**

```csharp
public sealed record RelatorioFaturamentoDto(
    DateTime De, DateTime Ate,
    decimal FaturamentoBrutoTotal,
    decimal ComissaoTotalRetida,
    decimal RepasseTotalVendedores,
    int TotalPedidos,
    IReadOnlyList<FaturamentoPorLojaDto> PorLoja
);

public sealed record FaturamentoPorLojaDto(Guid LojaId, string LojaNome, decimal Bruto, decimal Comissao, decimal Liquido, int QtdPedidos);
```

```csharp
// Query Handler — agregação no banco
var baseQuery = _db.Pedidos
    .AsNoTracking()
    .Where(p => p.Status == StatusPedido.Finalizado)
    .Where(p => p.CriadoEmUtc >= req.De && p.CriadoEmUtc <= req.Ate)
    .Where(p => p.TaxaComissaoValor != null);

var totalBruto = await baseQuery.SumAsync(p => p.ValorBruto.Valor, ct);
var totalComissao = await baseQuery.SumAsync(p => p.TaxaComissaoValor!.Valor, ct);
var totalLiquido = await baseQuery.SumAsync(p => p.ValorLiquidoVendedor!.Valor, ct);

var porLoja = await baseQuery
    .GroupBy(p => new { p.LojaId, p.Loja.Nome })
    .Select(g => new FaturamentoPorLojaDto(
        g.Key.LojaId, g.Key.Nome,
        g.Sum(p => p.ValorBruto.Valor),
        g.Sum(p => p.TaxaComissaoValor!.Valor),
        g.Sum(p => p.ValorLiquidoVendedor!.Valor),
        g.Count()
    ))
    .OrderByDescending(x => x.Bruto)
    .ToListAsync(ct);
```

**Endpoint:**

```csharp
[HttpGet("api/admin/relatorios/faturamento")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> Faturamento([FromQuery] DateTime de, [FromQuery] DateTime ate, CancellationToken ct)
{
    var query = new RelatorioFaturamentoQuery(de, ate);
    return Ok(await _mediator.Send(query, ct));
}
```

---

## 4. Módulo de Agenda / Cronograma de Produção do Vendedor

### 4.1. Visão Geral e Regra de Negócio

O Vendedor possui tela exclusiva **Agenda** no Dashboard (`/vendedor/agenda`) para organizar a produção do dia. A agenda é cronológica por `AprovadoEmUtc` e responde à pergunta: *o que cozinhar primeiro?*

**Regra atômica normativa:** Ao clicar em **Aprovar** (`POST /api/pedidos/{id}/aprovar`), o sistema executa em **uma única transação**:

```
AprovarPedidoCommand
  ├─ Valida ownership + status Pendente
  ├─ Debita estoque (Produto.DebitarEstoque + xmin)
  ├─ Atualiza Pedido.Status = EmPreparo + Pedido.AprovadoEmUtc = agora
  └─ INSERT ItemAgenda { PedidoId, LojaId, VendedorId, Status=EmPreparo, AprovadoEmUtc=agora }
  └─ SaveChangesAsync → COMMIT ou ROLLBACK total
```

Se qualquer etapa falhar (estoque insuficiente, conflito `xmin`, `PedidoId` já na agenda), **nada** é persistido. Agenda nunca fica órfã de estoque e estoque nunca é debitado sem agenda.

### 4.2. Entidade de Domínio — `ItemAgenda`

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

### 4.3. Fluxo de Automação Detalhado — Aprovação ➔ Entrada na Agenda

**Diagrama de sequência (happy path):**

```
Vendedor (Dashboard)
  │  POST /api/pedidos/{pedidoId}/aprovar  (JWT vendor_id)
  ▼
WebAPI: PedidosController.Aprovar
  │  Authorize(VendorOwnerOrAdmin) + RateLimit
  ▼
MediatR: AprovarPedidoCommandHandler
  │  1. SELECT Pedido + Itens + Loja  (tracking)
  │  2. Autoriza VendorId == Loja.VendedorId
  │  3. Se Status != Pendente → 400
  │  4. SELECT Produtos WHERE Id IN (...)  (tracking xmin)
  │  5. Para cada Item: valida Ativo + estoque >= qtd, DebitarEstoque(qtd)
  │  6. agora = IClock.UtcNow
  │  7. pedido.Aprovar(vendorId, agora)  → Status=EmPreparo, AprovadoEmUtc=agora
  │  8. Verifica ItemAgenda já existe? se sim → 409
  │  9. itemAgenda = ItemAgenda.Criar(pedidoId, lojaId, vendorId, agora)
  │ 10. _db.ItensAgenda.Add(itemAgenda)
  │ 11. SaveChangesAsync()  → gera em 1 transação:
  │        UPDATE Produtos SET Quantidade=..., xmin WHERE Id=... AND xmin=@old
  │        UPDATE Pedidos SET Status=2, AprovadoEmUtc=@agora WHERE Id=...
  │        INSERT ItensAgenda (Id, PedidoId, LojaId, VendedorId, Status, AprovadoEmUtc)
  │        INSERT AuditLogs ...
  │ 12. COMMIT
  ▼
Response 200 { pedidoId, status:"EmPreparo", aprovadoEmUtc, agendaId }
  │
  ▼
Frontend: toast "Pedido #123 aprovado e na agenda" + refresh GET /api/vendedor/agenda
```

**Casos de falha e compensação:**

| Falha | Momento | Efeito |
|---|---|---|
| Estoque insuficiente | Passo 5 | `Result.Failure(400)` antes de qualquer UPDATE, sem tocar na agenda |
| Conflito `xmin` concorrente | Passo 11 (DbUpdateConcurrencyException) | Rollback total, retorna 409, agenda não criada |
| Pedido já aprovado / já na agenda | Passo 8 ou Unique violation em `PedidoId` | 409, estoque não debitado duas vezes |
| `Pedidos.AprovadoEmUtc` NULL mas `ItemAgenda` existe (inconsistência) | Invariante quebrado | Handler verifica e lançar exceção → trigger alerta auditoria |

**Idempotência:** `Pedidos.PedidoId` em `ItensAgenda` é `UNIQUE`. Segunda chamada de `Aprovar` para mesmo pedido falha com 409 mesmo sob race condition (catch `DbUpdateException` com inner `IX_ItensAgenda_PedidoId`).

### 4.4. Ordenação Cronológica e Consultas da Agenda

**Regra de ordenação:** agenda do dia ordenada por `AprovadoEmUtc ASC` (quem foi aprovado primeiro, cozinha primeiro). Em caso de empate (mesmo segundo), desempate por `CriadoEmUtc` do pedido.

**Query — `ListarAgendaDoDia`:**

```csharp
namespace GastronomiaMarketplace.Application.Agenda.Queries.ListarAgendaDoDia;

public sealed record ListarAgendaDoDiaQuery(Guid LojaId, Guid VendedorId, bool IsAdmin, DateTime? Data, StatusPedido? Status) : IRequest<IReadOnlyList<ItemAgendaDto>>;

public sealed record ItemAgendaDto(
    Guid AgendaId,
    Guid PedidoId,
    string ClienteNome,
    DateTime AprovadoEmUtc,
    StatusPedido Status,
    IReadOnlyList<string> ItensResumo,
    decimal ValorBruto,
    int PosicaoFila
);

public sealed class ListarAgendaDoDiaQueryHandler : IRequestHandler<ListarAgendaDoDiaQuery, IReadOnlyList<ItemAgendaDto>>
{
    private readonly IAppDbContext _db;

    public ListarAgendaDoDiaQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ItemAgendaDto>> Handle(ListarAgendaDoDiaQuery req, CancellationToken ct)
    {
        var dia = req.Data?.Date ?? DateTime.UtcNow.Date;
        var inicio = dia;
        var fim = dia.AddDays(1);

        var baseQuery = _db.ItensAgenda
            .AsNoTracking()
            .Where(a => a.LojaId == req.LojaId)
            .Where(a => a.AprovadoEmUtc >= inicio && a.AprovadoEmUtc < fim);

        if (req.Status.HasValue)
            baseQuery = baseQuery.Where(a => a.Status == req.Status.Value);

        var itens = await baseQuery
            .Include(a => a.Pedido)
            .ThenInclude(p => p.Itens)
            .OrderBy(a => a.AprovadoEmUtc)
            .ThenBy(a => a.CriadoEmUtc)
            .Select(a => new ItemAgendaDto(
                a.Id,
                a.PedidoId,
                a.Pedido.ClienteNomeSnapshot,
                a.AprovadoEmUtc,
                a.Status,
                a.Pedido.Itens.Select(i => $"{i.Quantidade}x {i.ProdutoNomeSnapshot}").ToList(),
                a.Pedido.ValorBruto,
                0
            ))
            .ToListAsync(ct);

        var comPosicao = itens.Select((dto, idx) => dto with { PosicaoFila = idx + 1 }).ToList();
        return comPosicao;
    }
}
```

**SQL gerado esperado:**

```sql
SELECT a."Id", a."PedidoId", p."ClienteNomeSnapshot", a."AprovadoEmUtc", a."Status"
FROM "ItensAgenda" a
JOIN "Pedidos" p ON p."Id" = a."PedidoId"
WHERE a."LojaId" = @lojaId
  AND a."AprovadoEmUtc" >= @inicio AND a."AprovadoEmUtc" < @fim
ORDER BY a."AprovadoEmUtc" ASC, a."CriadoEmUtc" ASC;
```

**Índices para performance (ver database_concurrency.md):**

```sql
CREATE INDEX "IX_ItensAgenda_Loja_AprovadoEm" ON "ItensAgenda" ("LojaId", "AprovadoEmUtc");
CREATE INDEX "IX_ItensAgenda_Loja_Status_AprovadoEm" ON "ItensAgenda" ("LojaId", "Status", "AprovadoEmUtc");
```

### 4.5. Avanço na Agenda — Pronto ➔ Finalizado

Quando o vendedor termina de cozinhar, ele avança o item na agenda. Essa ação sincroniza `ItemAgenda.Status` e `Pedido.Status` na mesma transação para manter consistência visual.

```csharp
namespace GastronomiaMarketplace.Application.Agenda.Commands.AvancarStatusAgenda;

public sealed record AvancarStatusAgendaCommand(Guid AgendaId, Guid VendedorId, bool IsAdmin, StatusPedido NovoStatus) : IRequest<Result>;

public sealed class AvancarStatusAgendaCommandHandler : IRequestHandler<AvancarStatusAgendaCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AvancarStatusAgendaCommandHandler(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(AvancarStatusAgendaCommand req, CancellationToken ct)
    {
        var item = await _db.ItensAgenda
            .Include(a => a.Pedido)
            .FirstOrDefaultAsync(a => a.Id == req.AgendaId, ct);
        if (item is null) return Result.Failure("Item da agenda não encontrado.");

        if (!req.IsAdmin && item.VendedorId != req.VendedorId)
            return Result.Failure("Vendedor não autorizado para esta agenda.");

        var agora = _clock.UtcNow;

        if (req.NovoStatus == StatusPedido.ProntoParaRetirada)
        {
            item.AvancarParaProntoRetirada(agora);
            item.Pedido.MarcarProntoParaRetirada();
        }
        else if (req.NovoStatus == StatusPedido.Finalizado)
        {
            item.Finalizar(agora);
            item.Pedido.Finalizar();
        }
        else
        {
            return Result.Failure("Transição de agenda inválida.");
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Endpoints:**

```
GET    /api/vendedor/agenda?data=2026-09-09&status=EmPreparo     → lista cronológica do dia
PATCH  /api/vendedor/agenda/{agendaId}/avancar  { novoStatus: "ProntoParaRetirada" }
PATCH  /api/vendedor/agenda/{agendaId}/avancar  { novoStatus: "Finalizado" }
```

**Filtros de dashboard:**

- `EmPreparo` → fila de produção (ordem de cozinhar)
- `ProntoParaRetirada` → aguardando cliente retirar
- `Finalizado` → histórico do dia

### 4.6. Integração com Outras Regras

- **Estoque:** segue regra otimista `xmin` — débito e agenda são atômicos; cancelamento em `EmPreparo` estorna estoque e remove/finaliza item da agenda (ver database_concurrency.md §2.9).
- **Auditoria:** `AuditableEntityInterceptor` registra `INSERT ItensAgenda` e `UPDATE Pedidos` no mesmo `SaveChanges`, rastreável por `AuditLogs.Entidade = "ItemAgenda"`.
- **LGPD:** `ItemAgenda` não duplica dados sensíveis, apenas `PedidoId` + snapshot de nome.

---

## 5. Fluxos de Pedido — Máquina de Estados

```
                    ┌──────────┐
              ┌────►│ Pendente │◄─────────────────────┐
              │     └────┬─────┘                      │
              │          │ Aprovar (vendedor)         │ Cancelar (cliente/vendedor/admin)
              │          ▼                            │
              │   ┌─────────────┐                     │
              │   │ Em Preparo  │─────────────────────┼──► Cancelado
              │   │ (na Agenda) │                     │
              │   └──────┬──────┘                     │
              │          │ Pronto p/ Retirada         │
              │          ▼                            │
              │  ┌──────────────────┐                 │
              │  │Pronto p/ Retirada│─────────────────┘
              │  │  (na Agenda)     │
              │  └────────┬─────────┘
              │           │ Finalizar (vendedor)
              │           ▼
              │     ┌───────────┐
              └─────┤Finalizado │
                    └───────────┘
```

**Regras atualizadas com Agenda:**

- Cliente cria → `Pendente`. Estoque **não** debitado, **não** entra na agenda (ver `database_concurrency.md`).
- Vendedor `Aprovar` → `EmPreparo` + debita estoque + `INSERT ItemAgenda` com `AprovadoEmUtc=agora` (transação atômica com `xmin`).
- Agenda ordena por `AprovadoEmUtc ASC` — primeiro aprovado, primeiro a produzir.
- Vendedor `ProntoParaRetirada` → atualiza `Pedido` e `ItemAgenda` sincronamente para `ProntoParaRetirada`.
- Vendedor `Finalizar` → ambos para `Finalizado` (imutável; entra em relatórios).
- Cancelamento em `Pendente` → sem efeito em agenda. Em `EmPreparo` → estorna estoque e marca agenda como `Cancelado` ou remove.


---

## 6. Checklist para o Agente

- [ ] Habilitar PostGIS (`CREATE EXTENSION postgis`) e mapear `Loja.Localizacao` como `geography(Point,4326)` com índice GIST.
- [ ] Implementar `HaversineCalculator` para fallback/testes e `ListarLojasProximasQueryHandler` com `IsWithinDistance` + `Distance` + `OrderBy`.
- [ ] Seed de 8 `CategoriasLoja` com `Slug` e `OrdemExibicao`.
- [ ] Endpoints `GET /api/categorias`, `GET /api/categorias/{slug}/lojas`, `GET /api/lojas/{id}/produtos` com filtros de visibilidade.
- [ ] Implementar `ConfiguracaoGlobal` (taxa padrão 8.5%) + `PATCH /api/admin/configuracoes/taxa-comissao`.
- [ ] Implementar `Pedido.RegistrarPagamentoPix` com invariante `bruto == comissao + liquido` e transação atômica em `SimularPagamentoPixCommandHandler`.
- [ ] Gerar `PixPayload` fictício BR Code + `QrCodeBase64` mock.
- [ ] Implementar `RelatorioFaturamentoQuery` com agregação por loja e período.
- [ ] Implementar `ItemAgenda` + `AprovarPedidoCommandHandler` atômico (estoque + Pedido.AprovadoEmUtc + INSERT agenda) + `ListarAgendaDoDia` ordenado por `AprovadoEmUtc ASC`.
- [ ] Endpoints `GET /api/vendedor/agenda` e `PATCH /api/vendedor/agenda/{id}/avancar` com sincronização de status Pedido ↔ Agenda.
- [ ] Garantir que todos os cálculos monetários usam `decimal` + `MidpointRounding.AwayFromZero` + tipo `Dinheiro` Value Object.
