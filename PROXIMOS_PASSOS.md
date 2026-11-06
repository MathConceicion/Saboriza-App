# Próximos Passos — Saboriza Marketplace

Data: 2026-10-30
Base: auditoria completa registrada em `ESTADO_ATUAL.md` e `PROGRESSO_CONTINUACAO.md`

## Regra de Prioridade

Não avançar para novas funcionalidades antes de concluir os itens P0. O sistema está funcional para demonstração, mas os riscos abaixo impedem uso em produção ou com dados reais.

## P0 — Bloqueadores de Segurança e Conformidade

### 1. Conter o vazamento em AuditLogs

Problema:

- `AuditableEntityInterceptor` serializa todas as propriedades alteradas.
- `AuditLogs` contém `SenhaHash`, `Cpf`, `Email` e dados de saúde em texto claro.
- `GET /api/admin/audit-logs` expõe o JSON bruto.

Ações:

1. Alterar o interceptor para uma allowlist de entidades e campos auditáveis.
2. Excluir ou mascarar `SenhaHash`, `Cpf`, `AlergiasSeveras`, `RestricoesAlimentares` e `ObservacoesSaude`.
3. Não auditar dados de entidades `Usuario` que sejam sensíveis; registrar apenas a ação e o identificador.
4. Ajustar o endpoint de auditoria para nunca devolver campos sensíveis.
5. Definir tratamento dos 259 registros existentes, incluindo os 40 com senha e os 40 com campos de saúde.
6. Registrar o incidente e a decisão de retenção.
7. Criar teste de regressão que falhe se `SenhaHash` ou dados de saúde aparecerem em `AuditLogs`.

Aceite:

- Nenhum novo `AuditLog` contém hash de senha, CPF completo ou dado de saúde.
- Consulta textual em `ValoresNovosJson` e `ValoresAntigosJson` não encontra esses campos.
- Endpoint de auditoria retorna apenas dados permitidos.
- Registros antigos tratados conforme decisão documentada.

### 2. Implementar consentimento LGPD

Problema:

- `ConsentimentosLgpd` tem 0 registros.
- Existe usuário com dados de saúde sem consentimento.
- Não há endpoint para conceder consentimento.
- `ConsentimentoLgpd.Conceder` não é usado.

Ações:

1. Criar comando e endpoint para conceder consentimento de `DadosSaude`, `Geolocalizacao` e `Marketing`.
2. Exigir consentimento explícito antes de persistir qualquer campo de saúde.
3. Bloquear `AtualizarDadosSaude` quando não houver consentimento ativo.
4. Revogar consentimento deve limpar os dados de saúde, como já ocorre no endpoint de revogação.
5. Garantir que anonimização revogue consentimentos e limpe dados sensíveis.
6. Criar teste de integração para consentimento ausente, concedido e revogado.
7. Auditar a concessão e a revogação sem gravar os valores sensíveis.

Aceite:

- Nenhum usuário tem dados de saúde sem consentimento ativo.
- Concessão e revogação funcionam de ponta a ponta.
- Revogação limpa os campos sensíveis.
- Teste cobre o fluxo completo.

### 3. Corrigir RBAC de Admin

Problema:

- `VendedorAprovado` exige `RequireRole("Vendedor")`.
- Admin puro não possui essa role.
- Admin recebe 403 em endpoints de vendedor.
- A matriz de `docs/auth_roles.md` prevê acesso total do Admin.

Ações:

1. Decidir entre duas abordagens:
   - Criar policy específica que aceite `Vendedor` aprovado ou `Admin`.
   - Alterar a policy atual para `RequireRole("Vendedor", "Admin")` e ajustar as verificações de `vendor_id`.
2. Garantir que Admin não dependa de `vendor_id` nem `loja_id`.
3. Corrigir `VendedorController`, `PedidosController.Aprovar` e `ProdutosController`.
4. Garantir que o handler de aprovação use a branch de Admin.
5. Criar testes da matriz RBAC para Cliente, Vendedor dono, Vendedor de outra loja e Admin.

Aceite:

- Admin acessa todos os endpoints previstos na matriz.
- Vendedor de outra loja recebe 403.
- Cliente recebe 403.
- Testes cobrem a matriz completa.

### 4. Corrigir isolamento de pedidos

Problema:

- `CriarPedido` não valida que cada produto pertence à loja do pedido.
- Não valida loja ativa e aprovada.
- Não valida quantidade positiva.

Ações:

1. Validar `loja.Ativo` e `loja.Aprovado` antes de criar o pedido.
2. Validar que todos os produtos pertencem a `request.LojaId`.
3. Rejeitar produto inexistente, inativo ou não aprovado.
4. Rejeitar quantidade menor ou igual a zero.
5. Rejeitar lista vazia e itens duplicados.
6. Criar `CriarPedidoCommandValidator` com FluentValidation.
7. Adicionar testes de cross-tenant e quantidade negativa.

