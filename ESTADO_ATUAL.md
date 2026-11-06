# Estado Atual — Saboriza Marketplace

Data: 2026-10-30
Sessão: auditoria completa do repositório, banco, runtime, frontend e segurança
Branch: local, diretório sem repositório Git inicializado
Veredito: demonstração funcional e buildável, mas não conforme para produção. Existem falhas críticas de LGPD, RBAC, isolamento entre lojas e dependências vulneráveis.

## 1. Sumário Executivo

O sistema está operacional em Docker com PostgreSQL/PostGIS, WebAPI e frontend saudáveis. O build .NET passa com 0 erros e 12 warnings, e o build do frontend passa com 145 módulos. A base possui dados de demonstração e os endpoints públicos principais respondem.

Entretanto, a auditoria encontrou bloqueadores reais:

1. `AuditLogs` grava dados de saúde em texto claro e hash de senha. Há 40 registros de auditoria de `Usuario` contendo `SenhaHash` e 40 contendo campos de saúde; pelo menos 1 contém alergias, restrições e observações preenchidas em texto claro.
2. Não existe fluxo de concessão de consentimento LGPD. A tabela `ConsentimentosLgpd` tem 0 registros, mesmo havendo usuário com dados de saúde preenchidos.
3. Admin não consegue acessar endpoints de vendedor porque a policy `VendedorAprovado` exige a role `Vendedor`, que um Admin puro não possui. O bypass de Admin previsto nos handlers fica inalcançável.
4. `CriarPedido` não valida que os produtos pertencem à loja do pedido, nem que a loja está ativa e aprovada. Isso permite misturar itens de lojas diferentes no mesmo pedido.
5. Não existem CHECK constraints no banco, contrariando `docs/database_concurrency.md`. A API aceita produto com preço/estoque negativos e pedido com quantidade negativa.
6. Existem vulnerabilidades conhecidas nas dependências: `Microsoft.AspNetCore.DataProtection 10.0.0` crítica, `System.Security.Cryptography.Pkcs 9.0.0` alta e moderada.
7. Refresh token aceita access token, pois não valida a claim `token_type=refresh`.
8. Antiforgery está configurado, mas nenhum endpoint valida o token e o frontend não envia `X-CSRF-TOKEN`.
9. Não há projeto de testes automatizados; `dotnet test` não executa nenhum teste.
10. O diretório não possui `.gitignore`, e há `.env` e chaves de DataProtection no diretório de trabalho.

Os três documentos de estado anteriores estavam desatualizados: `ESTADO_ATUAL.md` e `PROGRESSO_CONTINUACAO.md` estavam datados de 2026-09-17, e `PROXIMOS_PASSOS.md` ainda instruía seed manual de categorias e promoção manual de Admin.

## 2. Solução e Build

- Solution: `GastronomiaMarketplace.sln` com 4 projetos Clean Architecture.
- `src/Domain`: `net10.0`, `TreatWarningsAsErrors=true`, sem EF Core, com `NetTopologySuite` para `Loja.Localizacao`.
- `src/Application`: MediatR, FluentValidation e `IAppDbContext`.
- `src/Infrastructure`: EF Core 10, Npgsql/PostGIS, BCrypt, DataProtection, `AppDbContext`, interceptors, token e serviços.
- `src/WebAPI`: controllers, middlewares, policies, rate limiting, Swagger e composição de DI.
- `frontend`: React 19, Vite 8, React Router DOM 7, Context API, PropTypes e Axios.
- `docker-compose.yml`: `postgres`, `webapi` e `frontend`.

Resultados executados em 2026-10-30:

- `dotnet build GastronomiaMarketplace.sln -v:m`: 0 erros, 12 warnings.
- `npm run build --prefix frontend`: 145 módulos, `dist/assets/index-D5LDp9JI.js` com 498.35 kB, build concluído.
- `npm run lint --prefix frontend`: 0 erros, 27 warnings (oxlint).
- `dotnet test GastronomiaMarketplace.sln`: não há projeto de teste; nenhum teste é executado.
- `docker ps`: `saboriza-webapi`, `saboriza-postgres` e `saboriza-frontend` com status `healthy`.
- `GET /health`: 200 `Healthy`.
- `GET /api/categorias`: 200.
- `GET /api/lojas/proximas?cep=01310-100`: 200 com itens.
- `GET http://localhost:5173/`: 200.

