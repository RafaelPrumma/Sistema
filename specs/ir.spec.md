# Spec — Submódulo IR (rascunho da declaração + imposto)

> Gera o esboço de tudo que precisa declarar + imposto a pagar, segundo as regras atualizadas, exportável em **Excel ("cola")** para a época de declarar. Consome dados do submódulo Investimentos + os informes em `arquivos/ir/`. Contexto técnico: skill `financas-dominio`.

## Objetivo
Na época do IR, gerar um Excel/relatório com tudo organizado por ficha da declaração, para usar como cola — sem precisar montar na mão.

## Regras (confirmar a cada ano-calendário)
### Ações / FIIs / ETFs (B3)
- **Bens e Direitos**: posição em 31/12 (qtd × preço médio) por ativo, com CNPJ.
- **Rendimentos isentos**: dividendos de ações; rendimento de FII (isento p/ PF).
- **Tributação exclusiva**: JCP (15% IRRF na fonte).
- **Ganhos de capital (renda variável)**: por mês → **DARF**. Ações swing: isenção se vendas ≤ **R$ 20.000/mês**; acima, 15% sobre o lucro. FII: **20%** sempre. Day trade: 20% (apurado à parte).
### Cripto (regra VIGENTE em 2026 — a "regra nova" NÃO passou)
> ⚠️ **A MP 1.303/2025 (17,5% fixo, fim da isenção, apuração trimestral) foi REJEITADA pela Câmara em 08/10/2025 (251×193) e perdeu a validade.** Valem as **regras anteriores**. O tema deve voltar como PL em 2026 → reconfirmar antes de cada ano-calendário (ver `## Disclaimer`).
- **Exchange nacional (PF, custódia direta):** apuração **MENSAL**. Total de **alienações no mês ≤ R$ 35.000 → ganho ISENTO**. Acima de R$35k no mês → ganho do mês pela **tabela progressiva de ganho de capital** (15% até R$5M; 17,5% R$5–10M; 20% R$10–30M; 22,5% acima), DARF (cód. **4600**) até o último dia útil do mês seguinte. Compensa perdas da mesma natureza.
- **Exchange estrangeira / offshore (Lei 14.754/2023):** ganhos tributados **anualmente a 15%**, declarados na DAA. ⚠️ **Binance**: verificar se a operação se dá via entidade BR (regime nacional) ou estrangeira — muda o enquadramento.
- **Bens e Direitos:** saldo de cada cripto em 31/12 ao **custo de aquisição** (grupo 08).

## Saídas
1. **Bens e Direitos** (ações/FII/ETF/cripto em 31/12, custo).
2. **Rendimentos isentos** (dividendos + FII).
3. **Tributação exclusiva** (JCP).
4. **Ganhos de capital / DARF** (vendas por mês, imposto devido, alertas de vencimento).
5. **Cripto** (ganho **mensal**; isenção R$35k/mês em exchange nacional, progressivo acima; exterior 15% anual — Lei 14.754/2023).
6. **Exportação Excel** com uma aba por bloco.

## Fontes
Posições/vendas/proventos do submódulo Investimentos + cruzamento com os **informes oficiais** em `arquivos/ir/` (escrituradores BB/Bradesco = ações; rendimento de FII via Brapi/export B3).

## Critério de aceite
Os valores batem com os informes reais do usuário em `arquivos/ir/` (ex.: dividendos/JCP de 2025) e com os totais de proventos informados (2025 R$8.381 / 2024 R$7.267 / 2023 R$6.053).

## Status (implementação)
- **F1 — motor de cálculo: ✅ feito (jun/2026).** `CalculadoraIr` (puro/testável, `1 - Aplicacao/Sistema.APP/Services/CalculadoraIr.cs`) + DTOs (`IrDtos.cs`): ganho de capital mensal por natureza (Ações isenção R$20k/15%; FII 20%; ETF/BDR 15%; Cripto isenção R$35k + progressivo 15–22,5%), compensação de prejuízo cronológica por natureza, Bens e Direitos em 31/12 ao custo, proventos isentos (dividendos+FII) vs JCP. 7 testes (`CalculadoraIrTests`), `dotnet test` verde. **Simplificações:** não separa day-trade; cripto assume exchange nacional (exterior 15% anual da Lei 14.754/2023 fora); proventos classificados por `IncomeType`.
- **F2 (a fazer):** wiring no app service (alimentar a `CalculadoraIr` a partir do repositório), tela/relatório e **exportação Excel** (saída #6) com uma aba por bloco; cruzar com os informes de `arquivos/ir/` para o aceite.

## Disclaimer
É apoio/cola, **não substitui contador**. Cripto: a MP 1.303/2025 caiu (out/2025) e o tema deve voltar como PL em 2026 — **reconfirmar a regra vigente a cada ano-calendário** e sinalizá-la na tela.
