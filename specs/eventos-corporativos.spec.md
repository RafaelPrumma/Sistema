# Spec — Eventos Corporativos (desdobramento / grupamento)

> Documento-contrato (o "o quê" e o "por quê"). Os subagents implementam contra ele; a skill `financas-dominio` dá o contexto técnico. Status: aprovado para implementação.

## 1. Problema
Após a reimportação limpa, posições de B3 ficam erradas: fundos/ações **vendidos aparecem com saldo negativo** e **fundos mantidos aparecem com quantidade/preço-médio errados**. A causa é **desdobramento de cotas** (split) não modelado: a nota registra a compra em cotas **pré-split** e a venda em cotas **pós-split** (multiplicadas pelo fator). Sem aplicar o fator, o estoque e o preço médio quebram.

## 2. Objetivo
Modelar eventos corporativos e **normalizar as transações para cotas atuais**, de forma que posição, preço médio, resultado realizado, gráfico de patrimônio e proventos fiquem corretos — batendo com a carteira real do usuário.

## 3. Critério de aceite (carteira real — fonte da verdade)
Após a correção, as posições calculadas devem bater com a lista abaixo (tolerância pequena: o banco tem dados até **maio/2026** e houve aportes em **junho**; o usuário **não vendeu nada** recentemente). Ações/FIIs fora desta lista devem ficar **zerados** (não aparecer).

| Ticker | Qtd real | PM real | PM no banco hoje | Fator de split provável |
|---|---|---|---|---|
| ITUB4 | 280 | 33,39 | 34,67 | — |
| BBDC4 | 495 | 15,58 | 15,69 | — |
| VALE3 | 105 | 63,27 | 66,61 | — |
| BBAS3 | 430 | 18,42 | 26,23 | investigar (PM ~1,4×) |
| PETR4 | 137 | 32,91 | 30,74 | — |
| TAEE4 | 390 | 11,98 | 11,91 | — |
| CXSE3 | 250 | 16,26 | 15,94 | — |
| CPTS11 | 860 | 8,29 | 18,62 | **~1:2** (PM 2,25×) |
| AFHI11 | 55 | 96,00 | 96,00 | — (já bate) |
| RZAK11 | 64 | 88,65 | 88,02 | — (já bate) |
| FYTO11 | 620 | 7,60 | 8,24 | investigar |
| HGLG11 | 26 | 158,04 | 157,78 | — (já bate) |
| RECR11 | 48 | 86,74 | 86,74 | — (já bate) |
| KNSC11 | 420 | 8,95 | 14,83 | **split** (PM 1,66×) |
| DEVA11 | 215 | 42,73 | 42,73 | — (já bate) |

**Vendidos (devem zerar):** BCFF11 (split 1:8), GGRC11 (split 1:10), CNES11, BRCR11, VGHF11, RZAT11, e ações zeradas hoje negativas (KLBN3, ITSA3/4, CMIG3, KEPL3, TIMS3, AMER3, BBDC3, GOGL34, ROXO34…).

**Cripto (fora desta fase de split, mas é o alvo geral):** BTC 0,0697; XRP 822,09; DOGE 3137,17; BNB 0,2695; + staking WBETH 1,4844 / BNSOL 26,6339.

> O **ratio PM_banco / PM_real** é uma pista forte do fator (ex.: CPTS11 18,62/8,29≈2,25 → 1:2). Confirmar cada fator/data na B3 ou no informe do fundo.

## 4. Modelo de dados
`EventoCorporativo` (tabela `FinanceiroEventoCorporativo`): `Id`, `AtivoFinanceiroId`, `Tipo` (enum `Desdobramento`/`Grupamento`/`Bonificacao`), `Data` (data-ex), `Fator` (decimal — 8 = 1:8; 0,1 = grupamento 1:10), `Fonte`, `ChaveNatural` (idempotência) + auditoria. Índice único filtrado em `ChaveNatural`.

## 5. Regra de ajuste
Ao carregar transações para cálculo, para cada transação com `Date < Evento.Data` do mesmo ativo: `Quantity *= fator` e `UnitPrice /= fator` (mantém `GrossAmount`). Eventos múltiplos = produto dos fatores. **Só no caminho de cálculo** (`BuscarTodasTransacoesAsync`), nunca na lista/grid de transações.

## 6. Fases
- **F1 — Núcleo:** entidade + mapping + migration + ajuste no carregador + seed dos eventos conhecidos + testes. → zera os negativos e corrige os mantidos.
- **F2 — Cadastro manual:** CRUD de eventos na UI (padrão do lançamento manual de transação).
- **F3 — Auto-busca Brapi:** job recorrente que tenta buscar splits e faz upsert idempotente; alerta para cadastro manual quando a fonte não tiver o dado.

## 7. Fora de escopo
Incorporação/troca de ticker (ex.: BCFF11→BTHF11) — merge de ativos, fase posterior.

## 7b. Fonte de dados
Notas/Binance versionados em `arquivos/financeiro/`; informes do IR em `arquivos/ir/`. A reimportação lê de `arquivos/financeiro` (config Financas/WatchedFolderPath). Detalhes e armadilhas na skill `financas-dominio`.

## 8. Verificação
`dotnet build` + `dotnet test` verde (teste do ajuste: compra pré-split + venda pós-split → saldo e PM corretos). SQL: nenhum `AssetClass=2` com saldo < 0. Posições conferem com a tabela do §3. Grid de transações mostra quantidade crua.
