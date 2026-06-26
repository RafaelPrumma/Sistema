# Spec — Submódulo Gestão de Gastos

> ⏸️ **ADIADO — só começa depois de terminar Investimentos.** Controle financeiro pessoal estilo **GuiaBolso / Minhas Economias / Mobills**: cartão + conta → categorização → receita vs despesa → orçamento e fluxo de caixa. Contexto técnico: skill `financas-dominio`. Esta spec é detalhada para servir de base aos **subagentes** quando chegar a vez.

## Objetivo
Entrar com os gastos do **cartão** e da **conta corrente**, categorizá-los (o que gastei, quanto ganhei), planejar (orçamento) e visualizar por categoria/mês — sem digitar tudo à mão, porque os dados **já estão importados**.

## Sacada — os dados já estão no sistema
A importação já traz, mas hoje **não usa**:
- **Faturas Nubank** (`arquivos/financeiro/Nubank_2026-*.pdf`) → **cartão de crédito** (compras com data/descrição/valor, parcelas, estabelecimento).
- **Extratos da NuConta** (`arquivos/financeiro/NU_40648231_*.pdf`) → **conta** (Pix, débito, crédito em conta, salário, compra de ações, RDB).

Hoje o importador guarda como texto bruto/alerta (`ExtratoContaNubank`/`ExtratoInvestimentosNubank`) sem extrair lançamentos. Este submódulo passa a **parsear e categorizar**.

## Modelo de dados
- **`LancamentoGasto`**: `Data`, `Descricao`, `Valor`, `Tipo` (Receita/Despesa/Transferência/Aporte), `CategoriaId`, `Fonte` (Cartão/Conta), `Estabelecimento`, `ParcelaAtual/ParcelaTotal`, `RecorrenteId?`, `SourceDocumentId`, `ChaveNatural`, `RawJson`, auditoria. Idempotente por chave natural (data+descrição+valor+fonte).
- **`CategoriaGasto`**: `Nome`, `Tipo` (Receita/Despesa), `Icone`, `Cor`, `CategoriaPaiId?` (subcategorias), `Ativo`.
- **`RegraCategorizacao`**: padrão de descrição (`Contém`/regex) → `CategoriaId`, prioridade, origem (sistema/aprendida). Aprende com correções manuais.
- **`OrcamentoCategoria`**: `CategoriaId`, `Mes/Ano` (ou recorrente), `ValorPlanejado` — base do "planejado vs realizado".
- **`Recorrencia`**: assinatura/salário/conta fixa (`Descricao`, `ValorEstimado`, `Periodicidade`, `DiaVencimento`, `CategoriaId`, `Ativo`) — alimenta lembretes e fluxo de caixa projetado.

## Features (o que pode ser desenvolvido — base GuiaBolso/Minhas Economias/Mobills)

**Núcleo (MVP)**
1. **Parser** faturas + extratos → `LancamentoGasto` (idempotente; reaproveita `FinancasImportador`/conteúdo bruto já persistido; novo materializador, estilo `ExtratoB3Materializador`).
2. **Categorização automática** por `RegraCategorizacao` (ex.: "Uber/99"→Transporte, "iFood/Rappi"→Alimentação, "Nubank Rendimentos"→Receita) + **ajuste manual** que vira regra aprendida.
3. **Separar gasto real de investimento:** "Compra de Ações/Aplicação RDB" no extrato = **Aporte**, não despesa → vincular ao submódulo Investimentos (pode alimentar a curva de aportes do F-C).
4. **Visões:** receita vs despesa do mês, por categoria (pizza/barras), evolução mensal, maiores gastos, saldo. Tela de lançamentos (grid editável) + categorias.

**Planejamento (estilo Mobills/Minhas Economias)**
5. **Orçamento por categoria** (`OrcamentoCategoria`): planejado vs realizado no mês, % consumido, **alerta de estouro** (reaproveita o motor de alertas/`AlertaPreco` → notificação interna).
6. **Contas a pagar/receber recorrentes** (`Recorrencia`): assinaturas, salário, contas fixas + **lembretes** (vira alerta interno) e marcação pago/pendente.
7. **Cartão de crédito:** fatura do mês, limite usado/disponível, **parcelamentos futuros** (lançamentos com parcela), melhor dia de compra.
8. **Fluxo de caixa projetado:** saldo futuro combinando recorrências + faturas + lançamentos previstos.

**Análise (estilo GuiaBolso)**
9. **Saúde financeira / score:** % da receita comprometida, gasto vs receita, tendência; "onde dá pra cortar".
10. **Relatórios:** mês a mês, ano, por categoria/subcategoria, comparativo entre períodos; tags além de categoria.

## Skills/abordagem dos subagentes (quando chegar a vez)
Dividir em fases, **um subagente por fase**, cada um seguindo a skill `financas-dominio` (seção "Fluxo com subagentes"):
- **G1 — Modelo + parser:** entidades (`LancamentoGasto`/`CategoriaGasto`/`RegraCategorizacao`) + mapping + migration (snapshot junto, sem `database update`) + materializador idempotente das faturas/extratos. Seed das categorias e regras iniciais. Testes do parser com fixtures reais.
- **G2 — Categorização + correção:** auto-categorização por regra + tela de ajuste que **aprende** (cria/ajusta `RegraCategorizacao`). Ponte com Investimentos (aportes ≠ despesa).
- **G3 — Visões/dashboard:** ilhas no padrão existente (controller `Dashboard{X}` → `Obter{X}DashboardAsync` → parcial + `financas.js`; gráfico monta no JS); receita×despesa, por categoria, evolução, maiores gastos.
- **G4 — Orçamento + recorrências + fluxo de caixa:** `OrcamentoCategoria`/`Recorrencia` + alertas (reusa o motor de alertas) + projeção de saldo.
- **G5 — Análise:** score de saúde financeira + relatórios/comparativos.
Constraints sempre: à prova de falha no load; testar via `Sistema.Tests`; 0 warnings; commits focados; `git add` só dos próprios arquivos; sem push; **privacidade** (faturas/extratos têm dado sensível — `arquivos/` repo privado, sem push).

## Cuidados
- **Privacidade:** faturas/extratos têm CPF/conta/compras — mesma regra do `arquivos/` (repo privado, sem push).
- **Não duplicar com Investimentos:** "Compra de Ações/Aplicação RDB" são aportes, não despesas; transferências entre contas próprias não são gasto.
- **Idempotência:** reimportar a mesma fatura não duplica lançamentos (chave natural).

## Critério de aceite
Importar uma fatura + um extrato e ver os lançamentos categorizados, com receita vs despesa do mês coerente, orçamento por categoria (planejado vs realizado) e os aportes separados das despesas.
