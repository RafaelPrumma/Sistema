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
1. **B3 como fonte de verdade** (`importador-b3.spec.md`) — (a) fazer a B3 **entrar de fato** (pasta: o importador precisa varrer `arquivos/b3`), (b) **inverter a precedência** (B3 manda, Nubank complementa) + resync, (c) **reconciliação pela Posição** (F3) p/ cravar a quantidade oficial. **Conserta as posições erradas (vendidos fantasmas).**
2. **#9 cripto — netting** (baldes Trade/Rendimentos) — Binance não abate o lado vendido; stablecoins/BRL/tokens viram posição fantasma. **Conserta os fantasmas de cripto.**
3. **Rentabilidade F2** (série de `CriarEvolucaoPatrimonio` + CDI/Ibov/IPCA + UI).
4. **Aceite do IR** contra os informes de `arquivos/ir/`.
5. **Troca de ticker** (incorporação, ex.: TAEE3→TAEE4) + **alias IRDM11** (IRIDIUM/IRIM subconta).
6. **Gastos** (pilar novo) · 7. #8 metas/rebal + #7 alertas + #4 linha de aportes · 8. ideias A–E.

> O import da B3 subiu para #1 em jun/2026 (fonte oficial). Refinado depois: o problema não era só "dado faltante" — era **confiar na fonte errada** (notas Nubank incompletas). B3 vira a fonte de verdade; ver `importador-b3.spec.md` §3.1.

## Fontes de dados
`arquivos/financeiro/` (notas, Binance, extratos, faturas) e `arquivos/ir/` (informes). Reimport lê de `arquivos/financeiro`.