Aceite:

- Pedido com produto de outra loja retorna 400.
- Pedido com loja inativa retorna 400.
- Pedido com quantidade negativa retorna 400.
- Nenhum pedido inconsistente é persistido.

### 5. Atualizar dependências vulneráveis

Problema:

- `Microsoft.AspNetCore.DataProtection 10.0.0`: vulnerabilidade crítica.
- `System.Security.Cryptography.Pkcs 9.0.0`: vulnerabilidades alta e moderada.
- `NU1510`: referência explícita desnecessária a DataProtection.

Ações:

1. Executar `dotnet list package --vulnerable --include-transitive`.
2. Atualizar para versões corrigidas compatíveis com .NET 10.
3. Remover a referência explícita a `Microsoft.AspNetCore.DataProtection` se o framework já fornecer a versão corrigida.
4. Recompilar e reexecutar o build.
5. Validar que o build fica sem warnings de vulnerabilidade.
6. Validar encrypt/decrypt dos campos de saúde após a atualização.

Aceite:

- `dotnet build` sem `NU1902`, `NU1903` e `NU1904`.
- `dotnet list package --vulnerable` sem vulnerabilidades conhecidas.
- Dados existentes continuam descriptografando.

### 6. Criar CHECK constraints e validators

Problema:

- Nenhum CHECK constraint de negócio no banco.
- API aceita preço, estoque e quantidade negativos.
- Invariantes existem apenas no domínio.

Ações:

1. Criar migration `Constraints` com os CHECKs de `docs/database_concurrency.md`.
2. Incluir:
   - `CK_Usuarios_Papel`.
   - `CK_Vendedores_Status`.
   - `CK_Produtos_Preco` e `CK_Produtos_Quantidade`.
   - `CK_Pedidos_Status`.
   - `CK_Pedidos_Comissao_Consistencia`.
   - `CK_Pedidos_AprovadoEm_Consistencia`.
   - `CK_ItensDoPedido_Quantidade`, `PrecoUnitario` e `Subtotal`.
   - `CK_ItensAgenda_Status`.
   - `CK_Configuracoes_Taxa`.
3. Validar os dados existentes antes de aplicar a migration.
4. Adicionar validação de preço e estoque em `CriarProduto`.
5. Adicionar validação de quantidade em `CriarPedido`.
6. Criar teste de integração que tente inserir valores inválidos e espere erro do banco.

Aceite:

- Migration aplica em banco com dados válidos.
- API rejeita valores inválidos antes do banco.
- Banco rejeita inserções diretas inválidas.
- Testes cobrem os CHECKs.

### 7. Corrigir refresh token e CSRF

Problema:

- Refresh aceita access token.
- Antiforgery não é validado.
- Frontend não envia `X-CSRF-TOKEN`.

Ações:

1. Exigir `token_type=refresh` em `AuthController.Refresh`.
2. Rejeitar access token no endpoint de refresh.
3. Aplicar validação antiforgery em endpoints que usam cookie e alteram estado.
4. Fazer o frontend buscar o token em `GET /api/auth/antiforgery/token` e enviar `X-CSRF-TOKEN`.
5. Garantir que login, registro e refresh continuem funcionando com o fluxo.
6. Criar testes para refresh com access token e requisição sem CSRF.

Aceite:

- Access token não gera novo refresh token.
- Requisição de mutação sem CSRF é rejeitada.
- Frontend continua autenticando e renovando sessão.
- Testes cobrem os dois cenários.

## P1 — Correções Antes de Release

### 8. Corrigir conflito de concorrência para 409

Problema:

- `AprovarPedidoCommandHandler` captura `DbUpdateConcurrencyException` e retorna `Result.Failure`.
- O controller responde 400 em vez de 409.

Ações:

1. Remover o catch que converte a exceção em falha de resultado.
2. Deixar a exceção chegar ao `ExceptionHandlingMiddleware`.
3. Ou criar exceção específica `ConflitoException` e mapear para 409.
4. Ajustar os testes de concorrência para esperar 409.
5. Validar que o estoque não fica negativo.

Aceite:

- Aprovação concorrente retorna 409.
- Um pedido vence, o outro falha.
- Estoque final consistente.
- Agenda contém apenas um item.

### 9. Retirar regras de negócio dos controllers

Problema:

- `AdminController` usa `AppDbContext` diretamente.
- `PedidosController.SimularPix` executa regra de negócio.
- `UsuariosController.Revogar` resolve `IAppDbContext` do `RequestServices`.

Ações:

