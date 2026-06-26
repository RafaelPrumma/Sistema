# Spec — Submódulo IR (rascunho da declaração + imposto)

> Gera o esboço de tudo que precisa declarar + imposto a pagar, segundo as regras atualizadas, exportável em **Excel ("cola")** para a época de declarar. Consome dados do submódulo Investimentos + os informes em `arquivos/ir/`. Contexto técnico: skill `financas-dominio`.

## Objetivo
Na época do IR, gerar um Excel/relatório com tudo organizado por ficha da declaração, para usar como cola — sem precisar montar na mão.

## Estado (jun/2026)
✅ **Feito:** motor `CalculadoraIr` (puro) — ganho de capital B3 mensal (isenções/alíquotas Ações/FII/ETF/BDR), proventos isentos/JCP, Bens e Direitos; **cripto como EXTERIOR** (Lei 14.754, 15% anual sobre o ganho líquido, **sem** isenção R$35k) com Operações de Ganho (alienações valoradas em BRL pela F2 da cripto), Rewards, **B&D com código RFB** (08-01 BTC / 08-02 altcoin / 08-03 stablecoin / 08-99 token) + situação 31/12 anterior, e flag **IN 1888** (mês de cripto > R$30k). Tela `/Financas/IR` + **export Excel espelhando 8 abas** (Resumo · Como_Usar · Bens_Direitos · Aplic_Fin_Exterior · Operacoes_Ganho · Rendimentos_Rewards · Resumo_Mensal · Regras_Fontes). Reparo de classe (FII gravado como ETF) corrige a alíquota de FII para 20%.
🔲 **Falta:** **aceite real** contra os informes de `arquivos/ir/` e o consolidado do usuário (só rodando o app); aba **Compras_BRL** (expor os aportes em reais no DTO); **exchange nacional como opção** (config, p/ corretora nacional); day-trade.

## Regras (confirmar a cada ano-calendário)
### Ações / FIIs / ETFs (B3)
- **Bens e Direitos**: posição em 31/12 (qtd × preço médio) por ativo, com CNPJ.
- **Rendimentos isentos**: dividendos de ações; rendimento de FII (isento p/ PF).
- **Tributação exclusiva**: JCP (15% IRRF na fonte).
- **Ganhos de capital (renda variável)**: por mês → **DARF**. Ações swing: isenção se vendas ≤ **R$ 20.000/mês**; acima, 15% sobre o lucro. FII: **20%** sempre. Day trade: 20% (apurado à parte).
### Cripto — regra VIGENTE em 2026 (modelo: consolidado real do usuário, IRPF 2026)
> ⚠️ **A MP 1.303/2025 (17,5% fixo, fim da isenção, trimestral) foi REJEITADA (Câmara 08/10/2025) e perdeu validade.** Valem as regras anteriores. Reconfirmar a cada ano (ver `## Disclaimer`).

- **Enquadramento — Binance = EXTERIOR:** os criptos na Binance são tratados como **aplicação financeira no exterior** (Lei 14.754/2023): ficha própria na DAA (situação 31/12, rendimento/perda anual, imposto pago no exterior, país). *(Custódia direta nacional usaria ganho de capital mensal com isenção R$35k — manter como opção/config.)*
- **Bens e Direitos (grupo 08):** cada cripto ao **custo de aquisição** em 31/12, com **código RFB**: `08-01` Bitcoin · `08-02` altcoins/outras · `08-03` stablecoin · `08-99` outros (tokens de **staking**: WBETH, BNSOL — declarar separados). **Obrigatório se custo ≥ R$ 5.000** por tipo. Precisa do saldo em 31/12 do ano **e do anterior** (variação) + texto de **Discriminação** por ativo.
- **Ganho de capital — PERMUTA é alienação (crítico):** **toda alienação é tributável, inclusive permuta cripto-cripto** (não só venda por BRL). Conta: `Convert` cripto→cripto, `Small Assets Exchange BNB`, e as conversões de **staking** (ETH→WBETH, SOL→BNSOL). Ganho = valor de mercado na data − custo. Apuração mensal; isenção R$35k/mês de alienações (regime nacional) / regime exterior (14.754).
- **IN 1888/2019 (obrigação de DECLARAR, ≠ imposto):** informar à RFB as operações do mês quando o total **> R$ 30.000** — mesmo sem imposto. Sinalizar o mês que passa (≠ da isenção de R$35k de ganho).
- **Rendimentos/Rewards:** earn, staking, airdrop, `Simple Earn Flexible Interest`, `Crypto Box` = **rendimento tributável**, valorado em BRL na **data do recebimento**.
- **Conversão em reais:** valores em moeda estrangeira pelo câmbio da data (regra RFB).

