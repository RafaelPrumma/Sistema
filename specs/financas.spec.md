# Spec guarda-chuva — Módulo Finanças (3 submódulos)

> Visão geral e índice. Cada submódulo tem seu próprio spec; cada feature dos 10 itens da análise inicial + ideias novas vive dentro de um submódulo. Contexto técnico do código: skill `financas-dominio`.

## Por que reorganizar
O módulo Finanças deixa de ser só "carteira" e passa a ter **3 submódulos** que compartilham a mesma base de dados (notas/Binance/extratos já importados em `arquivos/`):

1. **Investimentos** (`investimentos.spec.md`) — acompanhamento da carteira B3 + cripto (o que existe hoje). Posição, preço médio, proventos, eventos corporativos, rentabilidade, alertas, metas.
2. **IR** (`ir.spec.md`) — rascunho da declaração + imposto a pagar, segundo as regras atualizadas, exportável em **Excel ("cola")** para a época de declarar.
3. **Gastos** (`gastos.spec.md`) — gestão de gastos estilo GuiaBolso/Mobills (cartão + conta → categorização → receita vs despesa). Os dados de fatura/extrato **já são importados** (hoje sem uso).

## Mapa dos itens (análise inicial vs concorrentes) → submódulo
| Item | Onde | Status |
|---|---|---|
| #1 Proventos | Investimentos | ✅ feito |
| #2 Eventos corporativos (split) | Investimentos | 🟡 spec `eventos-corporativos.spec.md` |
| #3 Rentabilidade TWR/TIR + benchmark | Investimentos | ⚪ |
| #4 Linha de aportes no gráfico | Investimentos | ⚪ |
| #5 Unificar fallback de preço | Investimentos | ⚪ |
| #6 IR/DARF | **IR** (submódulo próprio) | ⚪ |
| #7 Alertas de preço/provento | Investimentos | ⚪ |
| #8 Metas + rebalanceamento | Investimentos | ⚪ |
| #9 Baldes Trade/Rendimentos (earn) | Investimentos | 🟡 parcial |
| #10 Mobile | — | ⛔ descartado |
| Ideias novas A–E (YoC, risco, preço-teto, calendário, comparador) | Investimentos | ⚪ |
| Import do export oficial da B3 | Investimentos | 🟡 spec `importador-b3.spec.md` — **#1** |
| Gestão de gastos | **Gastos** (submódulo novo) | ⚪ |

## Sobre a "Conexão B3"
API ao vivo (estilo Kinvo) exige convênio institucional com a B3 / Open Finance — **inviável para app pessoal**. Caminho real: importar o **export da Área do Investidor B3** (Posição + Extrato de Movimentação/Proventos), que é a fonte oficial e resolve compras faltantes + rendimento de FII. Detalhe em `investimentos.spec.md`.

## Priorização sugerida

**✅ Feito (jun/2026):** #2 eventos corporativos (F1+F2+F3, 5 splits semeados) · motor IR (F1 cálculo + F2 wiring/tela/Excel) · motor rentabilidade (F1 TWR/MWR) · importador B3 (F1 parser + F2 materialização). Tudo com testes; specs marcadas.

**Em aberto (reordenado jun/2026 após o achado dos "fantasmas"):**
1. **B3 como fonte de verdade** (`importador-b3.spec.md`) — (a) varrer `arquivos/b3` ✅, (b) **inverter a precedência** (B3 manda) + resync ✅, (c) **reconciliação pela Posição** (F3) p/ cravar a quantidade oficial ⏳. **(a)+(b) feitos (jun/2026)** — falta só (c) + validar no app.
2. **#9 cripto — netting** (`cripto.spec.md`) — F1 netting ✅ (permuta abate origem, BRL=caixa, earn=posição); **F2 valoração BRL + F3 ponte IR** ⏳. É o elo que destrava o **IR de cripto** (permuta=alienação).
3. **Rentabilidade F2** (série de `CriarEvolucaoPatrimonio` + CDI/Ibov/IPCA + UI).
4. **Aceite do IR** contra os informes de `arquivos/ir/`.
5. **Troca de ticker** (incorporação, ex.: TAEE3→TAEE4) + **alias IRDM11** (IRIDIUM/IRIM subconta).
6. **Gastos** (pilar novo) · 7. #8 metas/rebal + #7 alertas + #4 linha de aportes · 8. ideias A–E.

**Achados ao validar no app (jun/2026) — corrigidos:** import B3 quebrava em provento de ativo novo (FK AssetId=0); **mercado fracionário** (sufixo F, ITUB4F) duplicava ativos → `NormalizarTicker` + resolução do ativo-base no resync (versão 9). **Ainda abertos:** (a) tickers **só-fracionários** sem base (ITSA3F, ITUB3F) só zeram num reimport B3 (entra na reconciliação F3); (b) **ledger Binance cobre só 2025** → quantidades de cripto abaixo do real (faltam saldos de abertura 31/12/2024; ETH/SOL negativos) → precisa ledger completo ou seed de abertura (vai na `cripto.spec.md`); (c) **CMIG e outros vendidos não zeram** → reconciliação pela Posição (F3, item 1c).

> O import da B3 subiu para #1 em jun/2026 (fonte oficial). Refinado depois: o problema não era só "dado faltante" — era **confiar na fonte errada** (notas Nubank incompletas). B3 vira a fonte de verdade; ver `importador-b3.spec.md` §3.1.

## Fontes de dados
`arquivos/financeiro/` (notas, Binance, extratos, faturas) e `arquivos/ir/` (informes). Reimport lê de `arquivos/financeiro`.