1. Criar comandos e queries MediatR para aprovação de vendedor, aprovação de produto, alteração de taxa, auditoria e faturamento.
2. Criar comando para simulação de PIX com idempotência.
3. Criar comando para revogação de consentimento.
4. Manter controllers finos, apenas extraindo claims e chamando `mediator.Send`.
5. Mapear `RegraDeNegocioException` no middleware para 400 ou 409 conforme o caso.

Aceite:

- Controllers sem `AppDbContext`.
- Regras centralizadas em handlers.
- Aprovações idempotentes.
- Erros de domínio retornam 400 ou 409, não 500.

### 10. Definir fluxo de moderação de produto

Problema:

- `CriarProduto` já aprova o produto.
- O fluxo documentado exige aprovação do Admin.

Ações:

1. Remover `produto.Aprovar()` da criação.
2. Exibir produto pendente no painel do vendedor.
3. Exibir fila de moderação no Admin.
4. Aprovar produto via comando dedicado.
5. Criar teste de produto pendente e produto aprovado.

Aceite:

- Produto novo nasce pendente.
- Produto pendente não aparece no catálogo público.
- Admin aprova e o produto passa a aparecer.
- Testes cobrem o fluxo.

### 11. Substituir o sanitizador caseiro

Problema:

- `Ganss.XSS` oficial não é referenciado.
- Existe um stub local com o mesmo namespace.

Ações:

1. Adicionar o pacote oficial `Ganss.XSS`.
2. Remover `GanssXssStub.cs`.
3. Ajustar a configuração de tags e atributos permitidos.
4. Criar testes com payloads de XSS.
5. Validar sanitização em nome e descrição de produto e loja.

Aceite:

- Projeto referencia `Ganss.XSS`.
- Stub removido.
- Testes de XSS passam.
- Nenhum HTML perigoso é persistido.

### 12. Endurecer uploads

Problema:

- Qualquer usuário autenticado pode enviar imagem.
- Validação apenas por extensão e tamanho.

Ações:

1. Exigir role de vendedor ou Admin para upload de imagem de produto.
2. Validar assinatura do arquivo, não apenas extensão.
3. Rejeitar conteúdo que não seja imagem real.
4. Remover endpoint duplicado de upload.
5. Garantir `X-Content-Type-Options: nosniff`.
6. Adicionar rate limit específico para upload.

Aceite:

- Cliente comum não consegue enviar imagem de produto.
- Arquivo com extensão falsa é rejeitado.
- Endpoint duplicado removido.
- Testes cobrem os casos.

### 13. Corrigir segredos e configuração Docker

Problema:

- `.env` no diretório de trabalho.
- Chaves de DataProtection no diretório.
- Sem `.gitignore`.
- Senha fixa `postgres` no compose.
- Modo Development na stack.

Ações:

1. Criar `.gitignore` com `.env`, `keys/`, `bin/`, `obj/`, `node_modules/`, `dist/` e logs.
2. Remover `.env` e chaves do diretório versionável.
3. Rotacionar `JWT_SECRET_KEY`, `APP_DB_PASSWORD` e `POSTGRES_PASSWORD`.
4. Fazer o compose usar variáveis obrigatórias, sem senha fixa.
5. Restringir a porta do PostgreSQL ao host local ou remover a exposição.
6. Usar `ASPNETCORE_ENVIRONMENT=Production` em compose de produção.
7. Documentar o procedimento de backup e restauração das chaves.

Aceite:

- Nenhum segredo no diretório versionável.
- `.gitignore` cobre os artefatos.
- Compose falha se variáveis obrigatórias estiverem ausentes.
- PostgreSQL não exposto publicamente.

## P2 — Qualidade e Operação

### 14. Criar projeto de testes

- Criar `tests/GastronomiaMarketplace.Tests` com xUnit.
- Usar Testcontainers.PostgreSql para `xmin`, PostGIS, constraints e concorrência.
- Cobrir:
  - Matriz RBAC.
  - Criar pedido cross-tenant.
  - Aprovação concorrente e 409.
  - PIX e invariante financeiro.
  - LGPD, consentimento, anonimização e auditoria.
  - XSS.
  - Rate limiting.
  - Refresh token e CSRF.
- Integrar `dotnet test` ao fluxo de build.

Aceite:

- `dotnet test` executa testes reais.
- Testes de integração usam PostgreSQL real.
- Pipeline local documentado.

### 15. Melhorar frontend

- Corrigir os 27 warnings de lint.
- Remover imports de `PropTypes` não usados.
- Corrigir dependências de `useEffect`.
- Remover `setFotoKey` não usado.
- Atualizar `frontend/README.md` com instruções reais.
- Implementar envio de CSRF.
- Revisar CSP e configuração de API no Docker.
- Revisar o uso de `VITE_API_URL` em build versus runtime.

