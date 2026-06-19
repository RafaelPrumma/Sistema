# arquivos/b3/ — extratos da Área do Investidor B3

Fonte **oficial e consolidada** (Posição + Movimentação/Proventos) baixada da Área Logada do Investidor na B3. Substitui a "Conexão B3" ao vivo (inviável p/ app pessoal — exige convênio institucional). Resolve compras faltantes e traz o **rendimento de FII** que não aparece em nenhum informe de IR.

## Convenção de nomes (importante)
A B3 exporta **um único workbook `.xlsx` por mês** (relatório consolidado), nomeado com o **mês por extenso em pt-BR**:
- `relatorio-consolidado-mensal-AAAA-<mês>.xlsx` — ex.: `relatorio-consolidado-mensal-2022-setembro.xlsx`

O parser deriva o **ano-mês do nome do arquivo** (`setembro→09`). **Meses não precisam ser contíguos** — o portal da B3 falha ao gerar alguns meses, então a pasta é alimentada aos poucos e lacunas são normais.

## Abas do workbook (o conjunto varia por mês)
Tipicamente 6, mas há meses com `Posição - BDR`/`Posição - ETF` e meses **sem** `Negociações`. O parser lê as que existirem.
- **Negociações** — compras/vendas (agregadas por mês, com `Período` interno) → completa compras faltantes.
- **Proventos Recebidos** — proventos reais, **inclui rendimento de FII** (com data de `Pagamento` interna).
- **Posição - Ações / Fundos / Renda Fixa / Tesouro Direto / BDR / ETF** — *snapshot* do mês (sem data interna → usa o mês do nome) para conferência/aceite e detecção de split.

> Detalhe técnico e regras de importação: `specs/importador-b3.spec.md`.

## Privacidade
Contém CPF e dados de custódia → mesma regra do `arquivos/`: repo **privado, sem push**.
