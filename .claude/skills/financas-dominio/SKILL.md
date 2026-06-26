---
name: financas-dominio
description: Contexto técnico e aprendizados do módulo Finanças (carteira B3 + cripto) deste projeto .NET 8 em DDD por camadas. Use SEMPRE que mexer em posição, preço médio, importação de notas/Binance, dedup, normalização de ticker, cotações, proventos, eventos corporativos (split), carteiras, dashboard ou reimportação. Traz o mapa dos arquivos-chave, as convenções (entidade EF + mapping + migration + seed + job + CRUD), o cálculo de posição, as armadilhas de dados já descobertas e o procedimento de reimportação limpa.
---

# Módulo Finanças — contexto + aprendizados

Acompanhador de carteira (estilo Google Finance), 1 usuário. Renda variável B3 (notas Nubank/NU Invest em PDF) + cripto (relatórios Binance) + lançamento manual. Camadas: `0 - Apresentacao` (MVC), `1 - Aplicacao` (APP), `2 - Dominio` (CORE), `3 - Infraestrutura` (INFRA). EF Core 8 / SQL Server (`localhost`/`SISTEMA`, Trusted; tabelas `Financeiro*`), UnitOfWork + repositórios, auditoria + soft-delete.

## Fontes de dados (versionadas em `arquivos/`)
- `arquivos/financeiro/` = pasta monitorada (config Financas/WatchedFolderPath). Notas B3 (compra/venda) + Binance (trades/convert/depósitos/earn). **Os PDFs de nota se sobrepõem** (mesma nota em vários arquivos export) — o dedup cuida.
- `arquivos/ir/` = Informes de Rendimentos (proventos por ticker). Só **ações** (escrituradores BB/Bradesco). FII rendimento **não** vem aqui.

## Fonte de verdade e cálculo
- **`TransacaoFinanceira`** (`FinanceiroTransacao`) é a fonte única; staging (`OperacaoB3`, `TransacaoCripto`) é materializado nela. Entidades em `2 - Dominio/Sistema.CORE/Entities/Financas.cs`.
- **Posição/preço médio**: `CalcularPosicoes` em `1 - Aplicacao/Sistema.APP/Services/FinancasAppService.cs` (custo médio ponderado móvel; `DeltaQuantidade` dá o sentido). Resumo/realizado em `ObterResumoAnaliticoAsync`; evolução em `CriarEvolucaoPatrimonio`.
- **Carregador central**: `IFinancasRepository.BuscarTodasTransacoesAsync` (`FinancasRepository.cs`), `AsNoTracking`, usado por TODOS os cálculos → ponto único pra ajustes globais (ex.: split). **Nunca** ajustar `BuscarTransacoesAsync` (grid mostra dado cru da nota).

## Importação e normalização
- `FinancasImportador` (`3 - Infraestrutura/.../Importers/FinancasImportador.cs`): lê a pasta, dedup por **Sha256 do documento** + **chave natural da operação**. `GarantirCargaInicialAsync` auto-importa no 1º acesso ao dashboard. B3 dedup usa **data** (nota não tem hora).
- `NormalizadorAtivoB3`: especificação da nota (`BRASIL ON EJ NM`) → ticker canônico (`BBAS3`) + classe, via mapa de aliases. Evita fragmentar por marcadores ex-dividendo.
- `ParseDecimal` entende **notação científica** (earn Binance vem como `6E-8` = 0,00000006; sem isso virava 6 — erro de 10^8).

