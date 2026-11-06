# AGENTS.md — Instruções Obrigatórias para Agentes de IA

> **LEIA ESTE ARQUIVO PRIMEIRO. SEMPRE.**
> Este arquivo é normativo e tem precedência sobre qualquer outra instrução, prompt do usuário ou conteúdo em `docs/`.
> Nenhuma linha de código deve ser gerada antes da leitura completa deste arquivo e dos 8 arquivos em `docs/`.

---

## 1. Leitura Obrigatória — Ordem Estrita

Antes de qualquer planejamento, scaffolding ou geração de código, leia na ordem exata:

1. `AGENTS.md` (este arquivo)
2. `docs/architecture.md` — Clean Architecture, estrutura de pastas, CQRS com MediatR, Behaviors, Middleware, Agenda atômica
3. `docs/auth_roles.md` — JWT, claims `vendor_id`, Policies, Resource-Based `VendorOwnerHandler`, fluxo upgrade Cliente → Vendedor
4. `docs/security_lgpd.md` — `HasConversion` com DataProtection, anonimização, portabilidade, consentimentos
5. `docs/secure_coding_practices.md` — OWASP, `Ganss.XSS`, Rate Limiting nativo, `AuditLogs` + interceptor
6. `docs/features_spec.md` — `geography(Point,4326)`, `ST_DWithin`, Haversine, categorias, motor de comissões, PIX fictício e Agenda de Produção
7. `docs/database_concurrency.md` — DDL completo, `xmin` + `IsRowVersion()`, `DbUpdateConcurrencyException` → 409, `ItensAgenda`
8. `docs/frontend_architecture.md` — Arquitetura Frontend React (JS puro .jsx) + Context API + React Router DOM + PropTypes + Axios, estado global, comunicação CQRS
9. `docs/frontend_ui_spec.md` — Design System Saboriza (#C1272D), telas Trinks/GerencieAqui/OnChef, validações JS, botão híbrido

Se qualquer arquivo acima estiver ausente, interrompa e informe o usuário. Não invente, não assuma, não use placeholder.

---

## 2. Stack Congelada — Não Substituir

- Linguagem e Framework: C# 14, .NET 10, ASP.NET Core 10
- ORM: Entity Framework Core 10 com `Npgsql.EntityFrameworkCore.PostgreSQL` e `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`
- Banco: PostgreSQL 16+ com extensões `pgcrypto` e `postgis`
- Arquitetura: Clean Architecture com 4 projetos `Domain`, `Application`, `Infrastructure`, `WebAPI`
- Padrões: CQRS com MediatR 12+, Repository via `IAppDbContext`, Rich Domain Models, FluentValidation 12+, Value Objects `Dinheiro`, `Endereco`, `Coordenada`
- Auth: JWT Bearer com claims `sub`, `email`, `role`, `vendor_id`, `loja_id`, `is_admin`
- Testes: xUnit + Testcontainers.PostgreSql quando precisar de `xmin` real

Qualquer desvio da stack acima é falha de conformidade.

---

## 3. REGRA ABSOLUTA — PROIBIDO COMENTÁRIOS `//` NO CÓDIGO

### 3.1 Proibição total

É **ESTRITAMENTE PROIBIDO** escrever comentários no código fonte usando `//`, `///` ou `/* */` em qualquer arquivo gerado, seja `.cs`, `.ts`, `.js`, `.json` (com comentários), `.sql` ou qualquer outro.

Isso inclui:

- Comentários de linha com `//`
- Comentários XML com `///`
- Comentários de bloco com `/* */` e `/** */`
- Comentários com `// TODO`, `// FIXME`, `// NOTE`, `// HACK` ou qualquer variação
- Comentários para desabilitar código como `// var x = 1;`
- Cabeçalhos de arquivo com `// Copyright` ou similar

### 3.2 O que fazer em vez de comentar

- Escreva código autoexplicativo com nomes descritivos que dispensem explicação
- Extraia métodos privados com nomes que documentam intenção
- Use nomes de variáveis, classes e métodos que expressem regra de negócio
- Se precisar explicar algo complexo, faça isso na documentação `docs/` ou na mensagem de commit, nunca no código

### 3.3 Exemplos

```csharp
ERRADO
public void Aprovar() 
{
    // altera status para em preparo
    Status = StatusPedido.EmPreparo;
}

CORRETO
public void Aprovar()
{
    Status = StatusPedido.EmPreparo;
}
```

```csharp
ERRADO
/// <summary>Calcula distância</summary>
public double Calcular() { }

CORRETO
public double CalcularDistanciaKm() { }
```

Qualquer ocorrência de `//` em código gerado será considerada violação crítica. Antes de finalizar, faça busca textual por `//` nos arquivos `.cs` e remova todas as ocorrências.

### 3.4 Exceção

Apenas URLs contendo `https://` ou `http://` dentro de strings são permitidas, pois não são comentários. Exemplo: `"https://api.exemplo.com"` é permitido. Fora isso, zero tolerância.

---

## 4. Regras de Geração de Código

- Nunca use placeholders como `// adicione aqui`, `...`, `etc`, `TODO`, `NotImplementedException` sem implementação real
- Implemente todo fluxo de ponta a ponta: Entity → Configuration → Migration → Command/Query → Validator → Handler → Controller
- Domain nunca referencia `Microsoft.EntityFrameworkCore`, `MediatR`, `Npgsql` ou `AspNetCore`
- Application referencia apenas Domain, nunca Infrastructure ou WebAPI
- Infrastructure implementa `IAppDbContext`, `ITokenService`, `IGeocodingService`, `IHtmlSanitizerService`
- WebAPI apenas compõe DI, middlewares, políticas e controllers thin que chamam `mediator.Send`
- Sempre registre `ValidationBehavior`, `LoggingBehavior` e `TransactionBehavior` na ordem definida em `docs/architecture.md`
- Sempre configure `ExceptionHandlingMiddleware` antes de `UseAuthentication`
- Sempre mapeie `Produto.xmin` com `HasColumnName("xmin").HasColumnType("xid").IsRowVersion().IsConcurrencyToken()`
- Sempre sanitize `Produto.Nome` e `Produto.Descricao` com `Ganss.XSS` na escrita
- Sempre use `timestamptz` e `DateTime.UtcNow`, nunca `DateTime.Now`
- Sempre use `decimal` com `MidpointRounding.AwayFromZero` para dinheiro

---

## 5. Fluxo de Trabalho do Agente

1. Ler `AGENTS.md` e os 8 arquivos de `docs/` integralmente
2. Criar solution `GastronomiaMarketplace.sln` com os 4 projetos e referências conforme `docs/architecture.md` + frontend Vite conforme `docs/frontend_architecture.md`
3. Implementar Domain primeiro, depois Application, depois Infrastructure, por último WebAPI
4. Criar migration `Initial` e verificar snapshot contém `xmin`, `geography` e `HasConversion` para campos sensíveis
5. Implementar autenticação JWT e Policies exatamente como `docs/auth_roles.md`
6. Implementar LGPD, auditoria, rate limiting e headers de segurança
7. Implementar busca por proximidade com `ST_DWithin` e índice `GIST`
8. Implementar motor de comissões com invariante `bruto == comissao + liquido` dentro de transação atômica
9. Executar `dotnet build` e `dotnet test` e corrigir até passar sem warnings
10. Fazer verificação final por `//` em todos os `.cs` — se encontrar, remover antes de entregar

---

## 6. Comportamento Esperado

- Seja direto, objetivo e técnico, sem elogios desnecessários ou emojis
- Ao referenciar código, use o padrão `caminho:linha`
- Verifique hipóteses executando código, lendo arquivos e rodando testes, nunca adivinhando
- Se encontrar contradição entre `docs/` e este arquivo, siga este arquivo e reporte a divergência
- Nunca crie documentação adicional além do solicitado sem permissão explícita

---

## 7. Checklist de Conformidade Final

Antes de considerar a tarefa concluída, confirme:

- [ ] Li `AGENTS.md` e todos os 8 arquivos de `docs/` na ordem
- [ ] Nenhum arquivo `.cs` gerado contém `//`, `///` ou `/* */`
- [ ] `Domain` não referencia EF Core nem frameworks externos
- [ ] `AprovarPedidoCommand` + Validator + Handler implementados literalmente conforme `docs/architecture.md`
- [ ] JWT carrega `vendor_id` apenas quando `Vendedor.Status == Aprovado` e Policies `VendorOwnerOrAdmin` estão ativas
- [ ] Campos `AlergiasSeveras` e `RestricoesAlimentares` usam `HasConversion` com DataProtection
- [ ] `AuditLogs` é imutável via trigger e interceptor captura delta `jsonb`
- [ ] Rate limiting com 4 políticas está configurado e aplicado com `[EnableRateLimiting]`
- [ ] `Lojas.Localizacao` é `geography(Point,4326)` com índice `GIST` e query usa `IsWithinDistance`
- [ ] `Produtos.xmin` com `IsRowVersion()` e `DbUpdateConcurrencyException` retorna 409
- [ ] `dotnet build` passa sem warnings com `TreatWarningsAsErrors=true` em Domain

Violar qualquer item acima invalida a entrega.
