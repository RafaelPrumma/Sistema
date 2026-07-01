# Spec — Cripto: modelo de transações (netting) + valoração + ponte para o IR (#9)

> Documento-contrato. Contexto técnico: skill `financas-dominio`. Liga `investimentos.spec.md` (F-F baldes) e `ir.spec.md` (cripto). Status: aprovado para detalhamento/implementação.

## 1. Problema
A posição de cripto vem **errada** (provado no banco, jun/2026): dezenas de ativos com **N compras e 0 vendas** (USDT, SOL, ETH, SIGN…), stablecoins e até **BRL** (caixa!) aparecendo como posição, e alguns saldos **negativos** (LTC, HMSTR). Causa: o staging Binance **não faz o netting das trocas** — registra o ativo comprado, mas **não abate** o ativo gasto. E o **IR de cripto** precisa de cada **permuta como alienação valorada em BRL** (ver `ir.spec.md`), que hoje não existe.

## 2. Objetivo
Modelar as operações da Binance de forma que (a) a **posição** bata com a corretora (fantasmas somem) e (b) cada **alienação** (venda **e permuta**) e cada **rendimento** fiquem prontos para a apuração de IR. Fonte de verdade da estrutura: o consolidado real do usuário (`IRPF_2026_cripto_Binance_..._consolidado.xlsx`, não versionado).

## 3. Fontes (CSV da Binance, já em `arquivos/financeiro/`)
- **Histórico de Transações** (ledger; cobre tudo): Deposit BRL, Buy Crypto With Fiat, Convert, Small Assets Exchange BNB, Staking (SOL Staking-Purchase, WBETH2.0-Staking), Earn (Simple Earn Flexible Interest, Airdrop Distribution, Crypto Box), Fees…
- **Trades Spot** (compras/vendas com par/preço) · **Convert orders** · **Depósitos**.

## 4. Modelo correto de transação (o que falta)
Cada operação afeta a **posição** (quantidade + custo) e, quando há saída, gera **alienação** p/ o IR:
- **Compra com fiat (BRL):** posição ↑; custo = BRL pago. (Buy Crypto With Fiat, Trade Spot BUY com par BRL.)
- **Venda por fiat (BRL):** posição ↓; **alienação** = BRL recebido; ganho = BRL − custo médio.
- **Permuta cripto→cripto (CRÍTICO — Convert / Small Assets Exchange / staking ETH→WBETH, SOL→BNSOL):** tem **DUAS pernas** —
  - perna de **saída** (ativo origem): posição ↓; **alienação** valorada ao **preço de mercado em BRL na data**; ganho = valor mercado − custo médio do origem;
  - perna de **entrada** (ativo destino): posição ↑; custo = o **mesmo** valor BRL da saída.
  - ⚠️ Hoje só a entrada é registrada → a origem nunca é abatida (causa-raiz dos fantasmas/negativos).
- **Rendimento (earn/staking reward/airdrop/interest):** posição ↑ **sem custo de compra**; é **rendimento tributável** valorado em BRL na data; o **custo de aquisição** do recebido = esse valor BRL (p/ alienações futuras).
- **BRL = caixa/fiat, NÃO é cripto** → não deve virar ativo de posição (bug atual: "Cripto · BRLUSDT"). Marcar BRL (e fiat) como não-ativo.
- **Stablecoins (USDT/USDC/FDUSD):** são cripto, mas funcionam como par de troca → o netting das permutas (acima) já as abate corretamente; não excluir, só netar.
- **Saldos negativos** = sintoma de saída sem entrada modelada → o netting completo (ambas as pernas) elimina.

## 5. Valoração em BRL na data
Permutas cripto-cripto precisam do **preço do ativo em BRL na data** — usar `PrecoHistoricoAtivoFinanceiro` (Binance, já importado) ou cotação; fallback ao mais próximo. Quando não houver preço, marcar a linha como **estimada/“a preencher”** (como o consolidado faz) e alertar, em vez de chutar.

## 6. Baldes (item #9 original)
Posição = **balde Trade** (compras/vendas, PM limpo) + **balde Rendimentos** (earn/staking, custo = valor de mercado na data; sem lucro artificial). Total = soma dos baldes (bate com a corretora). PM “de trade” não é poluído pelo earn.

## 7. Ponte para o IR (alimenta `ir.spec.md`)
- **Operacoes_Ganho:** uma linha por alienação (venda + **permuta**): data, mês, tipo/fonte, ativo, qtd, valor de alienação BRL, custo, ganho/perda.
- **Rendimentos_Rewards:** earn/staking/airdrop/interest valorados em BRL.
- **Bens e Direitos:** posição 31/12 ao custo, com **código RFB** (08-01/02/03/99) e Discriminação; saldo em 31/12 do ano e do anterior.
- **Aplic_Fin_Exterior:** Binance = exterior (Lei 14.754/2023).
- **IN 1888:** sinalizar mês com operações > R$ 30.000.

