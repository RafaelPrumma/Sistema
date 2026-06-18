# Spec — Submódulo Gestão de Gastos

> Controle financeiro pessoal estilo GuiaBolso/Mobills/Minhas Finanças: cartão + conta → categorização → receita vs despesa. Contexto técnico: skill `financas-dominio`.

## Objetivo
Entrar com os gastos do **cartão** e da **conta corrente**, categorizá-los (o que gastei e quanto ganhei) e visualizar por categoria/mês.

## Sacada — os dados já estão no sistema
A importação já traz, mas hoje **não usa**:
- **Faturas Nubank** (`arquivos/financeiro/Nubank_2026-*.pdf`) → transações de **cartão de crédito** (compras com data/descrição/valor, parcelas).
- **Extratos da NuConta** (`arquivos/financeiro/NU_40648231_*.pdf`) → movimentações de **conta** (Pix, débito, crédito em conta, compra de ações, RDB).

Hoje o importador guarda esses documentos como texto bruto/alerta (`ExtratoContaNubank`/`ExtratoInvestimentosNubank`) sem extrair lançamentos. Este submódulo passa a **parsear e categorizar**.

## Modelo de dados
`LancamentoGasto` (nova entidade): `Data`, `Descricao`, `Valor`, `Tipo` (Receita/Despesa), `CategoriaId`, `Fonte` (Cartão/Conta), `Documento`/origem, auditoria. `CategoriaGasto` (nome, tipo, ícone, regras de auto-categorização por padrão de descrição).

## Features
1. **Parser** das faturas + extratos → `LancamentoGasto` (idempotente por chave natural data+descrição+valor+fonte).
2. **Categorização automática** por regras de descrição (ex.: "Uber"→Transporte, "iFood"→Alimentação) + ajuste/correção manual; aprender com correções.
3. **Visões**: receita vs despesa do mês, por categoria (pizza/barras), evolução mensal, maiores gastos, saldo.
4. **Tela** de lançamentos (grid editável) + categorias + dashboard de gastos.
5. Separar **gasto real** de movimentação de investimento (ex.: "Compra de Ações" no extrato não é despesa — é aporte; vincular ao submódulo Investimentos quando possível).

## Cuidados
- Faturas/extratos têm dados sensíveis — mesma regra de privacidade do `arquivos/` (repo privado).
- Não duplicar com Investimentos: "Compra de Ações/Aplicação RDB" no extrato são aportes, não despesas.

## Critério de aceite
Importar uma fatura + um extrato e ver os lançamentos categorizados, com receita vs despesa do mês coerente.
