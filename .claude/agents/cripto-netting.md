---
name: cripto-netting
description: Implementa a F1 de specs/cripto.spec.md — netting das transações cripto da Binance (permuta/convert/staking como duas pernas, BRL=caixa fora da posição, earn como entrada de posição) para a posição bater com a corretora (some fantasma e saldo negativo). Valoração BRL e ponte IR ficam para F2/F3. Use depois da spec aprovada.
tools: Read, Edit, Write, Grep, Glob, Bash
model: sonnet
---

Você implementa a **Fase 1 (netting)** de `specs/cripto.spec.md`. **Leia primeiro** `.claude/skills/financas-dominio/SKILL.md` (materialização, armadilhas cripto) e o spec inteiro.

**Investigue os dados ANTES de codar** (é o ponto crítico): leia os CSVs da Binance em `arquivos/financeiro/` (Histórico de Transações/ledger, Trades Spot, Convert) e a tabela de staging `FinanceiroTransacaoCripto` (entidade `TransacaoCripto` em `Financas.cs`; parse em `ImportarTransacoesBinance`/`ImportarTransacaoBinanceRow` no `FinancasImportador.cs`). Entenda como **Convert**, **Small Assets Exchange BNB**, **staking (SOL Staking-Purchase, WBETH2.0-Staking)** e **earn/airdrop/interest** aparecem (no ledger, uma permuta vira **duas linhas**: `Change` negativo do ativo gasto + positivo do recebido, mesmo timestamp/operação).

**Diagnóstico de referência (banco do usuário):** hoje a materialização `SincronizarTransacoesCanonicasAsync` só pega `TransacoesCripto.Where(Price != null && Price > 0)` → as pernas de permuta/earn (sem preço) **não entram**, então a origem nunca é abatida. Resultado: muitos ativos com N compras/0 vendas, stablecoins/**BRL** como posição, e saldos **negativos**.

Escopo da F1 (foco = **posição correta por quantidade**; valoração BRL/custo e IR ficam p/ F2/F3):
1. **Permuta como duas pernas:** ao materializar, a perna de **saída** (Change<0) reduz a posição do ativo origem; a de **entrada** (Change>0) aumenta o destino. Para a quantidade fechar sem preço, use o **PM corrente do ativo** como preço da perna de saída (realizado ~0 nesta fase) e o mesmo valor como custo da entrada — documente. Inclui Convert, Small Assets Exchange, e conversões de staking.
2. **BRL/fiat = caixa:** BRL não é ativo de posição (bug atual "Cripto · BRLUSDT"). Marcar BRL (e fiat) como não-ativo / fora da posição.
3. **Earn/staking/airdrop/interest:** entra como **aumento de posição** (balde Rendimentos), custo = 0 nesta fase (valoração é F2). Não pode poluir o PM de trade.
4. **Sem saldo negativo** para os ativos realmente detidos.

Prefira **lógica pura testável** (ex.: uma função que, dada a lista de movimentos cripto, produz as entradas canônicas com netting), separada da persistência — como fizeram `ExtratoB3Materializador`/`CalculadoraIr`. Se precisar de mudança no parse (staging) + resync, **bump `MaterializacaoVersao`**. NÃO rode `dotnet ef database update`. **NÃO faça git commit** — deixe no working tree para revisão.

Aceite: `dotnet build Sistema.sln` + `dotnet test Sistema.sln` verdes; testes do netting (permuta abate origem; BRL fora; earn soma) com dados sintéticos. Reporte: arquivos alterados, build/test, e — se conseguir — o efeito esperado nas posições (quais fantasmas/negativos somem) e o que ficou para F2/F3.