Warnings de build:

- `NU1904` crítica em `Microsoft.AspNetCore.DataProtection 10.0.0`.
- `NU1902` moderada e `NU1903` alta em `System.Security.Cryptography.Pkcs 9.0.0`.
- `NU1510` por referência explícita a `Microsoft.AspNetCore.DataProtection`, pacote já disponível no framework.

Os 12 warnings vêm da repetição por projeto e configuração; as causas únicas são quatro. O estado anterior afirmava 4 warnings, o que não corresponde ao build atual.

## 3. Backend

### 3.1. Domain

Entidades e valores presentes:

- `Usuario` com `PromoverParaVendedor`, `AtualizarDadosSaude`, `RemoverDadosSaude` e `Anonimizar`.
- `Vendedor`, `Loja`, `Produto`, `Pedido`, `ItemPedido`, `ItemAgenda`, `ConsentimentoLgpd`, `AuditLog`, `CategoriaLoja` e `ConfiguracaoGlobal`.
- Value objects `Dinheiro`, `Endereco`, `Coordenada` e `HorarioFuncionamento`.
- Eventos de domínio para pedido criado/aprovado, upgrade de conta, anonimização e alteração de taxa.

O domínio contém invariantes importantes, como transições de status do pedido, débito de estoque e consistência financeira `bruto = comissao + liquido`.

Lacunas de domínio:

- `Produto` aceita preço e estoque negativos no construtor (`src/Domain/Entities/Produto.cs:22`).
- `ItemPedido` aceita quantidade zero ou negativa e calcula subtotal negativo (`src/Domain/Entities/ItemPedido.cs:19`).
- `Pedido.Criar` valida apenas lista vazia e produto inativo, não quantidade nem vínculo dos produtos com a loja (`src/Domain/Entities/Pedido.cs:42`).
- `RegistrarPagamentoPix` aceita pedido `Pendente`, embora o fluxo documentado indique simulação após aprovação/em preparo (`src/Domain/Entities/Pedido.cs:84`).

### 3.2. Application

Handlers e queries implementados:

- Pedidos: `CriarPedido`, `AprovarPedido`, `ListarMeusPedidos`, `ListarPedidosPendentes`.
- Produtos: `CriarProduto`, `EditarProduto`, `AlternarProduto`, `ExcluirProduto`.
- Agenda: `ListarAgendaDoDia`, `AvancarStatusAgenda`.
- Lojas: `ListarLojasProximas`, `ObterLojaPublica`, `ListarProdutosPorLoja`.
- Categorias: `ListarCategorias`, `ListarLojasPorCategoria`.
- Vendedores: `SolicitarUpgrade`, `AtualizarLoja`, `ObterLoja`.
- Usuários: `AnonimizarUsuario`, `ExportarDadosPortabilidade`.
- Admin: parte das operações ainda está diretamente no controller.

Validações:

- Existe validator apenas para `AprovarPedidoCommand` e `ListarMeusPedidosQuery`.
- `CriarPedido`, `CriarProduto`, `EditarProduto`, `SolicitarUpgrade` e `AtualizarLoja` não possuem validator FluentValidation.
- Várias regras são feitas manualmente dentro dos handlers, e exceções de domínio não são mapeadas pelo middleware.

Problemas relevantes:

- `CriarProdutoCommandHandler` chama `produto.Aprovar()` imediatamente (`src/Application/Produtos/Commands/CriarProduto/CriarProdutoCommandHandler.cs:26`). O fluxo documentado exige moderação do Admin (`Produto.Aprovado=false` na criação).
- `CriarPedidoCommandHandler` não verifica `produto.LojaId == request.LojaId`, nem `loja.Ativo` e `loja.Aprovado` (`src/Application/Pedidos/Commands/CriarPedido/CriarPedidoCommandHandler.cs:17`).
- `AprovarPedidoCommandHandler` captura `DbUpdateConcurrencyException` e devolve `Result.Failure` (`src/Application/Pedidos/Commands/AprovarPedido/AprovarPedidoCommandHandler.cs:83`). O controller converte falha em 400, não em 409, contrariando `docs/database_concurrency.md`.
- `SolicitarUpgrade` e `AtualizarLoja` dependem de validações parciais; erros de domínio podem virar 500.

