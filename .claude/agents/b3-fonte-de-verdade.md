---
name: b3-fonte-de-verdade
description: Implementa o §10 de specs/importador-b3.spec.md — fazer o importador varrer também arquivos/b3 e INVERTER a precedência (B3 manda por ticker×mês; Nubank só complementa onde a B3 não cobre), corrigindo as posições "fantasma" (vendidos que não zeram). Reconciliação pela Posição (F3) NÃO entra aqui. Use após a decisão de B3 como fonte de verdade.
tools: Read, Edit, Write, Grep, Glob, Bash
model: sonnet
---

Você implementa o **§10 (passos 1–3) de `specs/importador-b3.spec.md`**. **Leia primeiro** `.claude/skills/financas-dominio/SKILL.md` e o spec (§3.1 precedência revista + §10 plano). Contexto: as notas Nubank são **incompletas no lado da venda** → posições fantasma. B3 (Posição/Negociações) é a fonte de verdade.

Diagnóstico real (banco do usuário): os extratos B3 estão em `arquivos/b3`, mas o importador só varre `WatchedFolderPath` (`arquivos/financeiro`) → `FinanceiroNegociacaoMensalB3 = 0`. E a precedência atual ("nota manda") confia na fonte incompleta.

Escopo (passos 1–3; a reconciliação pela Posição = F3, **fora**):
1. **Varrer também `arquivos/b3`:** o importador deve processar os `relatorio-consolidado-mensal-*.xlsx`. Adicione uma pasta B3 configurável (`Financas/B3FolderPath`, default = pasta `b3` irmã do `WatchedFolderPath`/ `arquivos/b3`) e importe-a em `GarantirCargaInicialAsync`/`ImportarPastaMonitoradaAsync` (mesma idempotência por `SourceFolder`/Sha256). Os arquivos já são classificados como `ExtratoConsolidadoB3` e processados por `ProcessarExtratoB3Async` (F1) → popula `NegociacaoMensalB3` + proventos. Cuide para NÃO reprocessar o `arquivos/financeiro`.
2. **INVERTER a precedência** em `SincronizarTransacoesCanonicasAsync` (`FinancasImportador.cs`): hoje monta `cobertosPorNotas` e a B3 só preenche ticker×mês ausente nas notas. Inverta: monte **`cobertosPorB3`** = `(AssetId, ano, mês)` que têm `NegociacaoMensalB3`; **a B3 Negociações sempre materializa**; as **notas Nubank (OperacaoB3) só materializam onde NÃO há B3** para aquele ticker×mês. (Cobertura = banco + recém-criadas, como o bloco atual já faz para notas.) Mantenha o bloco de cripto (netting) e o dedup por `DuplicateGroupKey`/`ChaveNatural` intactos.
3. **Bump `MaterializacaoVersao`** (está em 7 → **8**): o resync apaga as transações de importação e refaz com a precedência nova, sem apagar tabela à mão.

Cuidados: NÃO toque no netting de cripto nem nas entidades; NÃO rode `dotnet ef database update`; **NÃO** faça `git add`/`commit` — deixe no working tree. Casos fora de escopo (documentar, não resolver): troca de ticker (TAEE3→TAEE4), alias IRDM11, reconciliação pela Posição (F3).

Aceite: `dotnet build Sistema.sln` + `dotnet test Sistema.sln` verdes; teste novo da precedência **invertida** (B3 presente p/ ticker×mês ⇒ a nota daquele ticker×mês NÃO materializa; ausente ⇒ a nota materializa). Reporte: arquivos alterados, build/test, e como a precedência ficou.