## 8. Fases
- **F1 — Netting (corrige a posição): ✅ feito (jun/2026).** `CriptoNetting` (puro/testável) classifica o ledger da Binance e neta as pernas assinadas (permuta abate a origem; BRL/fiat fora; earn = Rendimento custo 0; transferência interna ignorada). `MaterializarCriptoNettingAsync` usa **só o ledger** (fonte completa; spot/convert não materializam → evita dobrar), preço da perna = PM corrente (realizado ≈ 0 nesta fase). `MaterializacaoVersao` 6→7 (resync no próximo start). 18 testes, suíte 94/94. ⚠️ Pressupõe que o **ledger** ("Histórico de Transações") esteja presente — é a fonte única. Aceite real (posição bate, sem fantasma/negativo) só valida rodando o app.
- **F2 — Valoração BRL das pernas: ✅ feito (jun/2026).** `CriptoValorador` (puro) + `MaterializarCriptoNettingAsync` valoram cada perna ao **preço de mercado BRL na data** (histórico Binance `1d` → fallback candle anterior → cotação atual; alerta `PRECO_CRIPTO_FALTANTE` quando não há preço): venda/permuta-saída = valor de alienação; earn = custo de mercado na data (não 0); compra fiat = BRL pago. `MaterializacaoVersao`→13. O **BTC subcontado** (Earn) foi resolvido **antes** disto, pela fonte única do ledger `.xlsx` (DocumentKind reclassificado) + **§11** (dedup por staging) + hotfix de dados — não foi preciso o mecanismo VARIAÇÃO. **🔲 Falta:** **baldes Trade/Rendimentos (§6)** — separar o PM de trade do earn.
- **F3 — Ponte IR cripto: ✅ feito (jun/2026).** `CalculadoraIr.ApurarCriptoExterior` trata cripto como **exterior** (Lei 14.754, 15% anual, sem isenção R$35k); `CriptoExteriorIrDto` com Operacoes_Ganho (alienações valoradas), Rendimentos_Rewards, B&D com **código RFB** (08-01/02/03/99) + situação 31/12 anterior, e flag **IN 1888** (mês > R$30k); o export Excel espelha 8 abas. **🔲 Falta:** aba Compras_BRL + exchange nacional como opção (ver `ir.spec.md`).

## 9. Fora de escopo (inicial)
Reconciliação automática com saldo ao vivo da Binance via API (sem chave); preço intradiário exato (usar fechamento diário).

## 10. Critério de aceite
- Posição de cripto bate com a corretora: **só ativos realmente detidos** (sem stablecoin/BRL/token fantasma; **sem saldo negativo**). Referência do usuário (31/12/2025, do consolidado): BTC, WBETH, BNSOL, XRP, DOGE, BNB (ETH/SOL/USDT zerados).
- Cada permuta vira uma alienação valorada; earn vira rendimento. `dotnet build` + `dotnet test` verdes (testes do netting de permuta, do earn e do BRL-como-caixa, com dados sintéticos).

## 11. Ajuste de idempotencia do ledger Binance (USDT, jun/2026)

Achado em producao local: o arquivo **Historico de Transacoes** da Binance pode ter linhas legitimas iguais no mesmo timestamp, mesma moeda, mesma operacao e mesma quantidade. No caso USDT, o ledger bruto fecha em zero, mas a materializacao antiga descartou duplicatas legitimas por `ChaveNatural` e deixou saldo fantasma em `FinanceiroPosicaoAtivo`.

- Para cripto, nao deduplicar por chave natural baseada apenas em ativo/data/operacao/quantidade/preco.
- A chave segura do ledger cripto e a linha de staging: `DuplicateGroupKey = TransacaoCripto#{StagingId}` e, quando precisar preencher `ChaveNatural`, usar uma chave que inclua o staging, como `BinanceLedger|TransacaoCripto|{StagingId}`.
- `USDT`, `USDC` e `FDUSD` continuam sendo cripto, nao fiat; elas so devem desaparecer da carteira quando a posicao liquida for zero.
- `WBETH2.0 - Staking` e `SOL Staking - Purchase` sao conversoes/permutas de staking: abatem ETH/SOL e aumentam WBETH/BNSOL. Nao tratar como rendimento.
- `Simple Earn Flexible Subscription` e `Simple Earn Flexible Redemption` sao transferencia interna entre carteiras/produtos Earn; nao mudam a posicao detida.
- Rendimento real de cripto fica restrito a eventos como `Interest`, `Rewards`, `Airdrop`, `Crypto Box`, `Launchpool`, `Voucher`, `Distribution` e equivalentes positivos.