### 3.3. Infrastructure

Implementações confirmadas:

- `AppDbContext` com `IAppDbContext`, 11 DbSets e configurações por entidade.
- `UsuarioConfiguration` com `HasConversion` e DataProtection para `AlergiasSeveras`, `RestricoesAlimentares` e `ObservacoesSaude`.
- `LojaConfiguration` com `geography(Point,4326)` e índice GIST.
- `ProdutoConfiguration` com `xmin` mapeado como `xid`, `IsRowVersion` e token de concorrência.
- `AuditableEntityInterceptor` gerando `AuditLog` com valores antigos/novos em JSONB.
- `TokenService` emitindo claims `sub`, `email`, `role`, `vendor_id`, `loja_id`, `loja_status`, `is_admin` e `jti`.
- `CurrentUser` lendo `sub`, `vendor_id`, `loja_id`, `role` e `is_admin`.
- `TokenService` emite `token_type=refresh` no refresh token.

Implementações divergentes:

- O sanitizador usa uma classe local `Ganss.XSS.HtmlSanitizer` em `src/Infrastructure/Security/Sanitization/GanssXssStub.cs`, não o pacote oficial `Ganss.XSS`. O projeto não referencia o NuGet.
- O sanitizador local remove tags fora da allowlist e todos os atributos, mas é uma implementação simplificada por expressão regular, não a biblioteca exigida pela stack.
- `AuditableEntityInterceptor` não mascara campos sensíveis. Ele serializa todas as propriedades alteradas, incluindo `SenhaHash`, `Cpf`, `Email`, `AlergiasSeveras`, `RestricoesAlimentares` e `ObservacoesSaude`.
- O interceptor é a origem direta do vazamento em `AuditLogs`.
- Serilog está referenciado no projeto WebAPI, mas não está configurado no pipeline.

### 3.4. WebAPI e autorização

Policies existentes:

- `Authenticated`
- `AdminOnly`
- `VendedorAprovado`
- `VendorOwnerOrAdmin`
- `SelfOrAdmin`

Rate limiting existente:

- `default`: 100 por minuto por IP.
- `login`: 5 por minuto por IP.
- `registro`: 3 por hora por IP.
- `refresh`: 10 por minuto por IP.
- `pix`: 10 por minuto por usuário ou IP.

Pontos confirmados:

- `UseMiddleware<SecurityHeadersMiddleware>` e `UseMiddleware<ExceptionHandlingMiddleware>` rodam antes de `UseAuthentication`.
- `VendedorAprovado` exige `RequireRole("Vendedor")`, `vendor_id` e `loja_status=Aprovado` (`src/WebAPI/Program.cs:83`).
- Admin puro recebe roles `Cliente` e `Admin`, não recebe `Vendedor`; portanto é bloqueado em todos os endpoints com `VendedorAprovado`.
- Endpoints afetados: `PedidosController.Aprovar`, todos os endpoints de `VendedorController` com a policy e todos os endpoints de `ProdutosController`.
- O handler `AprovarPedido` possui lógica para Admin, mas ela não é alcançável por Admin puro.
- `AdminController` faz aprovação de vendedor/produto, alteração de taxa, auditoria e faturamento diretamente com `AppDbContext`, sem MediatR.
- `AdminController.AprovarVendedor` não é idempotente; uma segunda aprovação tende a lançar exceção de domínio e virar 500.
- `ExceptionHandlingMiddleware` mapeia `ValidationException`, `NotFoundException`, `ForbiddenException` e `DbUpdateConcurrencyException`, mas não mapeia `RegraDeNegocioException`.
- O middleware mapeia concorrência para 409, porém o handler de aprovação de pedido intercepta a exceção antes.
- `PedidosController.SimularPix` executa regra de negócio diretamente no controller, fora do MediatR, e também não trata concorrência de PIX duplicado.
- `AuthController.Refresh` valida assinatura, issuer, audience e lifetime, mas não exige `token_type=refresh` (`src/WebAPI/Controllers/AuthController.cs:96`). Um access token válido pode ser usado como refresh token.
- `AuthController` configura cookies `HttpOnly` e `SameSite=Strict` corretamente, com `__Host-` em produção.
- `AddAntiforgery` define cookie e header, mas não há `[ValidateAntiforgeryToken]`, `RequireAntiforgeryToken` ou chamada equivalente. O `UseAntiforgery` sozinho não valida endpoints sem metadata.
- O frontend não envia `X-CSRF-TOKEN`; o endpoint `GET /api/auth/antiforgery/token` existe, mas não é consumido.
- `UploadsController` permite upload para qualquer usuário autenticado, valida apenas extensão e tamanho, e serve o arquivo publicamente.
- `UploadsController` e `ProdutosController` possuem endpoints duplicados de upload de imagem.
- `SecurityHeadersMiddleware` aplica CSP, HSTS em produção e headers básicos, mas a CSP contém `connect-src` fixo para um domínio de exemplo, incompatível com a API local se aplicada a documentos.

