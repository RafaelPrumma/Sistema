# arquivos/ — fontes de dados da carteira (versionadas)

Arquivos brutos que alimentam o módulo Finanças, versionados junto com o código para a **reimportação ser reproduzível**.

## Estrutura
- **`financeiro/`** — fonte da importação da carteira:
  - `Nubank_notas_de_negociacao_*.pdf` — notas de negociação B3 (compras/vendas de ações, FIIs, ETFs). São exports que **se sobrepõem** (a mesma nota aparece em vários arquivos); o importador deduplica por Sha256 do documento + chave natural da operação.
  - `Binance-Historico-*.xlsx`, `*.csv` — trades spot, convert, depósitos e ledger (Simple Earn/staking) da Binance.
  - `NU_40648231_*.pdf` — extratos da NuConta; `Nubank_2026-*.pdf` — faturas de cartão (não geram trade).
- **`ir/`** — Informes de Rendimentos por ano (proventos por ticker dos escrituradores BB/Bradesco). Usado para proventos de **ações** (FII rendimento não vem aqui — ver skill). Notas fiscais médicas foram removidas.

## ⚠️ Privacidade
Contém **CPF, número de conta, extratos e informes fiscais**. **Mantenha o repositório PRIVADO e não dê push num remoto público** — o histórico do git não esquece. Se for publicar o projeto, remova `arquivos/` do versionamento antes (`.gitignore` + rewrite de histórico).

## Importação
A pasta monitorada (`Configuracao` Financas/WatchedFolderPath) aponta para `arquivos/financeiro`. No 1º acesso ao dashboard, `GarantirCargaInicialAsync` importa automaticamente.