## Armadilhas de dados já descobertas (memória da investigação)
1. **Fragmentação B3**: a nota cria um ativo por "especificação" (com marcadores EJ/ED/ATZ) → 1 ticker virava vários ativos, com `Ticker` nulo (sem cotação). Resolvido pelo `NormalizadorAtivoB3` + reimport.
2. **Variantes de nome do mesmo fundo**: ex.: `FII IRIDIUM CI` (compras) e `FII IRIM CI` (vendas) são o mesmo IRDM11 → precisa alias explícito no normalizador.
3. **Eventos corporativos (split) — causa de saldo negativo**: a nota tem compra **pré-split** e venda **pós-split** (×fator). Sem ajustar, o saldo fica negativo (fundo vendido "aparecendo") ou a posição mantida fica errada. **Método confiável p/ achar o fator = salto de quantidade na aba *Posição* do extrato B3** (qtd multiplica de um mês p/ outro **sem compra** na aba *Negociações* → razão = fator exato; cruzar com fato relevante p/ a data-ex). ⚠️ A heurística antiga **PM_banco/PM_real ENGANA** (compras pré+pós diluem a média: CPTS11 deu 2,25 mas é 1:10; KNSC11 1,66 → 1:10; BBAS3 1,42 → 1:2). **Splits confirmados/semeados:** BCFF11 1:8 (28/11/2023), GGRC11 1:10 (06/03/2024), CPTS11 1:10 (26/09/2023), KNSC11 1:10 (06/11/2023), BBAS3 1:2 (16/04/2024). FYTO11/CNES11 = sem split. Implementado (F1+F2): entidade `EventoCorporativo` + ajuste em `BuscarTodasTransacoesAsync` + CRUD `/Financas/Eventos`. → `specs/eventos-corporativos.spec.md`.
4. **Dedup é correto**: as operações "duplicadas" descartadas são overlap da mesma nota em arquivos diferentes (40 notas em vários PDFs). Não inflar/restaurar.
5. **Earn cripto**: vem do staging Binance (Simple Earn/staking) sem preço; valorado em BRL = qtd × preço histórico na data. Tem trava de sanidade (ignora valor absurdo).
6. **Proventos**: B3 via Brapi (cruzando posição na data-com) **depende do token Brapi**; informes do IR só cobrem **ações**. O grosso (rendimento de FII) **não** está em nenhum doc parseado → fonte boa = Brapi por ticker. Valores reais do usuário: **2025 R$8.381 / 2024 R$7.267 / 2023 R$6.053**.
7. **Cotação B3** sem token Brapi não funciona (mesmo com ticker). Cripto cota pela Binance sem token.
8. **Gráficos (ApexCharts)**: sempre `theme:{mode}` e `tooltip:{theme}` por `data-bs-theme`, senão tooltip ilegível no dark.

## Reimportação limpa (procedimento validado)
1. Backup das tabelas `Financeiro*`. 2. Salvar vínculo carteira→ticker. 3. Wipe das tabelas financeiras **mantendo `FinanceiroCarteira`** (definições dos grupos). 4. Config WatchedFolderPath → `arquivos/financeiro`. 5. Abrir o dashboard dispara `GarantirCargaInicialAsync` (ou rodar via hook temporário no startup). A auto-sugestão religa as carteiras. SQL manual: `SET QUOTED_IDENTIFIER ON` (índices filtrados).

## Convenções pra estender
- **Entidade**: classe em `Financas.cs` → `IEntityTypeConfiguration` em `Mapping/FinancasMap.cs` (chave natural = índice único filtrado `[Chave] IS NOT NULL AND [DataExclusao] IS NULL`) → `DbSet` em `Data/AppDbContext.cs` → `dotnet ef migrations add <Nome> -p "3 - Infraestrutura/Sistema.INFRA" -s "0 - Apresentacao/Sistema.MVC" -o Data/Migrations` (EF tool 8.x).
- **Seed**: `Data/Seeds/` + chamada no `DbInitializer`.
- **Job recorrente**: método no `FinancasMarketDataService` + `RecurringJob.AddOrUpdate` em `Program.cs` (ver `financas-cotacoes`/`financas-proventos`). HttpClient Brapi/Binance via `IHttpClientFactory`; token em config Financas/MarketData:BrapiToken.
- **CRUD/UI manual**: padrão de `RegistrarTransacaoManualAsync` + `_NovaTransacaoModal.cshtml`; `FinancasController` (GET página + POST `[ValidateAntiForgeryToken]`); link no `_SidebarMenu.cshtml`; permissão `[AuthorizePermission("Financas")]`.
- **Dashboard** (pós-refatoração Codex): "ilhas" lazy-loaded (`PrepararDashboard`/`DashboardPatrimonio`/`DashboardCarteiras`...).

## Roadmap (origem da análise inicial vs concorrentes)
Feito: proventos (Brapi+earn+IR), normalização/consolidação de ticker B3, seção Carteiras, dedup econômico, fix do earn, gráfico de proventos. **Em aberto**: #2 eventos corporativos (split — em spec), TWR vs benchmark (CDI/Ibov), helper de IR/DARF, alertas de preço, metas/rebalanceamento, importador recorrente de Informe de Rendimentos, baldes Trade/Rendimentos no saldo cripto.