## 4. Segurança e LGPD

### 4.1. Vazamento em AuditLogs

Evidência no banco:

- 259 registros em `AuditLogs`.
- 40 registros de `Usuario` contêm a chave `SenhaHash` em `ValoresNovosJson`.
- 40 registros contêm campos de saúde em `ValoresNovosJson`.
- Pelo menos 1 registro possui `AlergiasSeveras`, `RestricoesAlimentares` e `ObservacoesSaude` com valores em texto claro.
- O mesmo registro contém `Cpf` e `Email` em texto claro.
- Exemplo de valores encontrados: `SenhaHash` com hash bcrypt, `AlergiasSeveras` com `Amendoim (anafilaxia)`, `RestricoesAlimentares` com `Vegano` e `ObservacoesSaude` com `Obs teste`.

Impacto:

- O dado de saúde é dado pessoal sensível (LGPD art. 11) e deveria permanecer cifrado no banco e ausente de logs.
- Hash de senha em log de auditoria amplia a superfície de ataque de credenciais.
- `GET /api/admin/audit-logs` devolve o JSON bruto, expondo esses valores para qualquer Admin.
- A trigger de imutabilidade impede apagar os registros vazados sem procedimento controlado, o que exige tratamento de incidente e plano de retenção.

### 4.2. Consentimento LGPD

- `ConsentimentosLgpd` tem 0 registros.
- Existe 1 usuário com dados de saúde preenchidos e 0 consentimentos vinculados.
- Não existe endpoint para conceder consentimento; existe apenas endpoint de revogação.
- `ConsentimentoLgpd.Conceder` existe no domínio, mas não é usado por nenhum fluxo da aplicação.
- A documentação exige consentimento específico antes de persistir dados de saúde.

### 4.3. RBAC e isolamento

- Admin é bloqueado em endpoints de vendedor pela policy `VendedorAprovado`.
- A matriz de `docs/auth_roles.md` prevê Admin com acesso total.
- `CriarPedido` aceita produto de outra loja, pois não compara `Produto.LojaId` com `Pedido.LojaId`.
- `CriarPedido` não valida loja ativa/aprovada.
- A loja do pedido pode não ser a loja dos produtos, quebrando isolamento multi-tenant e relatórios por loja.
- Não foi encontrada validação de quantidade positiva no fluxo de criação de pedido.
- `CriarProduto` não valida preço positivo nem estoque não negativo, e o banco não possui CHECK para isso.
- `AdminController` usa `Guid.Empty` como ator ao alterar taxa de comissão (`src/WebAPI/Controllers/AdminController.cs:105`), perdendo rastreabilidade do Admin real.

### 4.4. Dependências e segredos

