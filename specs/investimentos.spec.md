# Spec — Submódulo Investimentos

> Acompanhamento da carteira B3 + cripto. Parte já existe; aqui ficam as features que faltam. Contexto técnico: skill `financas-dominio`.

## Estado atual
Importação (notas/Binance), posição/preço médio (`CalcularPosicoes`), proventos (Brapi+earn+IR), dashboard em ilhas, carteiras/grupos, resumo analítico. Falta o abaixo.

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

### Ideias novas (pesquisa)
- **A · Yield on Cost / DY da carteira** — dividendo anual ÷ preço médio (renda futura).
- **B · Risco × retorno** — volatilidade, Sharpe, max drawdown (Kinvo Premium tem).
- **C · Preço-teto** (Bazin/Graham) — "caro ou barato" via fundamentos.
- **D · Calendário de proventos** — futuros anunciados (data-com/pagamento).
- **E · Comparador de ativos** lado a lado.

## Critério de aceite
Posições e preço médio batem com a **carteira real do usuário** (tabela no `eventos-corporativos.spec.md` §3); rentabilidade comparável a benchmark; vendidos zerados.
