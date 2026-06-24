# Spec — Submódulo Investimentos

> Acompanhamento da carteira B3 + cripto. Parte já existe; aqui ficam as features que faltam. Contexto técnico: skill `financas-dominio`.

## Estado atual
Importação (notas/Binance), posição/preço médio (`CalcularPosicoes`), proventos (Brapi+earn+IR), dashboard em ilhas, carteiras/grupos, resumo analítico. Falta o abaixo.

### Modelo financeiro materializado (jun/2026)
`FinanceiroTransacao` continua sendo a fonte da verdade auditavel. A tabela `FinanceiroPosicaoAtivo` e um read model/projecao recalculavel: pode ser apagada e reconstruida a partir das transacoes canonicas, ja com eventos corporativos aplicados pelo carregador central.

- Dashboard financeiro: patrimonio, carteiras e posicoes devem ler `FinanceiroPosicaoAtivo` + `FinanceiroCotacaoAtivo` + `FinanceiroCarteiraAtivo`, evitando recalcular todas as transacoes em cada ilha.
- Recalcular `FinanceiroPosicaoAtivo` apos importacao/reprocessamento, ressincronizacao canonica, CRUD manual de transacao e CRUD de evento corporativo.
- `RawJson` permanece como nome padrao nas tabelas que ja guardam payload bruto. `FinanceiroPosicaoAtivo` nao tem `RawJson`, pois nao representa payload externo; e uma projecao calculada.
- Nomes fisicos das tabelas principais permanecem: `FinanceiroAtivo`, `FinanceiroCarteira`, `FinanceiroCarteiraAtivo`, `FinanceiroCotacaoAtivo`, `FinanceiroPosicaoEstimativa`.
- Campos padronizados em PT-BR: `Chave`, `Sigla`, `Nome`, `Classe`, `Mercado`, `Moeda`, `EhCripto`, `Ativo`, `PapelConceitual`, `CarteiraPaiId`, `CarteiraPai`, `EhSistema`, `AtivoFinanceiroId`, `AtivoFinanceiro`, `Simbolo`, `Preco`, `PrecoBRL`, `Variacao`, `VariacaoPercentual`, `HorarioMercado`, `ConsultadoEm`, `ExpiraEm`, `MensagemErro`, `Quantidade`, `PrecoMedio`, `TotalInvestido`, `TotalVendido`, `ResultadoRealizado`, `PosicaoAtualEstimada`, `NivelConfianca`, `UltimaOperacaoEm`.
- `Slug` fica em ingles por ser identificador tecnico estavel para URL/chave interna.
- O card Carteiras depende de `FinanceiroPosicaoAtivo`; hotfixes de dados devem preservar `FinanceiroTransacao` como fonte auditavel e depois recalcular ou ajustar explicitamente a projecao.

## Features

### F-A · Eventos corporativos (split/grupamento) — #2
Ver `eventos-corporativos.spec.md` (já detalhado). Causa de saldo negativo; ajuste de cotas no carregador central. **Pré-requisito** de quase tudo (posição correta).

### F-B · Rentabilidade vs benchmark — #3
Mostrar **TWR** (ponderado pelo tempo / sistema de cotas — neutraliza aportes, compara com benchmark) **e TIR/MWR** (experiência real). Benchmarks: **CDI, Ibovespa, IPCA** (+ rentabilidade real = descontando inflação). Reaproveita a curva diária de `CriarEvolucaoPatrimonio`. Concorrentes: Warren/Meu Dinheiro usam TWR; Investidor10 compara com índices.

**F1 — motor de cálculo: ✅ feito (jun/2026).** `CalculadoraRentabilidade` (puro/testável, `Sistema.APP/Services/CalculadoraRentabilidade.cs`) + DTOs: TWR (sistema de cotas), MWR/TIR (bissecção), anualização, comparação com benchmark (excesso + relativo) e rentabilidade real (desconta IPCA). 7 testes, `dotnet test` verde. **F2 (a fazer):** alimentar a série diária (valor + fluxo de aportes) a partir de `CriarEvolucaoPatrimonio` e **buscar CDI/Ibov/IPCA** (BCB/Brapi) para os benchmarks; expor na UI/gráfico.

