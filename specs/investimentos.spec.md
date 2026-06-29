# Spec — Submódulo Investimentos

> Acompanhamento da carteira B3 + cripto. **A maior parte já está feita** (jun/2026) — esta spec agora lista o que está pronto (resumido) e foca no que **falta para terminar Investimentos**. Contexto técnico: skill `financas-dominio`.

## Arquitetura (decisões vigentes)

### Modelo financeiro materializado
`FinanceiroTransacao` é a fonte da verdade auditável. `FinanceiroPosicaoAtivo` é um **read model/projeção recalculável**: pode ser apagada e reconstruída a partir das transações canônicas, já com eventos corporativos aplicados pelo carregador central.
- Dashboard (patrimônio, carteiras, posições) lê `FinanceiroPosicaoAtivo` + `FinanceiroCotacaoAtivo` + `FinanceiroCarteiraAtivo`, evitando recalcular todas as transações em cada ilha.
- Recalcular `FinanceiroPosicaoAtivo` após importação/reprocessamento, ressincronização canônica, CRUD manual de transação e CRUD de evento corporativo.
- Campos das entidades financeiras padronizados em **PT-BR** (`Chave`, `Sigla`, `Nome`, `Classe`, `Mercado`, `EhCripto`, `PrecoMedio`, `ResultadoRealizado`…); `Slug` permanece em inglês (identificador técnico). `FinanceiroPosicaoAtivo` não tem `RawJson` (é projeção calculada).
- Hotfix de dados deve preservar `FinanceiroTransacao` como fonte auditável e depois **recalcular** ou ajustar explicitamente a projeção.

### Modelo de cotações e histórico
**Não criar terceira tabela de cotações.** Série temporal central = `FinanceiroPrecoHistoricoAtivo`; `FinanceiroCotacaoAtivo` é só cache/status da última cotação por ativo/provedor.
- `FinanceiroPrecoHistoricoAtivo`: `Interval="30m"` (snapshots do job) + `Interval="1d"` (fechamento permanente); `Date` em UTC; upsert idempotente por `AtivoFinanceiroId + Provedor + Interval + Date`.
- Ao cotar: (1) atualiza `FinanceiroCotacaoAtivo`; (2) upsert do bucket `30m` (novo: O=H=L=C=preço; existente: preserva Open, estende High/Low, atualiza Close/CloseBRL); (3) dashboard nunca chama API para histórico.
- Consolidação `1d` a partir dos `30m` (ou candle oficial). Retenção: `1d` indefinido; apaga `30m` >24h só quando o `1d` já existir.
- Agenda: `financas-cotacoes` 30 min; B3 só em pregão/dias úteis; cripto 24/7 (fechamento 23:59 UTC). Proventos seguem em `FinanceiroRendimento`.

