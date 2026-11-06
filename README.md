# Saboriza

Marketplace de gastronomia artesanal para comércio local. O projeto conecta clientes a cozinhas e lojas próximas, permitindo descobrir estabelecimentos por localização, navegar pelo catálogo, montar pedidos e acompanhar a produção. Vendedores gerenciam loja, produtos, pedidos e agenda de produção; administradores moderam cadastros, acompanham faturamento e auditoria.

> **Status:** MVP acadêmico em desenvolvimento. O PIX é fictício e o sistema não está pronto para produção. Riscos e pendências conhecidos estão em [ESTADO_ATUAL.md](ESTADO_ATUAL.md) e [PROXIMOS_PASSOS.md](PROXIMOS_PASSOS.md).

## Funcionalidades

- Busca por proximidade com CEP, endereço ou coordenadas, raio configurável (padrão 5 km, máximo 50 km) e ordenação por distância.
- Categorias gastronômicas e catálogo por loja, com produtos ativos e imagens.
- Cadastro, login, logout e renovação de sessão com JWT em cookies `HttpOnly`.
- Fluxo de upgrade de Cliente para Vendedor, com aprovação pelo administrador.
- Carrinho com regra de loja única, criação de pedido e histórico do cliente.
- Aprovação de pedido pelo vendedor com débito de estoque e entrada atômica na agenda de produção.
- Agenda de produção cronológica com avanço de status: `EmPreparo`, `ProntoParaRetirada` e `Finalizado`.
- PIX fictício com cálculo de comissão e invariante `bruto = comissão + líquido`.
- Painel administrativo com moderação de vendedores e produtos, taxa de comissão, auditoria e relatório de faturamento.
- LGPD: criptografia de campos de saúde, anonimização, portabilidade de dados e revogação de consentimento.
- Segurança: policies por papel, rate limiting, CORS, headers de segurança, auditoria imutável e concorrência otimista com `xmin`.

## Stack

| Camada | Tecnologias |
|---|---|
| Backend | C# 14, .NET 10, ASP.NET Core 10, MediatR, FluentValidation |
| Persistência | EF Core 10, Npgsql, NetTopologySuite, PostgreSQL 16 + PostGIS |
| Segurança | JWT Bearer, BCrypt, DataProtection, rate limiting nativo |
| Frontend | React 19, Vite 8, React Router DOM 7, Context API, Axios, TailwindCSS 3.4, PropTypes |
| Infraestrutura | Docker Compose, Nginx |

## Arquitetura

O backend segue Clean Architecture com quatro projetos e dependências unidirecionais:

```
Domain          (regras de negócio, sem dependências externas)
  ^
Application     (CQRS, handlers, validators, behaviors)
  ^
Infrastructure  (EF Core, PostGIS, JWT, DataProtection, serviços externos)
  ^
WebAPI          (composição de DI, controllers thin, middlewares, policies)
```

Decisões relevantes:

- CQRS com MediatR e pipeline `ValidationBehavior` → `LoggingBehavior` → `TransactionBehavior`.
- Domain rico com Value Objects como `Dinheiro`, `Endereco`, `Coordenada` e `HorarioFuncionamento`.
- `Lojas.Localizacao` como `geography(Point,4326)` com índice GIST e consultas `ST_DWithin`/`ST_Distance`.
- Concorrência otimista em `Produtos` via `xmin` e `IsRowVersion()`.
- Campos sensíveis de saúde cifrados com `HasConversion` + DataProtection.
- `AuditLogs` imutável por trigger, alimentado por interceptor do EF Core.
- Frontend em JavaScript puro (`.jsx`) com estado global via Context API e tokens fora do `localStorage`.

## Estrutura do repositório

```
.
├── docs/                  Documentação técnica e normativa
├── frontend/              SPA React + Vite + Tailwind
├── src/
│   ├── Domain/            Entidades, Value Objects, eventos e regras
│   ├── Application/       Commands, queries, handlers e validators
│   ├── Infrastructure/    EF Core, migrations, segurança e integrações
│   └── WebAPI/            Controllers, middlewares, policies e composição
├── docker-compose.yml     PostgreSQL/PostGIS, WebAPI e frontend
├── GastronomiaMarketplace.sln
└── .env.example           Variáveis de ambiente necessárias
```

## Como executar

### Pré-requisitos

- Docker Desktop com Docker Compose
- .NET SDK 10 (apenas para executar ou aplicar migrations fora do Docker)
- Node.js 20+ (apenas para desenvolvimento local do frontend)

### Com Docker

1. Crie o arquivo de ambiente e preencha os valores:

```powershell
Copy-Item .env.example .env
```

Defina `APP_DB_PASSWORD` e `JWT_SECRET_KEY`. A chave JWT precisa ter no mínimo 32 caracteres.

2. Suba a stack:

```powershell
docker compose up -d --build
```

3. Serviços disponíveis:

| Serviço | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API | http://localhost:5178 |
| Swagger (Development) | http://localhost:5178/swagger |
| Health check | http://localhost:5178/health |
| PostgreSQL | localhost:5433 |

O frontend é compilado durante o build da imagem. Alterações no código exigem `docker compose up -d --build frontend`, pois não há volume de hot reload no container.

### Banco de dados em volume novo

O compose usa `ApplyMigrations=false`, portanto as migrations não são aplicadas automaticamente. Em um volume novo, crie o usuário da aplicação e aplique o schema:

```powershell
docker exec -it saboriza-postgres psql -U postgres -d gastronomia -c "CREATE ROLE gastronomia_app LOGIN PASSWORD 'DEFINA_A_SENHA';"
docker exec -it saboriza-postgres psql -U postgres -d gastronomia -c "GRANT ALL PRIVILEGES ON SCHEMA public TO gastronomia_app;"
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
docker exec -it saboriza-postgres psql -U postgres -d gastronomia -c "GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO gastronomia_app; GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO gastronomia_app;"
```

A senha criada para `gastronomia_app` deve ser a mesma definida em `APP_DB_PASSWORD` no `.env`.

### Desenvolvimento local

Backend:

```powershell
$env:ConnectionStrings__Default="Host=localhost;Port=5433;Database=gastronomia;Username=gastronomia_app;Password=SUA_SENHA"
$env:Jwt__SecretKey="UMA_CHAVE_COM_NO_MINIMO_32_CARACTERES"
dotnet run --project src/WebAPI
```

Frontend:

```powershell
Copy-Item frontend/.env.example frontend/.env
npm install --prefix frontend
npm run dev --prefix frontend
```

O servidor Vite usa a porta `5173` e faz proxy de `/api` para `http://localhost:5178`.

## Variáveis de ambiente

| Variável | Onde é usada | Descrição |
|---|---|---|
| `APP_DB_PASSWORD` | Backend | Senha do usuário `gastronomia_app` no PostgreSQL |
| `JWT_SECRET_KEY` | Backend | Chave HMAC para assinatura dos tokens, com no mínimo 32 caracteres |
| `VITE_API_URL` | Frontend | URL da API embutida no build |
| `VITE_ENV` | Frontend | Ambiente do build do frontend |

Os arquivos `.env` reais não são versionados. Use apenas os `.env.example` como referência.

## API

A API segue o padrão REST com controllers thin e respostas de erro padronizadas. Principais grupos:

| Grupo | Rotas |
|---|---|
| Autenticação | `/api/auth/registro`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, `/api/auth/me` |
| Catálogo | `/api/categorias`, `/api/categorias/{slug}/lojas`, `/api/lojas/proximas`, `/api/lojas/{id}`, `/api/lojas/{id}/produtos` |
| Pedidos | `/api/pedidos`, `/api/pedidos/meus`, `/api/pedidos/{id}/aprovar`, `/api/pedidos/{id}/simular-pix` |
| Vendedor | `/api/vendedor/solicitar-upgrade`, `/api/vendedor/loja`, `/api/vendedor/pedidos-pendentes`, `/api/vendedor/agenda` |
| Produtos | `/api/produtos` e variações de edição, ativação e exclusão |
| Administração | `/api/admin/vendedores`, `/api/admin/produtos`, `/api/admin/audit-logs`, `/api/admin/relatorios/faturamento` |
| LGPD | `/api/usuarios/{id}/anonimizar`, `/api/usuarios/{id}/exportar-dados`, revogação de consentimento |

## Rotas do frontend

| Rota | Descrição |
|---|---|
| `/` | Landing page para parceiros |
| `/buscar` | Busca de lojas por proximidade |
| `/categoria/:slug` | Lojas de uma categoria |
| `/loja/:lojaId` | Catálogo da loja e carrinho |
| `/checkout` | Finalização do pedido |
| `/meus-pedidos` | Histórico do cliente |
| `/conta` | Dados da conta |
| `/vender` | Solicitação de upgrade para vendedor |
| `/login` e `/registro` | Autenticação |
| `/vendedor/dashboard` | Painel do vendedor |
| `/vendedor/agenda` | Agenda de produção |
| `/vendedor/produtos` | Gestão de produtos |
| `/vendedor/pedidos` | Pedidos pendentes |
| `/vendedor/configuracoes` | Configurações da loja |
| `/admin` | Painel administrativo |

## Papéis e permissões

- `Cliente`: navega no catálogo, cria pedidos e gerencia a própria conta.
- `Vendedor`: gerencia a própria loja, produtos, pedidos e agenda.
- `Admin`: modera vendedores e produtos, altera a taxa de comissão, consulta auditoria e relatórios.

As policies `AdminOnly`, `VendedorAprovado`, `VendorOwnerOrAdmin` e `SelfOrAdmin` controlam o acesso na API. O claim `vendor_id` só é emitido quando o vendedor está aprovado.

## Documentação

- [Arquitetura](docs/architecture.md)
- [Autenticação e papéis](docs/auth_roles.md)
- [Segurança e LGPD](docs/security_lgpd.md)
- [Práticas de código seguro](docs/secure_coding_practices.md)
- [Especificação de funcionalidades](docs/features_spec.md)
- [Banco de dados e concorrência](docs/database_concurrency.md)
- [Arquitetura do frontend](docs/frontend_architecture.md)
- [Especificação visual do frontend](docs/frontend_ui_spec.md)

## Testes e verificação

```powershell
dotnet build GastronomiaMarketplace.sln
npm run lint --prefix frontend
npm run build --prefix frontend
```

O repositório ainda não possui suíte de testes automatizados. O `dotnet test` não executa testes até que um projeto de testes seja adicionado.

## Aviso

Este projeto tem finalidade acadêmica e de demonstração. O pagamento PIX é simulado e não há integração com instituição financeira. Antes de qualquer uso real, revise os riscos de segurança e LGPD documentados em [ESTADO_ATUAL.md](ESTADO_ATUAL.md) e [PROXIMOS_PASSOS.md](PROXIMOS_PASSOS.md).
