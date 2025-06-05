# Sistema

Projeto de exemplo em .NET 8 utilizando arquitetura em camadas.

## Estrutura
- **0 - Apresentacao**: `Sistema.API` - API ASP.NET Core com Swagger e Serilog.
- **1 - Aplicacao**: `Sistema.APP` - serviços de aplicação, perfis do AutoMapper e registro de dependências.
- **2 - Dominio**: `Sistema.CORE` - entidades, serviços e interfaces que definem as regras de negócio.
- **3 - Infraestrutura**: `Sistema.INFRA` - Entity Framework Core, repositórios, Unit of Work e seeds.

### Organização de pastas
- `2 - Dominio/Sistema.CORE/Entities` contém todas as entidades do domínio.
- `2 - Dominio/Sistema.CORE/Services` possui as implementações e interfaces dos serviços.
- `1 - Aplicacao/Sistema.APP/DTOs` concentra os DTOs utilizados pela API.

## Funcionalidades
- CRUD de `Perfil` e `Usuario` com validações de negócio.
- Campos de auditoria nas entidades e registro de operações em `Log`.
- `UnitOfWork` centraliza o commit das alterações.
- Seeds criam perfis e usuários iniciais (Admin e Comercial).
- Serilog grava logs mensais na pasta `log` e as operações também ficam salvas na tabela `Logs` com usuário e detalhes da exceção.
- Perfis e usuários podem ser ativados ou desativados e as APIs permitem filtrar registros por data, perfil e status.
- Listagens retornam `PagedResult<T>` para suportar paginação via `page` e `pageSize`.
- Perfis possuem funcionalidades com permissões de leitura e escrita gravadas em `PerfilFuncionalidades`.
- Autenticação via JWT protege a API; obtenha um token em `/api/auth/login`.

Para compilar a solução:

```bash
dotnet build Sistema.sln -c Release
```

Para executar os testes automatizados:

```bash
dotnet test Sistema.sln -c Release
```