## ✅ Concluído (jun/2026)
Importação (notas/Binance/extratos B3), posição/preço médio, proventos, dashboard em ilhas — **mais**:
- **F-A Eventos corporativos** (split/grupamento) — `eventos-corporativos.spec.md` (F1+F2+F3; 5 splits semeados).
- **F-E Importador B3** — extrato consolidado como **fonte de verdade**; precedência invertida (B3 manda); reconciliação pela Posição + ativo virtual `VARIACAO`; fracionário unificado; alias IRIM11→IRDM11. `importador-b3.spec.md`.
- **Read model `FinanceiroPosicaoAtivo` + padronização PT-BR.**
- **Cotações/histórico** — `FinanceiroPrecoHistoricoAtivo` (buckets 30m/1d + consolidação + retenção + agenda).
- **F-I Carteiras hierárquicas** — `CarteiraPaiId`; topo Bancário e Seguridade / FIIs / Minério e energia / Criptomoedas + subcarteiras; `ClassificadorCarteira` (mapa por ticker + fallback). Reparo: FII gravado como ETF reclassificado pelo nome (corrige IR 20% vs 15%).
- **F-J Carteiras zeradas / Resultado** — custo como piso + status `SemCotacao`; cotação `B3Custodia` (Preço de Fechamento da Posição) para o Resultado não ficar 0 sem token Brapi.
- **F-C Linha de aportes** no gráfico de evolução.
- **F-G Metas + rebalanceamento** — peso atual vs `PesoAlvo` + sugestão de aporte (falta a tela de edição do peso-alvo).
- **F-H Alertas** — entidade `AlertaPreco` + job `financas-alertas` + CRUD + notificação interna (preço com re-arme; provento novo).
- **Dashboard transparência** — F-K card de proventos · F-L painel de saúde/rastreabilidade + composição do valor + "Posições calculadas vs custódia" · F-M card de reconciliação B3 · F-N proventos por fonte · F-O aviso "cripto parcialmente reconciliado".
- **Cripto/IR** (`cripto.spec.md` + `ir.spec.md`) — netting fonte única + §11 idempotência (USDT); F2 valoração BRL das pernas; IR cripto exterior (Lei 14.754) + B&D código RFB + IN 1888 + export 8 abas.
- **F-G (fim)** — tela `/Financas/PesoAlvo`: edição em lote do peso-alvo por ativo-em-carteira (link na ilha de Metas + menu). Fecha o F-G ponta a ponta. *(branch `feat/investimentos-final`)*
- **F-S Saúde das cotações** — ilha dedicada (`DashboardSaudeCotacoes`): por ativo com posição>0, status (atual/vencida/falhou/sem token/fallback custo/B3Custodia) + provedor/símbolo/última atualização/erro + lacunas 1d/30m, agrupado B3 / cripto / B3Custódia. Lógica pura `ClassificadorSaudeCotacao`.
- **F-T Calendário de proventos** — ilha (`DashboardCalendarioProventos`): realizado mês×tipo×fonte (B3 Extrato/Brapi/IR/Binance Earn) + faixa "previsto/anunciado" (PaymentDate futuro já persistido).
- **F-H (fim)** — 3 tipos de alerta novos no `FinancasAlertaService`: cotação vencida/sem fonte (reusa `ClassificadorSaudeCotacao`), ativo posição>0 sem carteira, divergência calculado×custódia (reusa o nº do card F-M); dedup/re-arme por marcador `AlertaConfiabilidade`; config `Alertas:DivergenciaValorLimiar`/`:DivergenciaPctLimiar`.
- **F-Q (1ª fatia)** — "Explique este valor" em **Posições + Patrimônio**: modal mostra qtd, PM, preço usado, fonte do preço/fallback (reusa `ClassificadorSaudeCotacao`), resultado, ajuste de reconciliação (`VARIACAO`) + deep-link p/ transações do ticker. Mecanismo reutilizável (`MontadorExplicacaoValor` puro). **Falta: Carteiras + Proventos.**
- **Fix proventos — dupla contagem B3+Brapi** — a precedência "B3 manda" agora vale também p/ proventos: Brapi suprimida onde `Fonte='B3 Extrato'` cobre o ativo×mês de pagamento; limpeza self-healing dos duplicados históricos (soft-delete). Inflava ~+R$1,6k (2023)/+R$0,9k (2024)/+R$1,2k (2025).

**Mapa de classificação de carteiras** (referência; semeado, editável na tela):
| Topo | Sub | Ativos |
|---|---|---|
| Bancário e Seguridade | Bancos / Seguridade | BBAS3, BBDC4, ITUB4 / CXSE3 |
| FIIs | Papel / Tijolo | AFHI11, CPTS11, DEVA11, FYTO11, KNSC11, RECR11, RZAK11… / HGLG11 |
| Minério e energia | Petróleo / Mineração e Metais / Energia | PETR4 / VALE3, GOLD11 / TAEE4 |
| Criptomoedas | BTC / Altcoins / Memecoins | BTC / WBETH, BNSOL, XRP, BNB / DOGE |

Fallback p/ ativos novos: FII com "RECEBÍVEIS/CRI/SECURITIES"→Papel senão Tijolo; cripto BTC→BTC, memecoins (DOGE/SHIB/PEPE…)→Memecoins, resto→Altcoins; ação/ETF sem mapa→só topo + alerta.

