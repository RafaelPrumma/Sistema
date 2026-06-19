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
1. **import B3** (`importador-b3.spec.md`) — destrava compras faltantes + proventos de FII; snapshot mensal p/ aceite e detecção de split · 2. #2 eventos (split) · 3. #6 IR cripto 2026 (urgente, regra nova) · 4. #3 rentabilidade+benchmark · 5. #9 baldes + #4 aportes · 6. Gastos (pilar novo) · 7. #8 metas/rebal + #7 alertas · 8. ideias A–E.

> Reordenado em jun/2026: o import da B3 subiu para #1 (fonte oficial chegando aos poucos; resolve dado faltante que destrava o resto). Os demais desceram uma posição.

## Fontes de dados
`arquivos/financeiro/` (notas, Binance, extratos, faturas) e `arquivos/ir/` (informes). Reimport lê de `arquivos/financeiro`.