## Fluxo com subagentes (nosso padrão de trabalho)
O Rafael prefere **delegar a implementação a subagentes** (economiza tokens do thread principal; ele revisa depois). Padrão:
- O **thread principal** faz diagnóstico + decisões de design, atualiza specs/skills e **escreve um brief auto-suficiente** (o subagente parte do zero, frio). O brief SEMPRE manda: ler esta skill + o spec do item; seguir as convenções.
- **Todo brief deve incluir estas constraints:**
  - **À PROVA DE FALHA:** `GarantirCargaInicialAsync` e tudo que ele chama (auto-sugestão de carteiras, reconciliação, recálculo da projeção) rodam no load do dashboard — exceção derruba o dashboard (já houve regressões). try-catch que loga e segue; método de ilha devolve DTO vazio/zerado, nunca estoura.
  - **Validar** com `dotnet test "4 - Testes/Sistema.Tests/Sistema.Tests.csproj"` (NÃO a solução: a app costuma estar rodando e trava a DLL da MVC; se travar o build, parar `Sistema.MVC`). Build **0 warnings** — há `.editorconfig` na raiz configurando CA1707/CA1861/CA1859; não alterá-lo.
  - **Commits focados por subtrabalho**, mensagem terminando em `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. `git add` **só dos arquivos alterados** (NUNCA `git add -A` — o working tree pode ter trabalho de outras frentes/Codex). **Sem push** (só com pedido explícito; `arquivos/` tem CPF).
  - **Migration:** `dotnet ef migrations add <Nome> -p "3 - Infraestrutura/Sistema.INFRA" -s "0 - Apresentacao/Sistema.MVC" -o Data/Migrations` e incluir o `AppDbContextModelSnapshot.cs` (fica em `Sistema.INFRA/Migrations/`, não em Data/Migrations) **no MESMO commit**. NÃO rodar `dotnet ef database update` (o app aplica no startup).
- **Coordenação:** só UM subagente por vez em arquivos que se sobrepõem (`FinancasAppService`, views do dashboard) — senão conflitam. Sequencie ou dê escopos disjuntos. `run_in_background` quando quiser offload sem bloquear.

## Aprendizados recentes (jun/2026) — arquitetura atual
- **Read model `FinanceiroPosicaoAtivo`** (`PosicaoAtivo`, campos PT-BR): projeção **recalculável** da posição (não é fonte — reconstrói das transações canônicas com eventos aplicados). Dashboard deve LER dela via `IPosicaoAtivoProjectionService`; **recalcular** após import/resync/CRUD de transação/CRUD de evento. (Migração ainda parcial: `FinancasAppService` ainda usa `CalcularPosicoes` no resumo analítico e na variação do dia.)
- **B3 = fonte de verdade** (precedência **INVERTIDA**: B3 manda por ticker×mês; Nubank só complementa). Reconciliação pela aba **Posição** + ativo virtual `VARIACAO` (à prova de falha) zera fantasmas e deixa a diferença visível.
- **Cotação B3 sem token Brapi:** usa o **Preço de Fechamento** da aba Posição como `ProvedorCotacao.B3Custodia` (`AtualizarCotacoesCustodiaB3Async`); a valoração prefere cotação com `PrecoBRL>0` à mais recente → "Resultado" deixa de ser 0 em ação/FII.
- **Carteiras hierárquicas:** `ClassificadorCarteira` (puro, mapa por ticker + fallback) + `CarteiraPaiId` self-FK; `GarantirCarteirasPadraoAsync` idempotente e à prova de falha, roda **DEPOIS** do resync/reconciliação (posição final). Topo: Bancário e Seguridade · FIIs · Minério e energia · Criptomoedas (com subcarteiras).
- **Ilhas do dashboard:** ação `Dashboard{X}` → `_service.Obter{X}DashboardAsync` → parcial `_Dashboard{X}` + loader em `wwwroot/js/financas.js` + `<section>` em `Views/Financas/Index.cshtml`. **Gráfico monta no JS** (script dentro de parcial injetada via innerHTML NÃO executa); passar dados por `data-*`.
- **Earn cripto como provento:** só conta `Rendimento` real (`CriptoNetting.Classificar`), NÃO o principal de staking (WBETH2.0-Staking/SOL Staking-Purchase). `AtualizarProventosCriptoEarnAsync` é self-healing (purga + regenera). Netting usa **fonte única** do ledger (`.xlsx` `BinanceTransactions`; `.csv`/`CsvBinance` só fallback); `FinancasDataRepairService` reclassifica `DocumentKind` antigo `Desconhecido`.

## Validação
- `dotnet test "4 - Testes/Sistema.Tests/Sistema.Tests.csproj"` (não a solução — a app rodando trava a DLL da MVC). Build **0 warnings**.
- Migration aplica no **startup** (não rodar `database update`).
- Aceite de carteira (posição correta) = lista real do usuário no §3 do `eventos-corporativos.spec.md`.