Aceite:

- `npm run lint` sem warnings.
- README descreve o projeto real.
- CSRF integrado.
- Build e runtime consistentes.

### 16. Configurar Serilog

- Configurar `UseSerilog` no `Program.cs`.
- Não logar `Authorization`, senha, CPF completo ou dados de saúde.
- Definir sinks de console e arquivo/PostgreSQL.
- Documentar retenção.

Aceite:

- Logs estruturados ativos.
- Nenhum dado sensível nos logs.
- Sink de produção definido.

### 17. Melhorar operação do banco

- Definir estratégia de migrations para banco novo.
- Avaliar `ApplyMigrations` em ambiente controlado.
- Documentar seed inicial.
- Criar backup e restauração.
- Revisar índices após crescimento.
- Revisar retenção de `AuditLogs` sem violar imutabilidade.

Aceite:

- `docker compose up` em volume novo cria schema e seed.
- Backup e restauração testados.
- Política de retenção documentada.

### 18. Atualizar documentação

- Atualizar `docs/` apenas se o comportamento planejado mudar.
- Documentar endpoints novos:
  - `GET /api/lojas/{lojaId}`.
  - `GET /api/lojas/{lojaId}/produtos`.
  - `POST /api/uploads/imagens`.
  - CRUD de produtos do vendedor.
  - `GET /api/vendedor/pedidos-pendentes`.
  - `GET /api/pedidos/meus`.
- Documentar variáveis de ambiente.
- Documentar o fluxo de consentimento LGPD.
- Documentar a matriz RBAC revisada.

Aceite:

- Documentação compatível com o código.
- Endpoints e variáveis descritos.
- Fluxo LGPD e RBAC sem ambiguidade.

## Ordem de Execução Recomendada

1. Item 1: conter vazamento de auditoria.
2. Item 2: consentimento LGPD.
3. Item 3: RBAC de Admin.
4. Item 4: isolamento de pedidos.
5. Item 5: dependências vulneráveis.
6. Item 6: CHECK constraints e validators.
7. Item 7: refresh e CSRF.
8. Item 8: conflito 409.
9. Itens 9 a 13: arquitetura, moderação, XSS, uploads e segredos.
10. Itens 14 a 18: testes, frontend, logs, operação e documentação.

## Comandos de Verificação

```powershell
dotnet build GastronomiaMarketplace.sln
dotnet test GastronomiaMarketplace.sln
npm run build --prefix frontend
npm run lint --prefix frontend
docker compose up --build
```

Verificação de vulnerabilidades:

```powershell
dotnet list package --vulnerable --include-transitive
```

Verificação de auditoria:

```powershell
@'
SELECT count(*) FROM "AuditLogs" WHERE "ValoresNovosJson"::text ILIKE '%SenhaHash%';
SELECT count(*) FROM "AuditLogs" WHERE "ValoresNovosJson"::text ILIKE '%AlergiasSeveras%';
SELECT count(*) FROM "AuditLogs" WHERE "ValoresNovosJson"::text ILIKE '%RestricoesAlimentares%';
'@ | docker exec -i saboriza-postgres psql -U postgres -d gastronomia
```

Verificação de consentimento:

```powershell
@'
SELECT count(*) FROM "ConsentimentosLgpd";
SELECT count(*) FROM "Usuarios" WHERE "AlergiasSeveras" IS NOT NULL OR "RestricoesAlimentares" IS NOT NULL OR "ObservacoesSaude" IS NOT NULL;
'@ | docker exec -i saboriza-postgres psql -U postgres -d gastronomia
```

Verificação de constraints:

```powershell
@'
SELECT conrelid::regclass AS tabela, conname, pg_get_constraintdef(oid) AS definicao
FROM pg_constraint
WHERE contype = 'c' AND connamespace = 'public'::regnamespace
ORDER BY 1, 2;
'@ | docker exec -i saboriza-postgres psql -U postgres -d gastronomia
```

## Critério de Conclusão

O projeto só deve ser considerado pronto para release quando:

- Nenhum dado sensível aparecer em `AuditLogs`.
- Todo dado de saúde tiver consentimento ativo.
- Admin acessar todos os endpoints da matriz RBAC.
- Nenhum pedido aceitar produto de outra loja.
- Nenhuma dependência vulnerável permanecer.
- Todos os CHECKs de negócio existirem no banco.
- Refresh token exigir `token_type=refresh`.
- CSRF estiver aplicado.
- Concorrência retornar 409.
- `dotnet test` executar testes reais.
- `dotnet build` e `npm run lint` passarem sem warnings.
- Segredos estiverem fora do diretório versionável.
