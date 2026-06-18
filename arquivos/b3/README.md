# arquivos/b3/ — extratos da Área do Investidor B3

Fonte **oficial e consolidada** (Posição + Movimentação/Proventos) baixada da Área Logada do Investidor na B3. Substitui a "Conexão B3" ao vivo (inviável p/ app pessoal — exige convênio institucional). Resolve compras faltantes e traz o **rendimento de FII** que não aparece em nenhum informe de IR.

## Convenção de nomes (importante)
Os arquivos da B3 vêm **1 por mês e sem data interna**. Para o parser saber o período, **nomeie por mês**, ex.:
- `posicao_2025-03.pdf` (ou `.xlsx`)
- `movimentacao_2025-03.pdf`

O parser deriva o ano-mês do nome do arquivo (ou da subpasta). Sem isso, não dá pra datar os lançamentos.

## Privacidade
Contém CPF e dados de custódia → mesma regra do `arquivos/`: repo **privado, sem push**.