## 🔲 Falta para TERMINAR Investimentos

> Atualizado (jun/2026, branch `feat/investimentos-final`): **F-G(fim), F-S, F-T, F-H(fim) e F-Q(1ª fatia) entregues** (ver ✅ Concluído) + fix da dupla contagem de proventos.

**Prioridade alta**
1. **F-B F2 · Rentabilidade vs benchmark (UI).** Motor pronto (`CalculadoraRentabilidade`, TWR/MWR/TIR). Falta: alimentar a série diária (valor + fluxo) de `CriarEvolucaoPatrimonio`, **buscar CDI/Ibov/IPCA** (BCB SGS / Brapi) e expor no gráfico/UI (excesso vs índice + rentabilidade real descontando IPCA). ⚠️ mexe em market-data.
2. **F-F · Baldes Trade/Rendimentos (cripto).** Separar a posição em balde **Trade** (PM limpo) e **Rendimentos** (earn, custo = mercado na data); total = soma dos baldes. `cripto.spec.md §6`. ⚠️ mexe no netting.
3. **F-R · Reconciliação cripto por snapshot.** Equivalente ao da B3: importar/cadastrar saldo real por moeda/data (Spot/Earn/Funding/staking), comparar com `FinanceiroPosicaoAtivo`, status (bate/falta/sobra/sem cotação/sem ativo), ajuste auditável (não apaga histórico). Remove o aviso "parcialmente reconciliado". ⚠️ depende de o Rafael fornecer o snapshot de saldos.
4. **F-Q (resto) · "Explique este valor" em Carteiras + Proventos.** A 1ª fatia (Posições + Patrimônio) já foi. Reaplicar o mesmo mecanismo (`MontadorExplicacaoValor`/modal) às ilhas de Carteiras e de Proventos.

**Proventos — pendência de dado (não é bug de código)**
- **Nov/2025 (~R$660) faltava** porque o extrato `relatorio-consolidado-mensal-2025-novembro.xlsx` não estava em `arquivos/b3/`. **Arquivo já fornecido** → materializa no próximo start do app (resync). Conferir após reimport.
- A **dupla contagem B3+Brapi** já foi corrigida (precedência + limpeza self-healing); a limpeza roda no `AtualizarProventosAsync`. Validar os totais anuais batendo com os reais (2023 R$6.053 / 2024 R$7.267 / 2025 R$8.381) após o job rodar.

**Pendências menores**
- **Troca de ticker / incorporação** (ex.: TAEE3→TAEE4) — fora do split.
- **Aba Compras_BRL** no export de IR + **exchange nacional como opção** (config) — `ir.spec.md`.
- **Varredura de encoding** nos docs/specs (acentos quebrados).
- **Aceite real** (posição/IR vs informes de `arquivos/ir/`) — só rodando o app.

## 💡 Backlog de evolução (inspiração Status Invest / Investidor10 / Kinvo / TradeMap)
- **Renda passiva projetada:** DY × posição → "quanto recebo por mês/ano" (dados já existem: proventos históricos + posição). **Yield on Cost** por ativo (dividendo anual ÷ PM).
- **Indicadores fundamentalistas** (P/L, P/VP, DY, ROE) + **preço-teto** (Bazin/Graham, "caro ou barato") por ativo — precisa fonte (Brapi/Status Invest).
- **Risco × retorno:** volatilidade, Sharpe, max drawdown, beta vs Ibov (já há série diária).
- **Comparador de ativos** lado a lado.
- **Metas de patrimônio / FIRE:** "quanto falta para viver de renda".
- **Notícias por ativo** (estilo TradeMap), se houver fonte.
- **PWA responsivo** (mobile nativo foi descartado; PWA aproveita o que existe).

## Critério de aceite
Posições e preço médio batem com a carteira real do usuário (`eventos-corporativos.spec.md` §3); rentabilidade comparável a benchmark; vendidos zerados; cada número do dashboard explica sua origem (F-Q).