### F-C · Linha de aportes no gráfico — #4
Sobrepor **custo acumulado (aportes)** × **patrimônio** no gráfico de evolução. Barato — o custo já é calculado.

### F-D · Unificar fallback de preço — #5
Hoje ativo sem cotação contribui 0 no histórico e custo no card (inconsistente). Unificar: usar custo como fallback consistente e sinalizar status "sem cotação".

### F-E · Import do export oficial da B3 — **prioridade #1**
Ver `importador-b3.spec.md` (detalhado). Substitui a "Conexão B3" (inviável p/ app pessoal). Importa o **relatório consolidado mensal** da Área do Investidor B3 (1 `.xlsx`/mês, 6 abas) → completa compras faltantes (aba **Negociações**) e traz **rendimento de FII** (aba **Proventos Recebidos**, que não está em informe nenhum); abas de **Posição** dão snapshot para aceite e detecção de split. Novo `DocumentKind` + parser.

### F-F · Baldes Trade/Rendimentos — #9
Earn/staking cripto entra na **posição** (não só como renda), em balde separado do "Trade", com custo = valor de mercado na data (sem lucro artificial). Preço médio limpo só dos trades; total = soma dos baldes (bate com a corretora).

### F-G · Metas + rebalanceamento — #8
Peso-alvo por carteira/classe + desvio atual vs alvo + sugestão de aporte para rebalancear. Já existe `PesoAlvo` em `CarteiraAtivoFinanceiro`.

### F-H · Alertas de preço/provento — #7
Job (Hangfire) + notificação interna quando preço cruza limiar ou provento é anunciado/pago. Reaproveita `AlertaConfiabilidade` + mensagens.

### F-I · Carteiras hierárquicas (rework) — jun/2026 (✅ FEITO)
Reorganizar os grupos com **subcarteiras** (hierarquia via `CarteiraPaiId` self-FK nullable + `Ordem` na `FinanceiroCarteira`).

- **Topo (4):** `Bancário e Seguridade` · `FIIs` · `Minério e energia` · `Criptomoedas`.
  - "Minério e energia" **junta** petróleo + minério/metais + energia (petróleo é commodity *e* energia; cabe VALE, ouro e Petrobras).
- **Subcarteiras:** Bancário e Seguridade → **Bancos** / **Seguridade**; FIIs → **Papel** / **Tijolo**; Minério e energia → **Petróleo** / **Mineração e Metais** (VALE + ouro) / **Energia**; Criptos → **BTC** / **Altcoins** / **Memecoins**.

**Mapa de classificação (custódia B3 2026-maio + cripto §10 cripto.spec) — semeado, editável na tela depois:**
| Topo | Sub | Ativos |
|---|---|---|
| Bancário e Seguridade | Bancos | BBAS3, BBDC4, ITUB4 |
| Bancário e Seguridade | Seguridade | CXSE3 |
| FIIs | Papel | AFHI11, AFHI12, CPTS11, DEVA11, FYTO11, KNSC11, RECR11, RECR12, RZAK11 |
| FIIs | Tijolo | HGLG11 |
| Minério e energia | Petróleo | PETR4 |
| Minério e energia | Mineração e Metais | VALE3, GOLD11 (ouro) |
| Minério e energia | Energia | TAEE4 |
| Criptomoedas | BTC | BTC |
| Criptomoedas | Altcoins | WBETH, BNSOL, XRP, BNB |
| Criptomoedas | Memecoins | DOGE |

- **Regra p/ ativos novos** (fallback até cadastro manual): FII com "RECEBÍVEIS/CRI/SECURITIES" no nome → Papel, senão Tijolo; cripto: BTC→BTC, lista de memecoins (DOGE, SHIB, PEPE, FLOKI, BONK, WIF…) → Memecoins, resto → Altcoins; ação/ETF sem mapa → sem subcarteira (só topo) + alerta para classificar.
- **BDR/KEPL3 NÃO entram** — o usuário não detém (vendidos; net ≤ 0 → pisados em 0). Não criar carteira de BDR. Só ativos com **posição > 0** entram nos cards.
- A **auto-sugestão** (`GarantirCarteirasPadraoAsync`) cria topo+sub idempotente e vincula os ativos detidos à subcarteira-folha. UI do dashboard agrupa carteira→subcarteira com agregação de valor para cima (pai = soma dos filhos).

