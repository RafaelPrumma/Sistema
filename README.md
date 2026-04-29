# Sistema

Projeto .NET 8 em camadas com foco em operação corporativa (auditoria, rastreabilidade, comunicação interna e governança).

## Estrutura
- **0 - Apresentacao** (`Sistema.MVC`): interface web, autenticação, documentação funcional e middleware.
- **1 - Aplicacao** (`Sistema.APP`): serviços de aplicação, DTOs, contratos e orquestração de casos de uso.
- **2 - Dominio** (`Sistema.CORE`): entidades, regras de negócio, resultados e contratos de repositório.
- **3 - Infraestrutura** (`Sistema.INFRA`): EF Core, DbContext, mapeamentos, repositórios, seeds e integração de persistência.
- **4 - Testes** (`Sistema.Tests`): testes unitários.

## Decisões arquiteturais implementadas

### 1) Auditoria automática e soft delete
Entidades auditáveis possuem:
- `DataInclusao`, `UsuarioInclusao`
- `DataAlteracao`, `UsuarioAlteracao`
- `DataExclusao`, `UsuarioExclusao`

No `AppDbContext`:
- `SaveChanges`/`SaveChangesAsync` preenche auditoria automaticamente;
- exclusões físicas são convertidas para exclusão lógica;
- filtro global de consulta oculta itens deletados por padrão.

### 2) Contexto de execução
A auditoria lê o usuário corrente via `IExecutionContext` (implementado por `HttpExecutionContext` com `IHttpContextAccessor`).
Fallback para `system` é usado em fluxos sem usuário HTTP.

### 3) Tratamento global de exceções
O pipeline MVC usa `GlobalExceptionMiddleware`:
- `ArgumentException` -> HTTP 422
- exceções não tratadas -> HTTP 500

Todas as respostas de erro usam `ApiResponse<T>` com `traceId` para facilitar suporte.

### 4) Logs e correlação
- Correlação por `X-Correlation-ID` é propagada na requisição/resposta.
- Rastreamento por `TraceIdentifier` e gravação em auditoria/log.

## Funcionalidades principais
- login, alteração e recuperação de senha
- cadastro de usuários/perfis/funcionalidades/configurações
- comunicação interna (mensagem direta, post de setor, aviso institucional)
- threads de mensagens e reações
- auditoria e retenção de logs por módulo

## Módulo de documentação (na aplicação)
A tela `/Documentacao` descreve:
- arquitetura por camadas
- regras de auditoria/soft delete
- tratamento de erros
- autenticação e autorização granular por funcionalidade (claims perm:{slug})
- comunicação interna e logs operacionais

## Build e testes
```bash
dotnet build Sistema.sln -warnaserror
dotnet test Sistema.sln --no-build
```


## Teste de responsividade (layout)
Usamos Playwright para validar screenshots e detectar overflow horizontal em múltiplos breakpoints.

```bash
npm install
npm run test:layout
```

O script utiliza `artifacts/screenshots/capture-layout.mjs` e cobre desktop/mobile para garantir comportamento responsivo.
