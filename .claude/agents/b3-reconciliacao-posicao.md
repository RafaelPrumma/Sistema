---
name: b3-reconciliacao-posicao
description: Implementa a F3 do importador B3 — reconciliação da posição pela aba Posição (custódia oficial) + ativo virtual VARIAÇÃO que absorve a diferença não explicada pelos relatórios. Zera fantasmas (vendidos sem a venda nos relatórios) e mantém a discrepância visível. DEVE ser à prova de falha (não pode derrubar o dashboard). Use após a precedência B3 estar pronta.
tools: Read, Edit, Write, Grep, Glob, Bash
model: sonnet
---

Você implementa a **F3 (reconciliação)** descrita em `specs/importador-b3.spec.md` §10 passo 4. **Leia primeiro** esse §10.4 inteiro + `.claude/skills/financas-dominio/SKILL.md` (materialização, ConteudoBruto, ObterOuCriarAtivo, GerarChaveNatural).

**Contexto:** posições de ações/FII têm "fantasmas" — ativos vendidos que não zeram porque as vendas faltam nos relatórios (ex.: NCHB11 +30, EQIN11 +6, CMIG3 +10, IRDM11 +7, todos sem venda registrada). A **aba Posição da B3 é a custódia oficial** = a verdade. O que não está nela = 0.

Implementar:
1. **Ativo virtual `VARIACAO`** (`ObterOuCriarAtivo` ou seed): AssetKey/Ticker `VARIACAO`, Name "Ajuste de Reconciliação (variação de custódia)", `ClasseAtivo.Outro`, IsCrypto=false.
2. **Posição-alvo:** por ativo B3 (não-cripto), a quantidade na **Posição mais recente**. Origem: `ConteudosBrutosFinanceiros` com `SheetName` em (`Posição - Ações`,`Posição - Fundos`); o período do documento está no `DocumentoFinanceiro.RawMetadataJson` (`referencePeriod` yyyy-MM) — pegue o documento de período MAIS RECENTE e leia as linhas (RawJson tem `row`/`cells`; colunas `Código de Negociação` e `Quantidade`). Normalize o ticker com `ExtratoB3Materializador.NormalizarTicker` (fracionário + alias). Ativo B3 sem aparecer na Posição mais recente → alvo **0**.
3. **Cálculo do ajuste:** `calculado` = soma das quantidades canônicas por ativo (transações `IsCanonical`, EXCETO as de `Fonte="Reconciliação"`); use o mesmo sentido de `DeltaQuantidade` (Compra/Deposito/Rendimento +, Venda/Saque/Taxa −). `diff = alvo − calculado`. Se |diff| > 1e-6: cria transação de ajuste `Fonte="Reconciliação"`, `Origem=Manual` (sobrevive ao resync, que só apaga `Importacao`), no ativo, `OperationType` = Compra se diff>0 senão Venda, `Quantity=|diff|`, `UnitPrice` = PM corrente do ativo (realizado ≈ 0), `Observacao` explicando; **e** uma contrapartida no ativo `VARIACAO` registrando o valor (|diff|×PM) com `UnitPrice=1` (pra aparecer como R$ na carteira). Total de patrimônio preservado, diferença visível.
4. **Idempotência:** no início, **apague** (hard ou soft, como o resto faz) as transações `Fonte="Reconciliação"` e recalcule. NUNCA toque em transações de importação/manuais reais.
5. **Gatilho + À PROVA DE FALHA (OBRIGATÓRIO):** chame a reconciliação em `GarantirCargaInicialAsync` **depois** da materialização/resync, **dentro de try-catch** — se lançar, **logue e siga** (o dashboard NÃO pode quebrar; já tivemos 2 regressões de runtime por exceção no carregamento). Cuidado com FK de ativo novo (Id=0): persista o ativo antes de referenciar o Id escalar (use navegação `Asset=` ou SaveChanges antes).

Prefira lógica pura testável (cálculo de diff/alvo separado da persistência), no estilo de `ExtratoB3Materializador`/`CalculadoraIr`. Cripto fica FORA (não há Posição importada). NÃO rode `dotnet ef database update`. **NÃO** faça `git add`/`commit` — deixe no working tree.

Aceite: `dotnet build Sistema.sln` + `dotnet test Sistema.sln` (ou só o projeto de testes se a MVC estiver travada pelo app) verdes; testes da lógica pura (diff zera fantasma; alvo da Posição; idempotência). Reporte: arquivos, build/test, como o try-catch garante que não derruba o dashboard, e o efeito esperado (quais fantasmas zeram).
