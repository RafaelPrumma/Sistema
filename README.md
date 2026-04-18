# Sistema

Projeto de exemplo em .NET 8 utilizando arquitetura em camadas.

## Estrutura
- **0 - Apresentacao**:
  - `Sistema.API` - API ASP.NET Core com Swagger e Serilog.
  - `Sistema.MVC` - frontend web MVC para autenticação, cadastro, recuperação de senha, documentação e edição de tema.
- **1 - Aplicacao**: `Sistema.APP` - perfis do AutoMapper, serviços de aplicação e registro de dependências.
- **2 - Dominio**: `Sistema.CORE` - entidades e regras de negócio.
- **3 - Infraestrutura**: `Sistema.INFRA` - Entity Framework Core, repositórios, Unit of Work, serviços e seeds.
- **4 - Testes**: `Sistema.Tests` - testes unitários.

## Funcionalidades principais
- Hub de comunicação interna com três tipos de publicação:
  - mensagem direta
  - post de setor
  - aviso institucional
- Threads sociais com respostas em cadeia (pai/filho).
- Reações por usuário/publicação com unicidade por item.
- Controle de leitura/entrega em publicações.
- Auditoria de operações por módulo (`Acesso`, `Comunicacao`, `Administracao`) em tabela única de logs.

## Auditoria e retenção de logs
A aplicação utiliza tabela única de `Log` com campo `Modulo` para segmentação lógica.

### Endpoints de consulta
- `GET /api/log`
- `GET /api/log/acesso`
- `GET /api/log/comunicacao`
- `GET /api/log/administracao`

### Correlação de requisições
- A aplicação propaga `X-Correlation-ID` em API e MVC.
- O valor é gravado no log em `CorrelationId` e pode ser usado para rastrear ponta a ponta.

### Retenção por módulo (configurável via sistema)
As regras de retenção são lidas do agrupamento de configuração **`LogsRetencao`**:
- `AcessoMeses` (padrão: 3)
- `ComunicacaoMeses` (padrão: 6)
- `AdministracaoMeses` (padrão: 12)
- `GeralMeses` (padrão: 12)

Você pode editar esses valores na tela de configurações (`/Configuracao`) filtrando pelo agrupamento `LogsRetencao`.

## Consistência transacional de logs
- O `LogAppService` não confirma transação por conta própria.
- O commit permanece no serviço/caso de uso chamador para manter consistência entre operação de negócio e auditoria.

## Fallback de log (resiliência)
Se ocorrer falha ao gravar log no banco, o sistema grava o evento em arquivo interno:
- `log-fallback/audit-fallback.ndjson`

Quando a gravação no banco volta a funcionar, os registros do arquivo fallback são migrados automaticamente para a tabela de logs e removidos do arquivo.

## Seeds
Na inicialização, o sistema cria dados iniciais de:
- perfis
- funcionalidades
- usuários
- configurações (incluindo `LogsRetencao`)

## Build e testes
Para compilar:

```bash
dotnet build Sistema.sln -warnaserror
```

Para executar testes:

```bash
dotnet test Sistema.sln --no-build
```

## Estilos com Sass
Os estilos do projeto foram organizados com [Sass](https://sass-lang.com/).
- O arquivo principal `0 - Apresentacao/Sistema.MVC/wwwroot/css/site.scss` importa as parciais localizadas em `0 - Apresentacao/Sistema.MVC/wwwroot/css/sass/`.
- Crie novos parciais com prefixo `_` (por exemplo, `_buttons.scss`) nessa pasta e adicione-os em `site.scss`.
- Para compilar o SCSS em CSS execute:

```bash
npm install
npm run build-css
```

## Scripts com jQuery
- `0 - Apresentacao/Sistema.MVC/wwwroot/js/site.js` utiliza jQuery para controlar menus, tema e outras interações.
- A tag `<html>` do layout está configurada com `lang="pt-BR"`.
