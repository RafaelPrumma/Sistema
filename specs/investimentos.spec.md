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

**Mapa de classificação de carteiras** (referência; semeado, editável na tela):
| Topo | Sub | Ativos |
|---|---|---|
| Bancário e Seguridade | Bancos / Seguridade | BBAS3, BBDC4, ITUB4 / CXSE3 |
| FIIs | Papel / Tijolo | AFHI11, CPTS11, DEVA11, FYTO11, KNSC11, RECR11, RZAK11… / HGLG11 |
| Minério e energia | Petróleo / Mineração e Metais / Energia | PETR4 / VALE3, GOLD11 / TAEE4 |
| Criptomoedas | BTC / Altcoins / Memecoins | BTC / WBETH, BNSOL, XRP, BNB / DOGE |

Fallback p/ ativos novos: FII com "RECEBÍVEIS/CRI/SECURITIES"→Papel senão Tijolo; cripto BTC→BTC, memecoins (DOGE/SHIB/PEPE…)→Memecoins, resto→Altcoins; ação/ETF sem mapa→só topo + alerta.

## 🔲 Falta para TERMINAR Investimentos

**Prioridade alta**
1. **F-B F2 · Rentabilidade vs benchmark (UI).** Motor pronto (`CalculadoraRentabilidade`, TWR/MWR/TIR). Falta: alimentar a série diária (valor + fluxo) de `CriarEvolucaoPatrimonio`, **buscar CDI/Ibov/IPCA** (BCB SGS / Brapi) e expor no gráfico/UI (excesso vs índice + rentabilidade real descontando IPCA). ⚠️ mexe em market-data.
2. **F-F · Baldes Trade/Rendimentos (cripto).** Separar a posição em balde **Trade** (PM limpo) e **Rendimentos** (earn, custo = mercado na data); total = soma dos baldes. `cripto.spec.md §6`. ⚠️ mexe no netting.
3. **F-Q · "Explique este valor".** Todo número relevante do dashboard abre sua composição/fonte (qtd, PM, preço usado, fonte do preço — Brapi/Binance/B3Custodia/custo —, fallback, transações, ajustes de reconciliação). Sem recalcular na UI (usa os read models). Aplicar 1º em Patrimônio, Carteiras, Posições, Proventos.
4. **F-R · Reconciliação cripto por snapshot.** Equivalente ao da B3: importar/cadastrar saldo real por moeda/data (Spot/Earn/Funding/staking), comparar com `FinanceiroPosicaoAtivo`, status (bate/falta/sobra/sem cotação/sem ativo), ajuste auditável (não apaga histórico). Remove o aviso "parcialmente reconciliado".

**Prioridade média**
5. **F-S · Saúde das cotações.** Painel dedicado: por ativo com posição>0, status (atual/vencida/falhou/sem token/fallback custo/B3Custodia), última atualização, provedor, símbolo, erro; lacunas do `1d`/`30m`. Separar B3 / cripto / B3Custodia.
6. **F-T · Calendário de proventos.** Realizado (`FinanceiroRendimento` por fonte: B3 Extrato/Brapi/IR/Binance Earn) × previsto/anunciado (Brapi data-com/pagamento), por mês e por tipo (Dividendo/JCP/Rendimento FII/Earn/Airdrop).
7. **Tela de edição do `PesoAlvo`** (CRUD por ativo-em-carteira) — fecha o F-G ponta a ponta.
8. **Tipos de alerta restantes (F-H):** cotação vencida/sem fonte; ativo com posição>0 sem carteira; divergência calculado×custódia (há card, falta virar alerta).

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
