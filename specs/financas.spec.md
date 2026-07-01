# Spec guarda-chuva — Módulo Finanças (3 submódulos)

> Visão geral e índice. Cada submódulo tem seu próprio spec; cada feature dos 10 itens da análise inicial + ideias novas vive dentro de um submódulo. Contexto técnico do código: skill `financas-dominio`.

## Por que reorganizar
O módulo Finanças deixa de ser só "carteira" e passa a ter **3 submódulos** que compartilham a mesma base de dados (notas/Binance/extratos já importados em `arquivos/`):

1. **Investimentos** (`investimentos.spec.md`) — acompanhamento da carteira B3 + cripto (o que existe hoje). Posição, preço médio, proventos, eventos corporativos, rentabilidade, alertas, metas.
2. **IR** (`ir.spec.md`) — rascunho da declaração + imposto a pagar, segundo as regras atualizadas, exportável em **Excel ("cola")** para a época de declarar.
3. **Gastos** (`gastos.spec.md`) — gestão de gastos estilo GuiaBolso/Mobills (cartão + conta → categorização → receita vs despesa). Os dados de fatura/extrato **já são importados** (hoje sem uso).

## Mapa dos itens (análise inicial vs concorrentes) → submódulo
| Item | Onde | Status (jun/2026) |
|---|---|---|
| #1 Proventos | Investimentos | ✅ feito (+ por fonte F-N) |
| #2 Eventos corporativos (split) | Investimentos | ✅ feito (F1+F2+F3) |
| #3 Rentabilidade TWR/TIR + benchmark | Investimentos | 🟡 motor F1 ✅; **F2 UI + CDI/Ibov/IPCA falta** |
| #4 Linha de aportes no gráfico | Investimentos | ✅ feito |
| #5 Unificar fallback de preço | Investimentos | ✅ feito (B3Custodia + status SemCotacao) |
| Histórico de cotações intradiário/diário | Investimentos | ✅ feito (buckets 30m/1d) |
| #6 IR/DARF | **IR** | ✅ feito (cripto exterior, B&D RFB, IN1888, export 8 abas); falta **aceite vs informes** |
| #7 Alertas de preço/provento | Investimentos | ✅ feito (preço+provento); faltam alguns tipos |
| #8 Metas + rebalanceamento | Investimentos | ✅ feito; falta **tela de edição do PesoAlvo** |
| #9 Baldes Trade/Rendimentos (earn) | Investimentos/Cripto | 🔲 **falta** (§6) |
| #10 Mobile | — | ⛔ descartado (avaliar **PWA**) |
| Transparência (explique-valor, reconciliação, saúde dados/cotações) | Investimentos | 🟡 F-L/M/N/O ✅; **F-Q/F-R/F-S/F-T faltam** |
| Ideias A–E (YoC/DY, risco, preço-teto, calendário, comparador) | Investimentos | 🔲 backlog de evolução |
| Import do export oficial da B3 | Investimentos | ✅ feito |
| Gestão de gastos | **Gastos** | ⏸️ adiado (spec detalhada para depois) |

## Sobre a "Conexão B3"
API ao vivo (estilo Kinvo) exige convênio institucional com a B3 / Open Finance — **inviável para app pessoal**. Caminho real: importar o **export da Área do Investidor B3** (Posição + Extrato de Movimentação/Proventos), que é a fonte oficial e resolve compras faltantes + rendimento de FII. Detalhe em `investimentos.spec.md`.

## Priorização

**✅ Feito (jun/2026 — grosso do módulo):** B3 como fonte de verdade (importador + precedência invertida + reconciliação/`VARIAÇÃO`) · eventos corporativos (split) · read model `FinanceiroPosicaoAtivo` + PT-BR · cotações/histórico (buckets 30m/1d) · carteiras hierárquicas (+ reparo FII×ETF) · Resultado via `B3Custodia` · linha de aportes · metas/rebalanceamento · alertas (preço+provento) · dashboard transparência (proventos por fonte, reconciliação, saúde/rastreabilidade, posições vs custódia) · **cripto** (netting fonte única + §11 idempotência + F2 valoração BRL) · **IR** (cripto exterior Lei 14.754, B&D código RFB, IN 1888, export 8 abas). Tudo com testes; branch `feat/cripto-e-b3-base`.

**🔲 Falta para TERMINAR Investimentos** (ordem sugerida — detalhe em `investimentos.spec.md`):
1. **F-B F2** — rentabilidade vs benchmark (série + CDI/Ibov/IPCA + UI).
2. **F-F** — baldes Trade/Rendimentos cripto (`cripto.spec.md §6`).
3. **F-Q** — "Explique este valor" (composição/fonte de cada número).
4. **F-R** — reconciliação cripto por snapshot (saldo real por moeda/data).
5. **F-S** saúde das cotações · **F-T** calendário de proventos.
6. Tela de edição do **PesoAlvo** · tipos de alerta restantes · troca de ticker (TAEE3→TAEE4).
7. **IR:** aceite vs informes de `arquivos/ir/` · aba Compras_BRL · exchange nacional como opção.
8. Varredura de **encoding** nas specs.

**⏸️ Depois de Investimentos:** **Gastos** (pilar novo, spec detalhada em `gastos.spec.md`) · ideias A–E / backlog de evolução (renda passiva, fundamentals/preço-teto, risco/Sharpe, comparador, FIRE, PWA).

**Operacional:** validar no app rodando (restart aplica reparos: FII→FII, DocumentKind, resync) · **PR** (precisa `gh auth login` + decisão de privacidade do `arquivos/`).

> O import da B3 é a fonte de verdade: o problema não era só "dado faltante" — era **confiar na fonte errada** (notas Nubank incompletas). Ver `importador-b3.spec.md` §3.1.

## Fontes de dados
`arquivos/financeiro/` (notas, Binance, extratos, faturas) e `arquivos/ir/` (informes). Reimport lê de `arquivos/financeiro`.
