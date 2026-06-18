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
### Cripto (MUDOU — MP 1.303/2025, a partir de 01/01/2026; confirmar conversão em lei)
- **17,5% fixo** sobre o ganho; **fim** da isenção de R$ 35k/mês.
- Apuração **trimestral**; recolhe até o último dia útil do mês seguinte ao trimestre.
- Inclui **custódia própria** e **exchange estrangeira**. Compensação de perdas por até 5 anos.
- Bens e Direitos: saldo de cada cripto em 31/12 (custo de aquisição).
- (Histórico até 2025: isenção até R$ 35k/mês de vendas.)

## Saídas
1. **Bens e Direitos** (ações/FII/ETF/cripto em 31/12, custo).
2. **Rendimentos isentos** (dividendos + FII).
3. **Tributação exclusiva** (JCP).
4. **Ganhos de capital / DARF** (vendas por mês, imposto devido, alertas de vencimento).
5. **Cripto** (ganho trimestral, imposto 17,5%).
6. **Exportação Excel** com uma aba por bloco.

## Fontes
Posições/vendas/proventos do submódulo Investimentos + cruzamento com os **informes oficiais** em `arquivos/ir/` (escrituradores BB/Bradesco = ações; rendimento de FII via Brapi/export B3).

## Critério de aceite
Os valores batem com os informes reais do usuário em `arquivos/ir/` (ex.: dividendos/JCP de 2025) e com os totais de proventos informados (2025 R$8.381 / 2024 R$7.267 / 2023 R$6.053).

## Disclaimer
É apoio/cola, **não substitui contador**. Regras de cripto dependem da conversão da MP em lei — sinalizar a regra vigente na tela.