- `NU1904` crítica: `Microsoft.AspNetCore.DataProtection 10.0.0`.
- `NU1903` alta e `NU1902` moderada: `System.Security.Cryptography.Pkcs 9.0.0`.
- `.env` existe no diretório de trabalho com `APP_DB_PASSWORD`, `JWT_SECRET_KEY` e `POSTGRES_PASSWORD`.
- Chaves de DataProtection existem em `keys/` e `src/WebAPI/keys/`.
- Não existe `.gitignore` na raiz e o diretório não é um repositório Git.
- `docker-compose.yml` usa `POSTGRES_PASSWORD: postgres` fixo para o superusuário e expõe `5433` para o host.
- `docker-compose.yml` define `ASPNETCORE_ENVIRONMENT: Development`, então cookies não usam `Secure` e detalhes de desenvolvimento ficam habilitados.
- `ApplyMigrations` está `false`; um volume novo não recebe schema automaticamente.

## 5. Banco de Dados

Ambiente:

- Container `saboriza-postgres` com `postgis/postgis:16-3.4`.
- Porta host `5433`, porta interna `5432`.
- Database `gastronomia`.
- Extensões `pgcrypto` e `postgis` habilitadas.
- 13 tabelas, incluindo `__EFMigrationsHistory` e `spatial_ref_sys`.

Migrations aplicadas:

1. `20260910110543_Initial`
2. `20260918141500_FotoLoja`
3. `20261009143842_HorarioVendedor`

Dados atuais:

- 8 categorias.
- 33 usuários, sendo 5 Admin.
- 15 vendedores, todos aprovados.
- 15 lojas, todas ativas e aprovadas.
- 21 produtos, todos aprovados.
- 17 pedidos.
- 8 itens de agenda.
- 259 audit logs.
- 0 consentimentos LGPD.

Estruturas confirmadas:

- `Produtos.xmin` como `xid`.
- `Lojas.Localizacao` como `geography(Point,4326)`.
- Índice GIST em `Lojas.Localizacao`.
- `ItensAgenda.PedidoId` com restrição única.
- Trigger `trg_audit_immutable` em `AuditLogs`.
- Trigger bloqueia UPDATE e DELETE.

Ausências confirmadas:

- Nenhum CHECK constraint nas tabelas de negócio; apenas o CHECK interno de `spatial_ref_sys`.
- Sem `CK_Usuarios_Papel`.
- Sem `CK_Vendedores_Status`.
- Sem `CK_Produtos_Quantidade` e CHECK de preço.
- Sem `CK_Pedidos_Status`.
- Sem `CK_Pedidos_Comissao_Consistencia`.
- Sem `CK_Pedidos_AprovadoEm_Consistencia`.
- Sem CHECK de quantidade e subtotal em `ItensDoPedido`.
- Sem `CK_ItensAgenda_Status`.
- Sem CHECK de taxa em `ConfiguracoesGlobais`.

Consequência: os invariantes existem apenas no domínio e podem ser burlados por fluxos que não chamam os métodos corretos, por dados diretos no banco ou por comandos sem validação.

## 6. Frontend

Stack real:

- React 19.2.8.
- Vite 8.2.2.
- React Router DOM 7.18.3.
- Axios 1.20.0.
- PropTypes 15.8.1.
- TailwindCSS 3.4.10.
- oxlint 1.79.0.

Rotas implementadas:

- `/`
- `/buscar`
- `/categoria/:slug`
- `/loja/:lojaId`
- `/checkout`
- `/meus-pedidos`
- `/conta`
- `/vender`
- `/login`
- `/registro`
- `/vendedor/dashboard`
- `/vendedor/agenda`
- `/vendedor/produtos`
- `/vendedor/pedidos`
- `/vendedor/configuracoes`
- `/admin`

Autenticação no frontend:

- `apiClient` usa `withCredentials: true`.
- Não há tokens em `localStorage`; apenas `user` em memória.
- `CartContext` e `AddressContext` usam `localStorage` para dados não sensíveis.
- Refresh automático em 401 está implementado com fila de requisições.
- O frontend não envia `X-CSRF-TOKEN`.
- O endpoint de antiforgery não é usado.

Build e lint:

- Build de produção concluído.
- 27 warnings de lint, 0 erros.
- Categorias de warning: `only-export-components`, `set-state-in-effect`, `exhaustive-deps`, `PropTypes` importado sem uso e `setFotoKey` declarado sem uso.
- `frontend/README.md` continua sendo o template padrão do Vite.

## 7. Runtime e Docker

Containers em execução:

