# Sistema

Projeto de exemplo em .NET 9 utilizando arquitetura em camadas.

## Estrutura
- **0 - Apresentacao**:
  - `Sistema.API` - API ASP.NET Core com Swagger e Serilog.
  - `Sistema.MVC` - frontend web MVC para autenticação, cadastro, recuperação de senha e edição de tema.
- **1 - Aplicacao**: `Sistema.APP` - perfis do AutoMapper e registro de dependências.
- **2 - Dominio**: `Sistema.CORE` - entidades e implementações dos serviços que definem as regras de negócio.
- **3 - Infraestrutura**: `Sistema.INFRA` - Entity Framework Core, repositórios, Unit of Work, serviços e seeds.

### Organização de pastas
- `2 - Dominio/Sistema.CORE/Entities` contém todas as entidades do domínio.
- `2 - Dominio/Sistema.CORE/Services` possui as implementações dos serviços e suas interfaces em `Services/Interfaces`.
- `3 - Infraestrutura/Sistema.INFRA/Repositories` contém as implementações dos repositórios.
- `2 - Dominio/Sistema.CORE/Repositories/Interfaces` guarda apenas as interfaces dos repositórios.
- `1 - Aplicacao/Sistema.APP/DTOs` concentra os DTOs utilizados pela API.
- `0 - Apresentacao/Sistema.MVC` reúne controllers, views e arquivos estáticos do frontend.
- `3 - Infraestrutura/Sistema.INFRA/Services` disponibiliza serviços como envio de e-mails.

## Funcionalidades
- CRUD de `Perfil`, `Usuario`, `Funcionalidade` e `Tema` com validações de negócio.
- Campos de auditoria nas entidades e registro de operações em `Log`.
- `UnitOfWork` centraliza o commit das alterações.
- Seeds criam perfis, funcionalidades e usuários iniciais (Admin e Comercial).
- Serilog grava logs mensais na pasta `log` e as operações também ficam salvas na tabela `Logs` com usuário e detalhes da exceção.
- Perfis e usuários podem ser ativados ou desativados e as APIs permitem filtrar registros por data, perfil e status.
- Listagens retornam `PagedResult<T>` para suportar paginação via `page` e `pageSize`.
- Perfis possuem funcionalidades com permissões de leitura e escrita gravadas em `PerfilFuncionalidades`.
- Autenticação via JWT protege a API; obtenha um token em `/api/auth/login`.
- Interface MVC permite login, registro, recuperação de senha e personalização de tema.
- `EmailService` envia mensagens SMTP para cadastro e recuperação de senha.

Para compilar a solução:

```bash
dotnet build Sistema.sln -c Release
```

