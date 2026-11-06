# Progresso Atual — Para Continuação

Data: 2026-10-30
Sessão: auditoria completa e atualização dos documentos de estado
Branch: local, sem repositório Git inicializado
Responsável: auditoria assistida por IA

## 1. Objetivo da Sessão

Verificar se o projeto Saboriza Marketplace está em conformidade com `AGENTS.md` e com os 8 documentos de `docs/`, identificar divergências reais e substituir os três READMEs de estado desatualizados por um retrato fiel do código, banco, runtime, frontend e segurança.

Resultado: auditoria concluída. Os documentos `ESTADO_ATUAL.md`, `PROGRESSO_CONTINUACAO.md` e `PROXIMOS_PASSOS.md` foram reescritos com data de 2026-10-30. Nenhum código de aplicação foi alterado.

## 2. Escopo Auditado

Backend:

- `src/Domain`, `src/Application`, `src/Infrastructure` e `src/WebAPI`.
- Controllers, handlers, validators, policies, middlewares, interceptors, token, DataProtection e rate limiting.
- Projetos e referências de pacotes.
- Migrations e snapshot do EF Core.

Banco:

- Estrutura das 13 tabelas.
- Migrations aplicadas.
- Índices, triggers, constraints e extensões.
- Contagens e amostras de dados.
- Auditoria de dados sensíveis em `AuditLogs`.
- Consentimentos LGPD.

Frontend:

- Rotas, contexts, services e apiClient.
- Uso de cookies, `localStorage` e CSRF.
- Build de produção e lint.
- README e configuração de Vite/Docker.

Runtime:

- Containers Docker e health checks.
- Endpoints públicos de smoke test.
- Configuração do `docker-compose.yml`.

## 3. O Que Foi Executado

Comandos:

```powershell
dotnet build GastronomiaMarketplace.sln -v:m
dotnet test GastronomiaMarketplace.sln -v:q
npm run build --prefix frontend
npm run lint --prefix frontend
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
Invoke-WebRequest -UseBasicParsing http://localhost:5178/health
Invoke-WebRequest -UseBasicParsing http://localhost:5178/api/categorias
Invoke-WebRequest -UseBasicParsing "http://localhost:5178/api/lojas/proximas?cep=01310-100"
Invoke-WebRequest -UseBasicParsing http://localhost:5173/
```

Consultas psql:

- `__EFMigrationsHistory`.
- `information_schema.tables`.
- `information_schema.columns`.
- `pg_constraint` com `contype='c'`.
- `pg_trigger`.
- Contagens de usuários, vendedores, lojas, produtos, pedidos, agenda, auditoria e consentimentos.
- Busca textual em `AuditLogs.ValoresNovosJson` por `SenhaHash`, `AlergiasSeveras` e `RestricoesAlimentares`.
- Busca de usuários com dados de saúde e contagem de consentimentos por usuário.

Inspeção de código:

- `Program.cs`.
- `AuthController`, `PedidosController`, `ProdutosController`, `VendedorController`, `AdminController`, `UsuariosController`, `UploadsController` e `CatalogoController`.
- `CriarPedidoCommandHandler`, `AprovarPedidoCommandHandler`, `CriarProdutoCommandHandler`, `EditarProdutoCommandHandler` e `AtualizarLojaCommandHandler`.
- `TokenService`, `CurrentUser` e `AuditableEntityInterceptor`.
- `SecurityHeadersMiddleware` e `ExceptionHandlingMiddleware`.
- `UploadImagemService` e `HtmlSanitizerService`.
- `frontend/src/routes/index.jsx`, `AuthContext.jsx`, `apiClient.js` e `package.json`.
- `docker-compose.yml`, `nginx.conf`, `appsettings.json` e `.env.example`.

## 4. Resultados da Auditoria

Build e lint:

- `dotnet build`: 0 erros, 12 warnings.
- `npm run build`: 145 módulos, build concluído.
- `npm run lint`: 0 erros, 27 warnings.
- `dotnet test`: nenhum projeto de teste encontrado.

Runtime:

- Três containers saudáveis.
- Health check 200.
- Categorias 200.
- Lojas próximas 200.
- Frontend 200.

Banco:

- Migrations `Initial`, `FotoLoja` e `HorarioVendedor` aplicadas.
- 33 usuários, 5 Admins.
- 15 vendedores e 15 lojas, todos aprovados.
- 21 produtos, todos aprovados.
- 17 pedidos.
- 8 itens de agenda.
- 259 audit logs.
- 0 consentimentos LGPD.

## 5. Achados Críticos

1. `AuditLogs` contém dados sensíveis em texto claro.
   - 40 registros de `Usuario` contêm `SenhaHash`.
   - 40 registros contêm campos de saúde.
   - Pelo menos 1 registro contém alergias, restrições e observações preenchidas.
   - Origem: `AuditableEntityInterceptor` serializa todas as propriedades alteradas.
   - O endpoint `GET /api/admin/audit-logs` devolve o JSON bruto.