### F-J · Carteiras com valor zerado (bug) — jun/2026
Algumas carteiras apareciam com **valor zerado** no dashboard. ✅ FEITO: (1) custo (PM) como piso consistente quando não há cotação + status `SemCotacao` confiável; (2) a coluna **Resultado** ficava 0 nas carteiras B3 (ação/FII não cota sem token Brapi → mercado = custo → resultado 0) — resolvido alimentando uma cotação `ProvedorCotacao.B3Custodia` com o **Preço de Fechamento** da aba Posição do extrato B3 (custódia oficial), e fazendo a valoração preferir cotação utilizável (`PrecoBRL` > 0) à mais recente.

### F-K · Card de proventos no dashboard — jun/2026 (✅ FEITO)
Ilha lazy-loaded `_DashboardProventos` (resumo do período + top pagadores + gráfico mensal, montado no `financas.js` porque script em parcial via innerHTML não executa). Reusa `RendimentoInvestimento` + a lógica da tela de Proventos.

### F-L · Painel de saúde dos dados & transparência (revisão Codex, jun/2026)
Hoje a ilha "Dados & importação" mostra pasta/documentos/cotações, mas não explica **de onde vem cada valor**. Transformar em painel de **saúde/rastreabilidade**:
- **Última Posição B3 usada** + período do snapshot; **meses faltantes** (extratos não contíguos).
- **Arquivos por fonte** (B3/Nubank/Binance/IR): importados, processados, parciais, falhos, duplicados ignorados, abas/linhas lidas, erros. (Há ~58 em `arquivos/b3`, ~26 em `arquivos/financeiro`, vários em `arquivos/ir`.)
- **Composição do valor**: separar "valor com cotação atual" × "preço de fechamento B3Custodia" × "custo/fallback" (o fallback já existe em `CriarAtivosCotadosDaTabela`, mas a tela mostra só o número final).
- Trocar **"Posições estimadas"** por **"Posições calculadas vs custódia"**: colunas valor, preço médio, **fonte do preço** e **diferença vs B3**.

### F-M · Card de reconciliação B3 (revisão Codex)
Tornar a reconciliação (`ReconciliadorPosicaoB3`) explícita p/ o usuário confiar no número: alvo da custódia, calculado por transações, nº de ajustes, **valor total no VARIACAO**, principais ativos ajustados e link p/ as transações `Fonte="Reconciliação"`.

### F-N · Proventos por fonte (revisão Codex)
No card de proventos, **separar/rotular as fontes**: B3 Extrato, Brapi, IR e Binance Earn (é informação de confiança — FII vem da B3 porque o informe de IR não cobre). Complementa o fix de valores do earn (já feito).

### F-O · Aviso de cripto parcialmente reconciliado (revisão Codex)
Enquanto não houver saldo de abertura/snapshot real da Binance, o dashboard deve sinalizar **"cripto parcialmente reconciliado"** (a `cripto.spec.md` ainda aponta lacunas de saldo/Earn/valoração). Honestidade > número cego.

### Higiene de specs/skills (revisão Codex) — parcial
Encoding quebrado em alguns docs (acentos) e notas de agentes desatualizadas. ✅ Corrigido: agente `importador-b3-materializa` dizia "notas mandam" (a decisão final é **B3 manda**). Pendente: varredura de encoding.

### Ideias novas (pesquisa)
- **A · Yield on Cost / DY da carteira** — dividendo anual ÷ preço médio (renda futura).
- **B · Risco × retorno** — volatilidade, Sharpe, max drawdown (Kinvo Premium tem).
- **C · Preço-teto** (Bazin/Graham) — "caro ou barato" via fundamentos.
- **D · Calendário de proventos** — futuros anunciados (data-com/pagamento).
- **E · Comparador de ativos** lado a lado.

## Critério de aceite
Posições e preço médio batem com a **carteira real do usuário** (tabela no `eventos-corporativos.spec.md` §3); rentabilidade comparável a benchmark; vendidos zerados.
