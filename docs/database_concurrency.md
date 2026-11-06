# Database & Concurrency — Modelo de Dados e Controle de Concorrência Crítica

> **Stack:** PostgreSQL 16+ / EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL / PostGIS / `xmin` (concorrência otimista)
> **Versão:** 1.1 — 2026-09-09 — **Atualizado com Tabela `ItensAgenda` e campo `Pedidos.AprovadoEmUtc`**
> **Changelog 1.1:** Adicionada tabela `ItensAgenda` (UNIQUE PedidoId, índices cronológicos), campo `AprovadoEmUtc` em `Pedidos`, `ItemAgendaConfiguration`, transação estendida (estoque + pedido + agenda) e cancelamento com estorno de agenda.

---

## 1. Modelo Relacional Completo — Tabelas, Tipos Postgres e FKs

### 1.1. Convenções

- **PK:** `uuid` com `gen_random_uuid()` (extensão `pgcrypto`).
- **Timestamps:** `timestamptz` (sempre UTC — `DateTime.UtcNow` no C#).
- **Monetário:** `numeric(10,2)` para valores, `numeric(5,2)` para percentuais.
- **Texto:** `varchar(n)` quando há limite de negócio, `text` quando livre ou ciphertext.
- **Soft delete:** `is_deleted boolean DEFAULT false` + `HasQueryFilter`.
- **Concorrência:** `xmin xid` (coluna de sistema Postgres) mapeada como `uint` com `IsRowVersion()` — ver §2.
- **Espacial:** `geography(Point,4326)` para `Lojas.localizacao`.

### 1.2. Diagrama ER (textual)

```
Usuarios 1──0..1 Vendedores 1──1 Lojas 1──* Produtos
   │                │             │  ▲          │
   │                │             │  │          │
   │                └─────────────┘  │          │
   │                                 │          │
   └──* Pedidos *──1 Loja            │          │
         │  ▲                        │          │
         │  │                        │          │
         └──* ItensDoPedido *────────┘          │
              │                                 │
              └──── FK → Produtos (snapshot) ───┘

Pedidos 1──0..1 ItensAgenda *──1 Lojas
                │  └─ FK PedidoId UNIQUE (1:1 com Pedido aprovado)
                └──── FK VendedorId → Vendedores

CategoriasLoja 1──* Lojas
CategoriasLoja 1──* Produtos (opcional — categoria do produto)

ConsentimentosLgpd *──1 Usuarios
AuditLogs *──0..1 Usuarios (usuario_id nullable — sistema pode auditar)
ConfiguracoesGlobais (single row)
```

### 1.3. DDL — Tabelas (ordem de criação respeitando FKs)

```sql
-- Extensões
CREATE EXTENSION IF NOT EXISTS "pgcrypto"; -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "postgis";  -- geography

-- 1. CategoriasLoja
CREATE TABLE "CategoriasLoja" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Nome" varchar(100) NOT NULL,
    "Slug" varchar(100) NOT NULL UNIQUE, -- ex: "pizzarias"
    "IconeUrl" varchar(500) NULL,
    "OrdemExibicao" integer NOT NULL,
    "Ativo" boolean NOT NULL DEFAULT true,
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL
);
CREATE INDEX "IX_CategoriasLoja_Slug" ON "CategoriasLoja" ("Slug");
CREATE INDEX "IX_CategoriasLoja_Ordem" ON "CategoriasLoja" ("OrdemExibicao");

-- 2. Usuarios
CREATE TABLE "Usuarios" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "NomeCompleto" varchar(200) NOT NULL,
    "Email" varchar(254) NOT NULL UNIQUE,
    "SenhaHash" varchar(255) NOT NULL,
    "Cpf" varchar(11) NULL UNIQUE, -- apenas dígitos
    "Telefone" varchar(20) NULL,
    "EnderecoAtual" varchar(500) NULL,
    "Cep" varchar(9) NULL,
    "Papel" smallint NOT NULL DEFAULT 1, -- 1=Cliente, 2=Vendedor, 3=Admin
    -- Dados sensíveis — ciphertext via HasConversion (text para acomodar base64)
    "AlergiasSeveras" text NULL,
    "RestricoesAlimentares" text NULL,
    "ObservacoesSaude" text NULL,
    "IsAnonimizado" boolean NOT NULL DEFAULT false,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL,
    -- xmin é coluna de sistema — não declarada, mas mapeada no EF Core
    CONSTRAINT "CK_Usuarios_Papel" CHECK ("Papel" IN (1,2,3))
);
CREATE INDEX "IX_Usuarios_Email" ON "Usuarios" ("Email");
CREATE INDEX "IX_Usuarios_Cpf" ON "Usuarios" ("Cpf") WHERE "Cpf" IS NOT NULL;
CREATE INDEX "IX_Usuarios_Papel" ON "Usuarios" ("Papel");

-- 3. ConsentimentosLgpd
CREATE TABLE "ConsentimentosLgpd" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UsuarioId" uuid NOT NULL REFERENCES "Usuarios"("Id") ON DELETE CASCADE,
    "Tipo" smallint NOT NULL, -- 1=DadosSaude, 2=Geolocalizacao, 3=Marketing
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

-- 4. Vendedores
CREATE TABLE "Vendedores" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UsuarioId" uuid NOT NULL UNIQUE REFERENCES "Usuarios"("Id") ON DELETE CASCADE,
    "NomeLoja" varchar(150) NOT NULL,
    "CategoriaLojaId" uuid NOT NULL REFERENCES "CategoriasLoja"("Id") ON DELETE RESTRICT,
    "EnderecoComercial" varchar(500) NOT NULL,
    "Cep" varchar(9) NOT NULL,
    "Telefone" varchar(20) NOT NULL,
    "Cnpj" varchar(14) NULL, -- apenas dígitos, nullable para MEI/pessoa física
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Status" smallint NOT NULL DEFAULT 1, -- 1=PendenteAprovacao, 2=Aprovado, 3=Rejeitado
    "MotivoRejeicao" varchar(500) NULL,
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "CK_Vendedores_Status" CHECK ("Status" IN (1,2,3))
);
CREATE INDEX "IX_Vendedores_UsuarioId" ON "Vendedores" ("UsuarioId");
CREATE INDEX "IX_Vendedores_Categoria" ON "Vendedores" ("CategoriaLojaId");
CREATE INDEX "IX_Vendedores_Status" ON "Vendedores" ("Status");

-- 5. Lojas (tenant lógico — 1:1 com Vendedores)
CREATE TABLE "Lojas" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "VendedorId" uuid NOT NULL UNIQUE REFERENCES "Vendedores"("Id") ON DELETE CASCADE,
    "CategoriaLojaId" uuid NOT NULL REFERENCES "CategoriasLoja"("Id") ON DELETE RESTRICT,
    "Nome" varchar(150) NOT NULL,
    "Descricao" text NULL, -- sanitizado via HtmlSanitizer
    "EnderecoComercial" varchar(500) NOT NULL,
    "Cep" varchar(9) NOT NULL,
    "Localizacao" geography(point, 4326) NOT NULL,
    "Ativo" boolean NOT NULL DEFAULT true,
    "Aprovado" boolean NOT NULL DEFAULT false, -- moderação Admin
    "HorarioFuncionamentoJson" jsonb NULL,
    "TaxaComissaoPercentual" numeric(5,2) NULL, -- override, NULL usa global
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);
CREATE INDEX "IX_Lojas_VendedorId" ON "Lojas" ("VendedorId");
CREATE INDEX "IX_Lojas_Categoria" ON "Lojas" ("CategoriaLojaId");
CREATE INDEX "IX_Lojas_Localizacao" ON "Lojas" USING GIST ("Localizacao");
CREATE INDEX "IX_Lojas_Ativo_Aprovado" ON "Lojas" ("Ativo", "Aprovado");

-- 6. Produtos (+ Estoque — ver §1.4)
CREATE TABLE "Produtos" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "LojaId" uuid NOT NULL REFERENCES "Lojas"("Id") ON DELETE CASCADE,
    "CategoriaLojaId" uuid NULL REFERENCES "CategoriasLoja"("Id") ON DELETE SET NULL, -- opcional: subcategoria do produto
    "Nome" varchar(150) NOT NULL,
    "Descricao" text NULL, -- sanitizado
    "Preco" numeric(10,2) NOT NULL CHECK ("Preco" >= 0),
    "QuantidadeEmEstoque" integer NOT NULL DEFAULT 0 CHECK ("QuantidadeEmEstoque" >= 0),
    "Ativo" boolean NOT NULL DEFAULT true,
    "Aprovado" boolean NOT NULL DEFAULT false, -- moderação Admin (segurança alimentar)
    "ImagemUrl" varchar(1000) NULL,
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    -- xmin mapeado como RowVersion — não declarado, gerenciado pelo Postgres
    CONSTRAINT "CK_Produtos_Quantidade" CHECK ("QuantidadeEmEstoque" >= 0)
);
CREATE INDEX "IX_Produtos_LojaId" ON "Produtos" ("LojaId");
CREATE INDEX "IX_Produtos_Loja_Ativo_Aprovado" ON "Produtos" ("LojaId", "Ativo", "Aprovado");
CREATE INDEX "IX_Produtos_Nome" ON "Produtos" ("Nome");

-- 7. Pedidos
CREATE TABLE "Pedidos" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ClienteId" uuid NOT NULL REFERENCES "Usuarios"("Id") ON DELETE RESTRICT,
    "LojaId" uuid NOT NULL REFERENCES "Lojas"("Id") ON DELETE RESTRICT,
    "Status" smallint NOT NULL DEFAULT 1, -- 1=Pendente, 2=EmPreparo, 3=ProntoParaRetirada, 4=Finalizado, 5=Cancelado
    "ValorBruto" numeric(10,2) NOT NULL CHECK ("ValorBruto" >= 0),
    "TaxaComissaoPercentual" numeric(5,2) NULL,
    "TaxaComissaoValor" numeric(10,2) NULL CHECK ("TaxaComissaoValor" IS NULL OR "TaxaComissaoValor" >= 0),
    "ValorLiquidoVendedor" numeric(10,2) NULL CHECK ("ValorLiquidoVendedor" IS NULL OR "ValorLiquidoVendedor" >= 0),
    "PixPayload" text NULL,
    "PixSimuladoEm" timestamptz NULL,
    "AprovadoEmUtc" timestamptz NULL, -- preenchido atomicamente no Aprovar junto com ItemAgenda
    "MotivoCancelamento" varchar(500) NULL,
    "ClienteNomeSnapshot" varchar(200) NULL, -- snapshot anonimizado para retenção fiscal
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "CK_Pedidos_Status" CHECK ("Status" IN (1,2,3,4,5)),
    CONSTRAINT "CK_Pedidos_Comissao_Consistencia" CHECK (
        ("TaxaComissaoValor" IS NULL AND "ValorLiquidoVendedor" IS NULL) OR
        ("TaxaComissaoValor" IS NOT NULL AND "ValorLiquidoVendedor" IS NOT NULL AND "ValorBruto" = "TaxaComissaoValor" + "ValorLiquidoVendedor")
    ),
    CONSTRAINT "CK_Pedidos_AprovadoEm_Consistencia" CHECK (
        ("Status" = 1 AND "AprovadoEmUtc" IS NULL) OR ("Status" != 1 AND "AprovadoEmUtc" IS NOT NULL)
    )
);
CREATE INDEX "IX_Pedidos_ClienteId" ON "Pedidos" ("ClienteId");
CREATE INDEX "IX_Pedidos_LojaId" ON "Pedidos" ("LojaId");
CREATE INDEX "IX_Pedidos_Status" ON "Pedidos" ("Status");
CREATE INDEX "IX_Pedidos_CriadoEm" ON "Pedidos" ("CriadoEmUtc" DESC);
CREATE INDEX "IX_Pedidos_Loja_Status" ON "Pedidos" ("LojaId", "Status");
CREATE INDEX "IX_Pedidos_AprovadoEm" ON "Pedidos" ("LojaId", "AprovadoEmUtc");

-- 8. ItensDoPedido
CREATE TABLE "ItensDoPedido" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "PedidoId" uuid NOT NULL REFERENCES "Pedidos"("Id") ON DELETE CASCADE,
    "ProdutoId" uuid NOT NULL REFERENCES "Produtos"("Id") ON DELETE RESTRICT,
    "ProdutoNomeSnapshot" varchar(150) NOT NULL, -- snapshot do nome no momento do pedido
    "PrecoUnitario" numeric(10,2) NOT NULL CHECK ("PrecoUnitario" >= 0),
    "Quantidade" integer NOT NULL CHECK ("Quantidade" > 0),
    "Subtotal" numeric(10,2) NOT NULL CHECK ("Subtotal" >= 0), -- PrecoUnitario * Quantidade
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX "IX_Itens_PedidoId" ON "ItensDoPedido" ("PedidoId");
CREATE INDEX "IX_Itens_ProdutoId" ON "ItensDoPedido" ("ProdutoId");

-- 8b. ItensAgenda — Agenda de Produção (novo módulo)
CREATE TABLE "ItensAgenda" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "PedidoId" uuid NOT NULL UNIQUE REFERENCES "Pedidos"("Id") ON DELETE CASCADE,
    "LojaId" uuid NOT NULL REFERENCES "Lojas"("Id") ON DELETE CASCADE,
    "VendedorId" uuid NOT NULL REFERENCES "Vendedores"("Id") ON DELETE CASCADE,
    "Status" smallint NOT NULL DEFAULT 2, -- 2=EmPreparo, 3=ProntoParaRetirada, 4=Finalizado, 5=Cancelado
    "AprovadoEmUtc" timestamptz NOT NULL, -- cópia de Pedidos.AprovadoEmUtc para ordenação sem JOIN
    "ProntoEmUtc" timestamptz NULL,
    "CriadoEmUtc" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL,
    CONSTRAINT "CK_ItensAgenda_Status" CHECK ("Status" IN (2,3,4,5))
);
CREATE INDEX "IX_ItensAgenda_PedidoId" ON "ItensAgenda" ("PedidoId");
CREATE INDEX "IX_ItensAgenda_Loja_AprovadoEm" ON "ItensAgenda" ("LojaId", "AprovadoEmUtc");
CREATE INDEX "IX_ItensAgenda_Loja_Status_AprovadoEm" ON "ItensAgenda" ("LojaId", "Status", "AprovadoEmUtc");
CREATE INDEX "IX_ItensAgenda_Vendedor_Status" ON "ItensAgenda" ("VendedorId", "Status");

-- 9. ConfiguracoesGlobais (single row)
CREATE TABLE "ConfiguracoesGlobais" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "TaxaComissaoPadraoPercentual" numeric(5,2) NOT NULL DEFAULT 8.50 CHECK ("TaxaComissaoPadraoPercentual" BETWEEN 0 AND 30),
    "VigenciaDesde" timestamptz NOT NULL DEFAULT now(),
    "AtualizadoEmUtc" timestamptz NULL
);

-- 10. AuditLogs
CREATE TABLE "AuditLogs" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UsuarioId" uuid NULL REFERENCES "Usuarios"("Id") ON DELETE SET NULL,
    "Acao" varchar(100) NOT NULL,
    "Entidade" varchar(100) NOT NULL,
    "EntidadeId" varchar(100) NOT NULL,
    "DataUtc" timestamptz NOT NULL DEFAULT now(),
    "ValoresAntigos" jsonb NULL,
    "ValoresNovos" jsonb NULL,
    "IpOrigem" varchar(45) NULL,
    "UserAgent" varchar(500) NULL,
    "Detalhes" text NULL
);
CREATE INDEX "IX_AuditLogs_Entidade_EntidadeId" ON "AuditLogs" ("Entidade", "EntidadeId");
CREATE INDEX "IX_AuditLogs_Usuario_Data" ON "AuditLogs" ("UsuarioId", "DataUtc" DESC);
CREATE INDEX "IX_AuditLogs_Data" ON "AuditLogs" ("DataUtc" DESC);

-- Imutabilidade (ver secure_coding_practices.md §3.2)
CREATE OR REPLACE FUNCTION fn_prevent_audit_mutation() RETURNS trigger AS $$
BEGIN RAISE EXCEPTION 'AuditLogs é imutável — UPDATE/DELETE não permitido'; END; $$ LANGUAGE plpgsql;
CREATE TRIGGER trg_audit_immutable BEFORE UPDATE OR DELETE ON "AuditLogs" FOR EACH ROW EXECUTE FUNCTION fn_prevent_audit_mutation();

-- Seed essencial para demo localhost offline
INSERT INTO "ConfiguracoesGlobais" ("TaxaComissaoPadraoPercentual") VALUES (8.50) ON CONFLICT DO NOTHING;

-- Seed Admin fixo para banca (senha: Admin@123)
-- senha_hash = BCrypt Hash com workFactor 12 de "Admin@123"
INSERT INTO "Usuarios" ("Id", "NomeCompleto", "Email", "SenhaHash", "Papel", "Cpf") VALUES
('00000000-0000-0000-0000-000000000001', 'Admin Saboriza', 'admin@saboriza.local', '$2a$12$EXEMPLOHASHADMIN12345678901234567890123456789012', 3, '00000000001')
ON CONFLICT ("Email") DO NOTHING;

-- Seed Cliente demo (senha: Cliente@123)
INSERT INTO "Usuarios" ("Id", "NomeCompleto", "Email", "SenhaHash", "Papel", "Cpf") VALUES
('00000000-0000-0000-0000-000000000002', 'João Cliente', 'cliente@saboriza.local', '$2a$12$EXEMPLOHASHCLIENTE12345678901234567890', 1, '00000000002')
ON CONFLICT ("Email") DO NOTHING;

-- Seed Vendedor aprovado + Loja demo (para busca por proximidade funcionar sem internet)
-- Coordenadas fixas: Av. Paulista, 1000 - São Paulo -23.5613,-46.6560 (PostGIS geography)
-- Categorias seed via EF Core HasData ou SeedAsync (ver features_spec.md §2.1) - 8 categorias com slugs fixos
-- Lojas demo: inserir 3 lojas com Localizacao = ST_SetSRID(ST_MakePoint(lng, lat),4326)::geography
```

### 1.4. Por que não tabela `Estoques` separada?

O enunciado lista `Estoques` como tabela, mas para este domínio **estoque é atributo do `Produto`** (`QuantidadeEmEstoque integer`). Tabela separada só se houver **múltiplos depósitos/lotes/validades** — fora do escopo MVP. Se o avaliador exigir `Estoques`, criar como:

```sql
CREATE TABLE "Estoques" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ProdutoId" uuid NOT NULL UNIQUE REFERENCES "Produtos"("Id") ON DELETE CASCADE,
    "Quantidade" integer NOT NULL DEFAULT 0 CHECK ("Quantidade" >= 0),
    "AtualizadoEmUtc" timestamptz NULL,
    "xmin" xid NOT NULL -- RowVersion aqui em vez de Produtos
);
-- Neste caso, remover QuantidadeEmEstoque de Produtos.
-- Este doc mantém estoque em Produtos por simplicidade e atomicidade (1 UPDATE em vez de JOIN).
```

**Decisão normativa deste doc:** Estoque vive em `Produtos.QuantidadeEmEstoque`. Se o projeto exigir `Estoques`, mover a coluna `xmin` para lá e ajustar handlers.

### 1.5. Tipos Postgres — Tabela Resumo

| Campo | Tipo Postgres | Mapeamento C# | Observação |
|---|---|---|---|
| `Id` | `uuid` | `Guid` | `gen_random_uuid()` |
| `Email` | `varchar(254)` | `string` | UNIQUE, lower |
| `Preco` | `numeric(10,2)` | `decimal` | `Dinheiro` Value Object |
| `QuantidadeEmEstoque` | `integer` | `int` | `CHECK >=0` |
| `CriadoEmUtc` | `timestamptz` | `DateTime` | UTC |
| `Localizacao` | `geography(Point,4326)` | `Point` (NetTopologySuite) | `ST_DWithin` |
| `HorarioFuncionamentoJson` | `jsonb` | `string` (JSON) | |
| `ValoresAntigos` | `jsonb` | `string` (JSON) | audit delta |
| `xmin` | `xid` | `uint` | RowVersion — ver §2 |

---

## 2. Controle de Concorrência de Estoque — Race Conditions e `xmin`

### 2.1. O Problema — Vendas Fantasmas (estoque negativo)

```
Tempo  Cliente A compra última unidade (1 em estoque)  Cliente B compra última unidade
─────  ───────────────────────────────────────────────  ──────────────────────────────────
 t0    Pedido A criado (Pendente)                       Pedido B criado (Pendente)
       — estoque ainda 1 (não debitado)                 — estoque ainda 1
 t1    Vendedor clica Aprovar(A)                        Vendedor clica Aprovar(B) — quase simultâneo
 t2    Handler A: lê estoque=1, debita → 0, save       Handler B: lê estoque=1 (antes do commit de A), debita → 0, save
 t3    COMMIT A → estoque 0                             COMMIT B → estoque -1  ❌
```

Sem controle, o banco aceita ambos os `UPDATE`s e o estoque fica **-1** (venda fantasma).

### 2.2. Por que Estoque NÃO Altera na Criação do Pedido (`Pendente`)

| Evento | Altera estoque? | Por quê |
|---|---|---|
| `Pendente` (cliente cria) | **Não** | Cliente pode criar e nunca pagar; vendedor pode rejeitar; produto pode ficar reservado indefinidamente. Débito antecipado gera **bloqueio de estoque fantasma** e exige mecanismo de expiração/reserva. |
| `EmPreparo` (`Aprovar`) | **Sim** | Momento de compromisso: vendedor validou disponibilidade e assumiu preparo. É aqui que a transação isolada e a verificação de concorrência acontecem. |
| `Cancelado` (após `EmPreparo`) | **Estorna** | Devolve ao estoque. |
| `Cancelado` (quando `Pendente`) | **Não** (nada foi debitado) | Sem efeito no estoque. |

**Alternativa com reserva temporária:** Possível, mas adiciona complexidade (TTL, job de expiração, `Reservas` tabela). Fora do escopo MVP — documentar como evolução futura.

### 2.3. Transação Isolada no `Aprovar` — Passo a Passo (com Agenda Atômica)

```
1. BEGIN TRANSACTION (Isolation Level: Read Committed — default Postgres)
2. SELECT * FROM "Produtos" WHERE "Id" IN (...)  (tracking xmin, sem FOR UPDATE)
   SELECT "Id", "QuantidadeEmEstoque", xmin FROM "Produtos" WHERE "Id" = @id
3. Valida: QuantidadeEmEstoque >= Quantidade solicitada? Se não → Failure
4. Produto.DebitarEstoque(qtd)  → QuantidadeEmEstoque -= qtd
5. agora = IClock.UtcNow
   Pedido.Aprovar(vendorId, agora)  → Status=EmPreparo, AprovadoEmUtc=agora
6. Verifica ItensAgenda.Any(PedidoId == @id) → se já existe, 409
   ItemAgenda.Criar(pedidoId, lojaId, vendedorId, agora) → Status=EmPreparo
   _db.ItensAgenda.Add(itemAgenda)
7. UPDATE "Produtos" SET "QuantidadeEmEstoque"=@novo WHERE "Id"=@id AND xmin=@old
8. UPDATE "Pedidos" SET "Status"=2, "AprovadoEmUtc"=@agora, "AtualizadoEmUtc"=@agora WHERE "Id"=@pedidoId
9. INSERT INTO "ItensAgenda" ("Id","PedidoId","LojaId","VendedorId","Status","AprovadoEmUtc") VALUES (...)
10. INSERT INTO "AuditLogs" (...) — via interceptor (audita Pedido, Produto e ItemAgenda)
11. COMMIT — atomicidade total: se xmin divergir ou UNIQUE PedidoId violar, rollback de tudo
    Se COMMIT ok, agenda cronológica garantida por AprovadoEmUtc
```

**Por que otimista (`xmin`) em vez de pessimista (`SELECT ... FOR UPDATE`)?**

- Otimista não bloqueia leitores; só falha no `UPDATE` se houver conflito — melhor throughput para marketplace com muitos vendedores.
- Pessimista (`FOR UPDATE`) serializa aprovações da mesma loja — vendedor não consegue aprovar dois pedidos diferentes concorrentemente se compartilham produto.
- `xmin` é nativo do Postgres, sem coluna extra `row_version` — ver §2.4.

### 2.4. Implementação — Concorrência Otimista com `xmin` no EF Core 10

#### 2.4.1. Entidade `Produto` — propriedade `xmin`

```csharp
// src/Domain/Entities/Produto.cs — linha 1 (fragmento completo com xmin)
namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class Produto : BaseEntity
{
    public Guid LojaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; } // mapeado como numeric(10,2) — ou Dinheiro VO
    public int QuantidadeEmEstoque { get; private set; }
    public bool Ativo { get; private set; }
    public bool Aprovado { get; private set; }

    // Coluna de concorrência — mapeada para xmin do Postgres
    public uint xmin { get; private set; } // uint mapeia para xid

    private Produto() { } // EF Core

    public Produto(Guid lojaId, string nome, string? descricao, decimal preco, int quantidadeEmEstoque)
    {
        LojaId = lojaId;
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        QuantidadeEmEstoque = quantidadeEmEstoque;
        Ativo = true;
        Aprovado = false;
    }

    public void DebitarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new RegraDeNegocioException("Quantidade deve ser positiva.");
        if (QuantidadeEmEstoque < quantidade)
            throw new RegraDeNegocioException($"Estoque insuficiente. Disponível: {QuantidadeEmEstoque}, solicitado: {quantidade}.");
        QuantidadeEmEstoque -= quantidade;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void EstornarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new RegraDeNegocioException("Quantidade deve ser positiva.");
        QuantidadeEmEstoque += quantidade;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Aprovar() { Aprovado = true; AtualizadoEmUtc = DateTime.UtcNow; }
    public void Desativar() { Ativo = false; AtualizadoEmUtc = DateTime.UtcNow; }
}
```

**Nova entidade — `ItemAgendaConfiguration`**

```csharp
namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ItemAgendaConfiguration : IEntityTypeConfiguration<ItemAgenda>
{
    public void Configure(EntityTypeBuilder<ItemAgenda> b)
    {
        b.ToTable("ItensAgenda");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(a => a.PedidoId).HasColumnType("uuid").IsRequired();
        b.HasIndex(a => a.PedidoId).IsUnique();
        b.Property(a => a.LojaId).HasColumnType("uuid").IsRequired();
        b.Property(a => a.VendedorId).HasColumnType("uuid").IsRequired();
        b.Property(a => a.Status).HasColumnType("smallint").IsRequired();
        b.Property(a => a.AprovadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(a => a.ProntoEmUtc).HasColumnType("timestamptz");
        b.Property(a => a.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(a => a.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.HasOne(a => a.Pedido).WithOne().HasForeignKey<ItemAgenda>(a => a.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Loja>().WithMany().HasForeignKey(a => a.LojaId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(a => new { a.LojaId, a.AprovadoEmUtc });
        b.HasIndex(a => new { a.LojaId, a.Status, a.AprovadoEmUtc });
    }
}
```

#### 2.4.2. Fluent API — `.IsRowVersion()` para `xmin`

```csharp
// src/Infrastructure/Persistence/Configurations/ProdutoConfiguration.cs — linha 1
namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.ToTable("Produtos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(p => p.Nome).HasColumnType("varchar(150)").IsRequired();
        b.Property(p => p.Descricao).HasColumnType("text");
        b.Property(p => p.Preco).HasColumnType("numeric(10,2)").IsRequired();
        b.Property(p => p.QuantidadeEmEstoque).HasColumnType("integer").IsRequired();
        b.Property(p => p.Ativo).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(p => p.Aprovado).HasColumnType("boolean").HasDefaultValue(false);
        b.Property(p => p.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(p => p.AtualizadoEmUtc).HasColumnType("timestamptz");

        // Concorrência otimista via xmin (coluna de sistema Postgres)
        b.Property(p => p.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion() // EF Core usa xmin no WHERE do UPDATE
            .IsConcurrencyToken();

        b.HasOne<Loja>().WithMany().HasForeignKey(p => p.LojaId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

**O que `.IsRowVersion()` faz:**
- No `SELECT`, EF Core lê `xmin` junto com as colunas.
- No `UPDATE`, gera: `UPDATE "Produtos" SET "QuantidadeEmEstoque" = @p1, "AtualizadoEmUtc" = @p2 WHERE "Id" = @p3 AND "xmin" = @xmin_old`
- Postgres compara `xmin` atual da linha com `@xmin_old`. Se outro `UPDATE` já incrementou `xmin`, a condição falha → 0 linhas afetadas → EF Core lança `DbUpdateConcurrencyException`.

**Snapshot da migration (verificação):**

```csharp
// Migrations/..._Initial.cs (fragmento gerado)
b.Property<uint>("xmin")
    .IsConcurrencyToken()
    .ValueGeneratedOnAddOrUpdate()
    .HasColumnType("xid")
    .HasColumnName("xmin");
```

#### 2.4.3. `AppDbContext` — sem configuração extra necessária

`xmin` é gerenciado pelo Postgres automaticamente — não precisa de `ValueGeneratedOnAddOrUpdate` manual além do `.IsRowVersion()`.

### 2.5. Código Completo — Handler `AprovarPedido` com Tratamento de Concorrência

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
                return Result<Guid>.Failure("Vendedor não autorizado para esta loja.");
        }

        if (pedido.Status != StatusPedido.Pendente)
            return Result<Guid>.Failure($"Pedido não está pendente. Status atual: {pedido.Status}.");

        var produtoIds = pedido.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var item in pedido.Itens)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId);
            if (produto is null)
                return Result<Guid>.Failure($"Produto {item.ProdutoId} não encontrado.");
            if (!produto.Ativo)
                return Result<Guid>.Failure($"Produto '{produto.Nome}' está inativo.");
            if (!produto.Aprovado)
                return Result<Guid>.Failure($"Produto '{produto.Nome}' ainda não foi aprovado pelo Admin.");
            if (produto.QuantidadeEmEstoque < item.Quantidade)
                return Result<Guid>.Failure($"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.QuantidadeEmEstoque}, solicitado: {item.Quantidade}.");
            produto.DebitarEstoque(item.Quantidade);
        }

        var agora = _clock.UtcNow;
        pedido.Aprovar(request.AtorVendorId ?? pedido.Loja.VendedorId, agora);

        var jaNaAgenda = await _db.ItensAgenda.AnyAsync(a => a.PedidoId == pedido.Id, ct);
        if (jaNaAgenda)
            return Result<Guid>.Failure("Pedido já está na agenda de produção.");

        var itemAgenda = ItemAgenda.Criar(pedido.Id, pedido.LojaId, pedido.Loja.VendedorId, agora);
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

### 2.6. Cenário de Teste — Dois Aprovar Concorrentes

```csharp
// tests/Application.Tests/Pedidos/AprovarPedidoConcurrencyTests.cs — linha 1
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

public sealed class AprovarPedidoConcurrencyTests : IClassFixture<DbFixture>
{
    [Fact]
    public async Task DoisAprovarConcorrentes_DisputandoUltimaUnidade_UmDeveFalharComConcurrencyException()
    {
        // Arrange: produto com 1 unidade, dois pedidos pendentes cada com 1 unidade do mesmo produto
        var produtoId = Guid.NewGuid();
        var lojaId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var pedidoAId = Guid.NewGuid();
        var pedidoBId = Guid.NewGuid();

        await SeedAsync(produtoId, lojaId, vendorId, pedidoAId, pedidoBId, quantidadeEstoque: 1);

        // Act: dois handlers carregam o mesmo produto (mesmo xmin) concorrentemente
        // Simula isolamento: cada handler usa seu próprio DbContext (como em requisições HTTP distintas)
        using var scopeA = _factory.CreateScope();
        using var scopeB = _factory.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<IAppDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<IAppDbContext>();

        var handlerA = new AprovarPedidoCommandHandler(dbA, NullLogger.Instance);
        var handlerB = new AprovarPedidoCommandHandler(dbB, NullLogger.Instance);

        // Ambos carregam antes de qualquer SaveChanges — mesmo xmin
        var taskA = handlerA.Handle(new AprovarPedidoCommand(pedidoAId, vendorId, vendorId, false), CancellationToken.None);
        var taskB = handlerB.Handle(new AprovarPedidoCommand(pedidoBId, vendorId, vendorId, false), CancellationToken.None);

        var results = await Task.WhenAll(taskA, taskB);

        // Assert: exatamente um sucesso e um falha por concorrência
        var sucessos = results.Count(r => r.IsSuccess);
        var falhas = results.Count(r => !r.IsSuccess);
        Assert.Equal(1, sucessos);
        Assert.Equal(1, falhas);
        Assert.Contains(results.First(r => !r.IsSuccess).Error!, s => s.Contains("Concorrência") || s.Contains("Estoque insuficiente"));

        // Estoque final deve ser 0, nunca -1
        using var verifyScope = _factory.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var produtoFinal = await verifyDb.Produtos.AsNoTracking().FirstAsync(p => p.Id == produtoId);
        Assert.Equal(0, produtoFinal.QuantidadeEmEstoque);
    }

    [Fact]
    public async Task Aprovar_DoisPedidos_SemConflitoDeProduto_AmbosDevemTerSucesso()
    {
        // Arrange: dois produtos distintos, cada pedido consome um produto diferente — sem contenção
        // Ambos devem aprovar com sucesso, estoque de cada vai a 0
    }
}
```

**Nota sobre `Task.WhenAll` com InMemory:** Teste com `Npgsql` real ou `Testcontainers.PostgreSql` — InMemory provider não emula `xmin`. Para CI sem Postgres, mockar `DbUpdateConcurrencyException` lançada por `SaveChangesAsync`.

### 2.7. Tratamento no Middleware — 409 Conflict

```csharp
// src/WebAPI/Middlewares/ExceptionHandlingMiddleware.cs — linha 1 (fragmento — ver architecture.md §4.6)
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Conflito de concorrência");
    ctx.Response.StatusCode = StatusCodes.Status409Conflict;
    await ctx.Response.WriteAsJsonAsync(new
    {
        error = "Conflito de concorrência. O recurso foi modificado por outro usuário. Recarregue e tente novamente.",
        code = "CONCURRENCY_CONFLICT"
    });
}
```

**Frontend:** Ao receber `409`, exibe toast "Estoque atualizado por outro processo. Recarregue a lista de pedidos." e recarrega.

### 2.8. Alternativas e Quando Usar Cada Uma

| Estratégia | Como | Prós | Contras | Quando usar |
|---|---|---|---|---|
| **Otimista `xmin`** (este projeto) | `IsRowVersion()` + `DbUpdateConcurrencyException` | Sem locks, alta concorrência, nativo Postgres | Cliente precisa retry | **Padrão para estoque** |
| Pessimista `FOR UPDATE` | `FromSqlRaw("SELECT ... FOR UPDATE")` | Garante exclusão mútua | Bloqueia leitores, deadlock risk | Estoque crítico com baixa concorrência e tolerância a espera |
| Fila/Serialização | Hangfire/Outbox — aprovações enfileiradas por `LojaId` | Sem conflitos | Latência, infra extra | Volume muito alto (milhares de pedidos/s) |
| Reserva com TTL | `Reservas` tabela + job expiração | UX melhor (estoque reservado ao criar pedido) | Complexidade | Marketplace com checkout longo |

### 2.9. Cancelamento com Estorno — Também com `xmin` + Agenda

```csharp
public async Task<Result> Handle(CancelarPedidoCommand req, CancellationToken ct)
{
    var pedido = await _db.Pedidos.Include(p => p.Itens).Include(p => p.Loja).FirstAsync(p => p.Id == req.PedidoId, ct);
    var itemAgenda = await _db.ItensAgenda.FirstOrDefaultAsync(a => a.PedidoId == pedido.Id, ct);

    var statusAntes = pedido.Status;
    pedido.Cancelar(req.Motivo);

    if (statusAntes == StatusPedido.EmPreparo)
    {
        var produtoIds = pedido.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var item in pedido.Itens)
            produtos.First(p => p.Id == item.ProdutoId).EstornarEstoque(item.Quantidade);

        if (itemAgenda is not null)
        {
            itemAgenda.Cancelar();
            _db.ItensAgenda.Remove(itemAgenda);
        }
    }

    try { await _db.SaveChangesAsync(ct); }
    catch (DbUpdateConcurrencyException) { return Result.Failure("Conflito ao cancelar. Tente novamente."); }
    return Result.Success();
}
```

Quando `Pendente` é cancelado, não há `ItemAgenda` para remover. Quando `EmPreparo`, a remoção da agenda e o estorno ocorrem na mesma transação do cancelamento.

---

## 3. Índices e Performance

- `GIST` em `Lojas.Localizacao` para `ST_DWithin` (ver `features_spec.md`).
- `IX_Produtos_Loja_Ativo_Aprovado` para catálogo.
- `IX_Pedidos_Loja_Status` para dashboard do vendedor (`WHERE loja_id = @id AND status = @s`).
- `IX_ItensAgenda_Loja_AprovadoEm` e `IX_ItensAgenda_Loja_Status_AprovadoEm` para agenda cronológica (`ORDER BY AprovadoEmUtc`).
- `IX_AuditLogs_Data` para relatórios por período.

**`EXPLAIN ANALYZE` obrigatório** antes de ir para produção em queries de proximidade, dashboard e agenda.

---

## 4. Checklist para o Agente

- [ ] Criar 11 tabelas com DDL exato (§1.3) incluindo `ItensAgenda` + `Pedidos.AprovadoEmUtc` + `CHECK`s, `UNIQUE`s, `GIST` e trigger de imutabilidade em `AuditLogs`.
- [ ] Habilitar `pgcrypto` + `postgis` na migration inicial e validar snapshot contém `ItensAgenda` e `AprovadoEmUtc`.
- [ ] Implementar `Produto.xmin uint` + `ProdutoConfiguration.IsRowVersion()` + `ItemAgendaConfiguration` com `PedidoId UNIQUE`.
- [ ] Implementar `AprovarPedidoCommandHandler` atômico: débito `xmin` + `Pedido.Aprovar(agora)` + `ItemAgenda.Criar` + `SaveChangesAsync` único com catch `DbUpdateConcurrencyException` → 409 e `DbUpdateException` UNIQUE → 409.
- [ ] Nunca usar `AsNoTracking` ao carregar `Produtos` para débito — tracking é necessário para `xmin`.
- [ ] Implementar `CancelarPedido` com estorno condicional + remoção de `ItemAgenda` na mesma transação.
- [ ] Implementar `ListarAgendaDoDia` com `ORDER BY AprovadoEmUtc ASC, CriadoEmUtc ASC` e índices `IX_ItensAgenda_Loja_AprovadoEm`.
- [ ] Escrever teste de concorrência com dois `DbContext`s distintos disputando última unidade — um deve falhar, estoque final 0, apenas 1 `ItemAgenda` criado.
- [ ] Configurar `ExceptionHandlingMiddleware` para mapear `DbUpdateConcurrencyException` → `409 Conflict`.
- [ ] Validar que `QuantidadeEmEstoque` nunca fica negativo (`CHECK >=0` + domínio + `catch`) e invariante `Pedidos.AprovadoEmUtc` preenchido quando `Status != Pendente`.