2. LGPD sem consentimento.
   - `ConsentimentosLgpd` tem 0 registros.
   - Existe usuário com dados de saúde e 0 consentimentos.
   - Não existe endpoint para conceder consentimento; apenas revogar.
   - `ConsentimentoLgpd.Conceder` não é utilizado.

3. RBAC de Admin.
   - `VendedorAprovado` exige a role `Vendedor`.
   - Admin puro tem roles `Cliente` e `Admin`.
   - Admin recebe 403 nos endpoints de vendedor.
   - A matriz de `docs/auth_roles.md` prevê acesso total do Admin.

4. Isolamento entre lojas.
   - `CriarPedido` não valida que cada produto pertence à loja do pedido.
   - Também não valida loja ativa/aprovada.
   - Produto de outra loja pode entrar no pedido.

5. Integridade de dados.
   - Nenhum CHECK constraint de negócio no banco.
   - `Produto` aceita preço e estoque negativos.
   - `ItemPedido` aceita quantidade negativa.
   - `CriarProduto` não valida preço ou estoque.
   - `CriarPedido` não valida quantidade positiva.

6. Vulnerabilidades de dependências.
   - `Microsoft.AspNetCore.DataProtection 10.0.0`: crítica.
   - `System.Security.Cryptography.Pkcs 9.0.0`: alta e moderada.

7. Refresh token.
   - `AuthController.Refresh` não exige `token_type=refresh`.
   - Access token válido pode ser usado como refresh token.

8. Antiforgery.
   - `AddAntiforgery` e `UseAntiforgery` existem.
   - Nenhum endpoint exige validação.
   - Frontend não envia `X-CSRF-TOKEN`.

9. Segredos e configuração.
   - `.env` no diretório de trabalho.
   - Chaves de DataProtection em `keys/` e `src/WebAPI/keys/`.
   - Sem `.gitignore`.
   - `docker-compose.yml` usa senha fixa `postgres`.
   - `ASPNETCORE_ENVIRONMENT=Development` na stack Docker.

10. Concorrência.
    - `AprovarPedidoCommandHandler` captura `DbUpdateConcurrencyException`.
    - Retorna `Result.Failure`.
    - Controller responde 400 em vez de 409.

11. Produto autoaprovado.
    - `CriarProdutoCommandHandler` chama `produto.Aprovar()` na criação.
    - O fluxo documentado exige moderação do Admin.

12. Arquitetura.
    - `AdminController` concentra regras de negócio com `AppDbContext`.
    - `PedidosController.SimularPix` também executa regra de negócio no controller.
    - `UsuariosController.Revogar` resolve `IAppDbContext` diretamente do `RequestServices`.
    - `RegraDeNegocioException` não é mapeada no middleware e vira 500.

## 6. O Que Não Foi Feito

- Nenhuma correção de código de aplicação.
- Nenhuma migration nova.
- Nenhum teste automatizado criado.
- Nenhuma redefinição de senha, rotação de chave ou limpeza de `AuditLogs`.
- Nenhum fluxo autenticado completo foi reexecutado por falta de credenciais de teste documentadas.
- Os fluxos E2E descritos em 2026-09-17 não foram revalidados nesta sessão.
- Nenhum commit ou push, pois não existe repositório Git.

## 7. Estado de Execução

- `saboriza-webapi`: healthy, `http://localhost:5178`.
- `saboriza-postgres`: healthy, `localhost:5433`.
- `saboriza-frontend`: healthy, `http://localhost:5173`.
- Banco com dados de demonstração e schema aplicado.
- Build e lint executáveis.
- Documentos de estado atualizados.

## 8. Próxima Tarefa

Seguir `PROXIMOS_PASSOS.md`, começando por:

1. Conter o vazamento em `AuditLogs` e definir tratamento dos registros existentes.
2. Implementar concessão de consentimento LGPD e impedir dados de saúde sem consentimento.
3. Corrigir RBAC de Admin e isolamento de produtos por loja.
4. Atualizar dependências vulneráveis.
5. Criar CHECK constraints e validators.
6. Corrigir refresh token, CSRF e conflito 409.
7. Criar projeto de testes e validar a matriz RBAC, LGPD, XSS e concorrência.

## 9. Comandos Úteis

```powershell
dotnet build GastronomiaMarketplace.sln
npm run build --prefix frontend
npm run lint --prefix frontend
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
Invoke-WebRequest -UseBasicParsing http://localhost:5178/health
```

Consulta ao banco:

```powershell
@'
SELECT count(*) FROM "AuditLogs";
SELECT count(*) FROM "ConsentimentosLgpd";
'@ | docker exec -i saboriza-postgres psql -U postgres -d gastronomia
```

## 10. Observação Histórica

A sessão de 2026-09-17 registrou conformidade e fluxos E2E aprovados. A auditoria de 2026-10-30 não revalidou todos esses fluxos e encontrou divergências novas, principalmente em LGPD, RBAC, isolamento de pedidos, constraints e dependências. O estado de 2026-09-17 deve ser tratado como histórico, não como garantia atual.
