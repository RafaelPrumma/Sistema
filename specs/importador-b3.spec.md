# Spec — Importador do extrato consolidado da B3 (Área do Investidor)

> Documento-contrato (o "o quê" e o "por quê"). Contexto técnico do código: skill `financas-dominio`. Status: **prioridade #1**, aprovado para detalhamento/implementação.

## 1. Problema / Objetivo
As notas de corretagem (PDF) não cobrem tudo: faltam **compras antigas** e, principalmente, o **rendimento de FII** não aparece em informe de IR nenhum (só ações). A fonte oficial e consolidada é o **relatório mensal da Área do Investidor B3**. Objetivo: **importar esse relatório** para completar as movimentações faltantes, trazer os proventos reais (inclusive FII) e ter um **snapshot mensal de posição** para conferência. Substitui de vez a "Conexão B3" ao vivo (inviável p/ app pessoal — exige convênio institucional).

## 2. Formato real do arquivo (verificado nos arquivos de `arquivos/b3/`)
- **1 workbook `.xlsx` por mês**, nomeado `relatorio-consolidado-mensal-AAAA-<mês-por-extenso-pt>.xlsx` (ex.: `relatorio-consolidado-mensal-2022-setembro.xlsx`). O **mês vem por extenso em pt-BR** → o parser mapeia `janeiro..dezembro → 01..12`.
- **Meses não são contíguos.** O portal da B3 falha ao gerar alguns meses; o usuário alimenta a pasta aos poucos. O importador **não pode pressupor sequência** nem quebrar por lacuna.
- O **conjunto de abas varia por mês** (verificado na F1): tipicamente as 6 abaixo, mas alguns meses trazem também `Posição - BDR` e/ou `Posição - ETF`, e há mês **sem a aba `Negociações`**. A **ordem das abas não é fixa** e o `r:id` é não-trivial (ex.: `rId3`→`sheet1.xml`) → o parser **itera as abas que existirem** via `xl/workbook.xml` + rels, **nunca pressupõe as 6**. O ticker já vem **canônico** na coluna `Código de Negociação` / no prefixo de `Produto` → **sem a fragmentação por especificação** das notas (não precisa do `NormalizadorAtivoB3` aqui; só validar units/FII).

### 2.1 Abas e cabeçalhos reais
> As 6 abas centrais. Abas extras de Posição (BDR, ETF) seguem o mesmo padrão das demais de Posição e por ora só têm o bruto capturado (classes fora do escopo de cálculo, ver §3.3).
| Aba | Alimenta | Cabeçalho (colunas) |
|---|---|---|
| **Negociações** | compras/vendas faltantes | `Código de Negociação` · `Período (Inicial)` · `Período (Final)` · `Instituição` · `Quantidade (Compra)` · `Quantidade (Venda)` · `Quantidade (Líquida)` · `Preço Médio (Compra)` · `Preço Médio (Venda)` |
| **Proventos Recebidos** | proventos reais (inclui **FII**) | `Produto` · `Pagamento` · `Tipo de Evento` · `Instituição` · `Quantidade` · `Preço unitário` · `Valor líquido` |
| **Posição - Ações** | snapshot mensal (aceite/split) | `Produto` · `Instituição` · `Conta` · `Código de Negociação` · `CNPJ da Empresa` · `Código ISIN / Distribuição` · `Tipo` · `Escriturador` · `Quantidade` · `Quantidade Disponível` · `Quantidade Indisponível` · `Motivo` · `Preço de Fechamento` · `Valor Atualizado` |
| **Posição - Fundos** | snapshot mensal (FII) | idem Ações, com `CNPJ do Fundo` e `Administrador`; `Tipo` = `Cotas` |
| **Posição - Renda Fixa** | snapshot (classe nova) | `Produto` · `Instituição` · `Emissor` · `Código` · `Indexador` · `Tipo de regime` · `Data de Emissão` · `Vencimento` · `Quantidade` · … · `Preço/Valor Atualizado MTM` · `Preço/Valor Atualizado CURVA` |
| **Posição - Tesouro Direto** | snapshot (classe nova) | `Produto` · `Instituição` · `Código ISIN` · `Indexador` · `Vencimento` · `Quantidade` · … · `Valor Aplicado` · `Valor bruto` · `Valor líquido` · `Valor Atualizado` |