## Saídas
1. **Bens e Direitos** (ações/FII/ETF/cripto em 31/12, custo).
2. **Rendimentos isentos** (dividendos + FII).
3. **Tributação exclusiva** (JCP).
4. **Ganhos de capital / DARF** (vendas por mês, imposto devido, alertas de vencimento).
5. **Cripto** (ganho **mensal**; isenção R$35k/mês em exchange nacional, progressivo acima; exterior 15% anual — Lei 14.754/2023).
6. **Exportação Excel** com uma aba por bloco.

## Estrutura do export (modelo = consolidado real de cripto do usuário)
A "cola" deve espelhar as abas do consolidado validado (`IRPF_2026_cripto_Binance_..._consolidado.xlsx`):
- **Resumo** — ano-calendário, arquivos-fonte, depósitos BRL, volume conhecido/estimado, mês que passou de R$30k (IN 1888), atenções.
- **Como_Usar** — passo a passo de onde lançar cada bloco na declaração.
- **Bens_Direitos** — por ativo: código RFB (08-01/02/03/99), qtd+custo em **31/12 do ano e do anterior**, "obrigatório?" (limite R$5.000), texto de Discriminação.
- **Aplic_Fin_Exterior** — mesmos ativos como aplicação no exterior (Lei 14.754): situação 31/12, rendimento/perda anual, imposto pago no exterior, país.
- **Operacoes_Ganho** — uma linha por **alienação** (venda **e permuta/convert/staking**): data, mês, tipo/fonte, ativo, qtd, valor de alienação, custo, ganho/perda.
- **Rendimentos_Rewards** — earn/staking/airdrop/interest: data, mês, operação, ativo, qtd, valor BRL.
- **Compras_BRL** · **Resumo_Mensal** (com flag R$30k IN 1888) · **Regras_Fontes** (regra + link RFB).

## Fontes
Posições/vendas/proventos do submódulo Investimentos + cruzamento com os **informes oficiais** em `arquivos/ir/` (escrituradores BB/Bradesco = ações; rendimento de FII via Brapi/export B3).

## Critério de aceite
Os valores batem com os informes reais do usuário em `arquivos/ir/` (ex.: dividendos/JCP de 2025) e com os totais de proventos informados (2025 R$8.381 / 2024 R$7.267 / 2023 R$6.053).

## Status (implementação)
- **F1 — motor de cálculo: ✅ feito (jun/2026).** `CalculadoraIr` (puro/testável, `1 - Aplicacao/Sistema.APP/Services/CalculadoraIr.cs`) + DTOs (`IrDtos.cs`): ganho de capital mensal por natureza (Ações isenção R$20k/15%; FII 20%; ETF/BDR 15%; Cripto isenção R$35k + progressivo 15–22,5%), compensação de prejuízo cronológica por natureza, Bens e Direitos em 31/12 ao custo, proventos isentos (dividendos+FII) vs JCP. 7 testes (`CalculadoraIrTests`), `dotnet test` verde. **Simplificações:** não separa day-trade; cripto assume exchange nacional (exterior 15% anual da Lei 14.754/2023 fora); proventos classificados por `IncomeType`.
- **F2 — wiring + tela + Excel: ✅ feito (jun/2026).** `ObterApuracaoIrAsync(ano)`/`ExportarApuracaoIrExcelAsync(ano)` no `FinancasAppService` (lê do carregador central `BuscarTodasTransacoesAsync` — já com split — + `BuscarRendimentosAsync`); tela `/Financas/IR` (seletor de ano, blocos B3/cripto/B&D/proventos, link no menu); **export `.xlsx` uma aba por bloco** via `EscritorXlsx` (writer OOXML próprio, **sem dependência externa**) + `ExcelApuracaoIr`. 2 testes round-trip (lê de volta com `ExtratoConsolidadoB3Reader`), build (inclui Razor) + test verdes. **Falta (F3):** validar o aceite contra os informes reais de `arquivos/ir/` rodando o app.
- **F3+ — lacunas reveladas pelo consolidado real de cripto (jun/2026):** o `CalculadoraIr` atual ainda **não** cobre, p/ cripto: (a) **permuta cripto-cripto como alienação** (hoje só conta `Venda` — precisa tratar `Convert`/`Small Assets Exchange`/staking ETH→WBETH/SOL→BNSOL como alienação tributável); (b) **B&D com código RFB** (08-01/02/03/99) + saldo em **dois 31/12** + Discriminação; (c) **enquadramento exterior** (Lei 14.754) separado do nacional; (d) **rewards/earn/airdrop como rendimento tributável** de cripto (hoje earn vira provento genérico); (e) **flag IN 1888 (>R$30k/mês)**; (f) export espelhando as 9 abas do modelo. Fonte do modelo: consolidado validado do usuário (não versionar — tem dado pessoal).

## Disclaimer
É apoio/cola, **não substitui contador**. Cripto: a MP 1.303/2025 caiu (out/2025) e o tema deve voltar como PL em 2026 — **reconfirmar a regra vigente a cada ano-calendário** e sinalizá-la na tela.