- `saboriza-webapi`: `healthy`, host `5178` para container `8080`.
- `saboriza-postgres`: `healthy`, host `5433` para container `5432`.
- `saboriza-frontend`: `healthy`, host `5173` para container `80`.

Smoke tests executados:

- `GET /health`: 200.
- `GET /api/categorias`: 200.
- `GET /api/lojas/proximas?cep=01310-100`: 200 com lojas.
- `GET http://localhost:5173/`: 200.

Observações de runtime:

- O frontend em Docker usa `VITE_API_URL` como variável de build, não apenas de runtime; mudar a variável no compose sem rebuild pode não alterar o bundle.
- O nginx do frontend faz proxy de `/api/` para `webapi:8080`, mas o bundle pode apontar para `http://localhost:5178` conforme o build.
- `ApplyMigrations=false` exige aplicação manual de migrations em banco novo.
- A stack está em modo `Development`, não em configuração de produção.

## 8. Conformidade com AGENTS.md e docs

Itens atendidos:

- Solution com 4 projetos e frontend.
- Domain sem EF Core, MediatR ou Npgsql.
- `AprovarPedidoCommand` com validator e handler.
- `vendor_id` emitido apenas quando vendedor aprovado.
- `HasConversion` com DataProtection nos campos de saúde.
- `AuditLogs` com trigger de imutabilidade.
- `Lojas.Localizacao` como `geography(Point,4326)` com índice GIST.
- `Produtos.xmin` com `IsRowVersion`.
- Rate limiting configurado e aplicado.
- Build sem erros.
- Nenhum comentário `//` nos arquivos de código próprios; ocorrências apenas em migrations geradas e `obj/`.

Itens divergentes:

- Admin bloqueado em endpoints de vendedor pela policy `VendedorAprovado`.
- `AprovarPedido` devolve 400 em conflito de concorrência em vez de 409.
- `AuditLogs` sem mascaramento de dados sensíveis.
- `ConsentimentosLgpd` sem fluxo de concessão e com 0 registros.
- CHECK constraints ausentes no banco.
- Sem testes automatizados.
- `Ganss.XSS` oficial não referenciado.
- Antiforgery não aplicado.
- Dependências com vulnerabilidades conhecidas.
- `.env` e chaves de DataProtection no diretório de trabalho.
- `docker-compose.yml` com senha fixa do PostgreSQL e modo Development.
- Nenhum `.gitignore` na raiz.

## 9. Riscos Prioritários

P0, bloquear produção:

- Vazamento de dados sensíveis e hashes em `AuditLogs`.
- Ausência de consentimento LGPD para dados de saúde.
- RBAC de Admin incompatível com a matriz documentada.
- Pedido aceitando produtos de outra loja e loja inativa.
- Dependências com vulnerabilidades crítica/alta.
- Refresh token aceitando access token.
- CHECK constraints ausentes e comandos sem validação de preço, estoque e quantidade.

P1, corrigir antes de release:

- Conflito de concorrência retornando 400.
- Regras de negócio no `AdminController` e aprovações não idempotentes.
- `RegraDeNegocioException` sem mapeamento no middleware.
- Antiforgery não aplicado.
- Produto criado já aprovado.
- Upload aberto a qualquer usuário autenticado.
- Sanitizador caseiro no lugar de `Ganss.XSS`.
- `.env`, chaves e senha fixa do Postgres.
- Rate limit por usuário no PIX ineficaz porque roda antes da autenticação.

P2, qualidade e operação:

- Sem projeto de testes.
- Sem `.gitignore`.
- README do frontend genérico.
- Serilog não configurado.
- Warnings de lint no frontend.
- `ApplyMigrations=false` e ausência de bootstrap de banco novo.
- Documentação de endpoints e variáveis de ambiente incompleta.

## 10. Próximo Passo

O plano priorizado está em `PROXIMOS_PASSOS.md`. A ordem recomendada é: conter o vazamento de auditoria e LGPD, corrigir RBAC e isolamento de pedidos, atualizar dependências vulneráveis, criar CHECK constraints e validators, corrigir refresh/CSRF, e só então retomar features e testes automatizados.

Nenhum código de aplicação foi alterado nesta auditoria. As alterações desta sessão estão restritas aos três documentos de estado.