### 2.2 Datação
- **Negociações** carrega `Período (Inicial)/(Final)` e **Proventos** carrega `Pagamento` → têm **data interna** (não dependem do nome).
- As abas de **Posição** são *snapshot* sem data de lançamento → o período é o **mês do nome do arquivo**. (Corrige o README antigo, que dizia "sem data interna" de forma genérica.)

## 3. Armadilhas / decisões de design (a confirmar antes de materializar)
1. **Negociações é AGREGADO mensal, não trade-a-trade**: por ticker/mês vem um par (compra agregada com PM) + (venda agregada com PM). **Risco de double-count com as notas PDF**, que são granulares. → **Regra de precedência (✅ confirmada jun/2026):** as notas (granular, com data/corretagem) mandam onde existem; a B3 só preenche **ticker×mês ausente nas notas**.
2. **Proventos Recebidos = realizado oficial** (sem token, inclui FII). Hoje proventos vêm do Brapi (estimado, precisa token) + earn + IR. → **B3 vira fonte primária (✅ confirmada jun/2026)** de provento para meses cobertos; **Brapi/IR ficam de fallback** para meses não cobertos/futuros. Dedup por ticker+data+tipo.
3. **Renda Fixa / Tesouro Direto = classes novas** (CDB, Tesouro IPCA+) não modeladas hoje (sistema = ações B3 + cripto). → **Fora do escopo inicial**: por ora só capturar o snapshot (ou ignorar). Modelagem dessas classes é fase posterior.
4. **Sinergia com eventos corporativos (split):** a **Posição mensal** revela **salto de quantidade sem negociação** = pista direta de split, e serve de **critério de aceite** por mês. O importador B3 *ajuda* a feature de split (não conflita com ela).
5. **Ticker pronto**: usar `Código de Negociação`; validar apenas units/FII e variantes já conhecidas.

## 4. Materialização e dedup
- Novo **`DocumentKind`** (ex.: `ExtratoConsolidadoB3`) + parser no `FinancasImportador` que lê o workbook, identifica as 6 abas pelo nome e deriva o ano-mês do nome do arquivo.
- **Negociações → `TransacaoFinanceira`** (compra e/ou venda agregada do mês). Chave natural sugerida: `ticker + ano-mês + sentido(compra/venda) + corretora`.
- **Proventos Recebidos → provento** (entidade/fluxo de proventos existente). Chave natural: `ticker + data pagamento + tipo de evento + corretora`.
- Dedup geral mantém o padrão: **Sha256 do documento** + **chave natural** (ver skill `financas-dominio`). Reimport idempotente.

## 5. Fases
- **F1 — Parser + DocumentKind (sem materializar): ✅ feito (jun/2026).** `ExtratoConsolidadoB3Reader` (puro/testável: resolve `sharedStrings`, itera abas via workbook+rels), `DocumentKind = ExtratoConsolidadoB3`, dispatch no importador, persistência bruta em `ConteudosBrutosFinanceiros`, ano-mês derivado do nome no `RawMetadataJson`. 11 testes contra os arquivos reais (`dotnet test` verde). Nada materializado.
- **F2 — Materializar movimento:** Negociações → transações e Proventos → proventos, com dedup e a **regra de precedência** vs notas/Brapi (§3.1, §3.2).
- **F3 — Reconciliação/aceite:** usar as abas de Posição como snapshot para **conferir a posição calculada** mês a mês; relatório de divergência; alimenta a detecção de split.

## 6. Fora de escopo (inicial)
Modelar Renda Fixa e Tesouro Direto como classes calculáveis; Conexão B3 ao vivo; incorporação/troca de ticker (ver `eventos-corporativos.spec.md` §7).

## 7. Fonte de dados / privacidade
Arquivos em `arquivos/b3/` (ver `arquivos/b3/README.md`). Contêm CPF e dados de custódia → repo **privado, sem push** (mesma regra do `arquivos/`).

## 8. Critério de aceite
- Importa **todos os meses presentes** em `arquivos/b3/` sem erro; **lacunas de mês não quebram** a importação.
- Tickers e proventos extraídos batem com o conteúdo das abas; **sem double-count** com as notas PDF.
- Proventos de **FII** passam a aparecer; totais reais de referência: **2025 R$8.381 / 2024 R$7.267 / 2023 R$6.053**.
- Reimport é idempotente (rodar 2× não duplica).

## 9. Verificação
`dotnet build` + `dotnet test` verde (teste do parser com os `.xlsx` reais e do dedup/precedência). Conferir uma posição mensal contra a aba de Posição correspondente.
