# Sistema

Projeto de exemplo em .NET 8 utilizando arquitetura em camadas.

## Estrutura
- **0 - Apresentacao**: `Sistema.API` - API ASP.NET Core com Swagger e Serilog.
- **1 - Aplicacao**: `Sistema.APP` - serviços de aplicação e perfis do AutoMapper.
- **2 - Dominio**: `Sistema.CORE` - entidades, serviços e interfaces.
- **3 - Infraestrutura**: `Sistema.INFRA` - Entity Framework Core, repositórios, Unit of Work e seeds.

### Organização de pastas
- `2 - Dominio/Sistema.CORE/Entities` contém todas as entidades do domínio.
- `1 - Aplicacao/Sistema.APP/DTOs` concentra os DTOs utilizados pela API.

## Funcionalidades
- CRUD de `Perfil` e `Usuario` com validações de negócio.
- Campos de auditoria nas entidades e registro de operações em `Log`.
- `UnitOfWork` centraliza o commit das alterações.
- Seeds criam perfis e usuários iniciais (Admin e Comercial).

Para compilar a solução:

```bash
dotnet build Sistema.sln -c Release
```

