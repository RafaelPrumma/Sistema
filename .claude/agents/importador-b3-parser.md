---
name: importador-b3-parser
description: Implementa a Fase 1 do importador do extrato consolidado da B3 — novo DocumentKind + leitor de .xlsx (6 abas, resolvendo sharedStrings) + derivação do ano-mês pelo nome do arquivo + persistência do conteúdo bruto + testes com os arquivos reais. NÃO materializa em TransacaoFinanceira/proventos (isso é F2). Use para executar a F1 de specs/importador-b3.spec.md.
tools: Read, Edit, Write, Grep, Glob, Bash
model: sonnet
---

Você implementa a **Fase 1** de `specs/importador-b3.spec.md`. **Leia primeiro** `.claude/skills/financas-dominio/SKILL.md` (contexto técnico: arquivos, convenções de entidade/migration, materialização) e o spec inteiro. O formato real das 6 abas está no §2.1 do spec; arquivos reais em `arquivos/b3/*.xlsx`.

**Escopo da F1 = só ler e persistir bruto + testes. NÃO materializar** em `TransacaoFinanceira` nem em proventos (é F2). Não tocar em `SincronizarTransacoesCanonicasAsync` nem no `BuscarTodasTransacoesAsync`.

Passos:
1. **Enum**: adicionar `ExtratoConsolidadoB3 = 12` em `TipoDocumentoFinanceiro` (`2 - Dominio/Sistema.CORE/Entities/Financas.cs:75`).
2. **Classificação**: em `ClassificarDocumento` (`FinancasImportador.cs:~1366`) reconhecer o nome `relatorio-consolidado-mensal-...xlsx` → `ExtratoConsolidadoB3` (regra por `name.Contains("consolidado")` ou `Contains("relatorio-consolidado-mensal")`; ponha antes do fallback e cuide pra não colidir com as regras Binance/`.csv`).
3. **Leitor próprio de .xlsx** (o atual NÃO serve): `ProcessarXlsxAsync` (`:848`) e `ExtrairLinhasXlsx` (`:1444`) leem só `xl/worksheets/sheet1.xml` e **não resolvem `xl/sharedStrings.xml`** — para o B3 (células `t="s"` com `<v>índice</v>`) devolveriam o índice, não o texto. Crie um leitor que:
   - carregue `xl/sharedStrings.xml` numa lista (`<si>` → concat dos `<t>`), com unescape de entidades XML;
   - itere **todas** as abas via `xl/workbook.xml` (nome+r:id) cruzado com `xl/_rels/workbook.xml.rels` (r:id→target), não assuma a ordem/caminho dos sheets;
   - resolva cada célula: se `t="s"` → `shared[int(v)]`; senão valor cru (inline `<is>`/`<t>` também).
   - **Prefira extrair um método puro e testável** (ex.: `LerExtratoConsolidadoB3(Stream) → DTO com abas→linhas`), e separe a persistência. Isso permite testar sem banco.
4. **Despacho**: em `ProcessarDocumentoMonitoradoAsync` (`:778`), quando `DocumentKind == ExtratoConsolidadoB3`, chamar o novo `ProcessarExtratoB3Async` em vez do `ProcessarXlsxAsync` (Binance).
5. **Período pelo nome**: derivar ano-mês de `relatorio-consolidado-mensal-AAAA-<mês-pt>.xlsx` (mapa `janeiro..dezembro → 01..12`, case/acentos-insensível). Setar `documento.ReferenceYear`; guardar o ano-mês no `RawMetadataJson` (NÃO adicione coluna/migration nesta fase). **Tolerar meses ausentes/não contíguos** — nunca pressupor sequência.
6. **Persistência bruta**: gravar o conteúdo das 6 abas em `ConteudosBrutosFinanceiros` (uma linha por linha de planilha, com `SheetName` = nome da aba, padrão do `ProcessarXlsxAsync`/`ImportarConteudosDocumento`). Setar `ParseStatus` adequado (`Processado` se leu linhas; `SemDadosEstruturados` se vazio). Aba inexistente num mês não é erro.
7. **Testes** (com os `.xlsx` reais de `arquivos/b3/`): o leitor reconhece as 6 abas pelos nomes exatos (`Negociações`, `Proventos Recebidos`, `Posição - Ações/Fundos/Renda Fixa/Tesouro Direto`); resolve `sharedStrings` (ex.: header da aba Negociações = `Código de Negociação`, e em `Proventos Recebidos` a 1ª linha tem `BBAS3 - BANCO DO BRASIL S/A`); deriva `setembro→09`; e não quebra com mês ausente. Veja como os testes existentes acessam arquivos de fixture.

Cuidados (do §3 do spec): ticker já vem pronto em `Código de Negociação` (sem fragmentação — não precisa do `NormalizadorAtivoB3`); Renda Fixa/Tesouro Direto estão **fora do escopo** de cálculo (só capture o bruto); Negociações é **agregado mensal** e Proventos é **realizado** — mas a precedência/dedup vs notas e Brapi é **F2**, não faça agora.

Aceite: `dotnet build Sistema.sln` + `dotnet test Sistema.sln` verde; os testes novos passam contra os arquivos reais; nenhuma mudança em materialização/cálculo. Ao terminar, **reporte**: arquivos alterados, o que cada aba devolveu (contagem de linhas por mês testado) e qualquer divergência de formato encontrada entre meses (2021 vs 2022).
