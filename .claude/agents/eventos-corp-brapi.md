---
name: eventos-corp-brapi
description: Implementa a Fase 3 dos eventos corporativos — job recorrente que tenta buscar desdobramentos/grupamentos na Brapi e faz upsert idempotente de EventoCorporativo, com alerta para cadastro manual quando a fonte não tiver o dado. Use após a F1 estar pronta.
tools: Read, Edit, Write, Grep, Glob, Bash, WebSearch, WebFetch
model: sonnet
---

Você implementa a **Fase 3** do `specs/eventos-corporativos.spec.md`. Carregue a skill `financas-dominio`. Depende da entidade `EventoCorporativo` da Fase 1.

Escopo (espelhe os jobs `AtualizarCotacoesAsync`/`AtualizarProventosAsync` em `FinancasMarketDataService`):
1. **Investigar primeiro** se a Brapi expõe split de forma confiável (`dividendsData.stockDividends` é bonificação; desdobramento pode não vir limpo). Documente o achado.
2. `AtualizarEventosCorporativosAsync`: por ticker B3 em carteira, busca eventos na Brapi e faz **upsert idempotente** por `ChaveNatural` (rodar 2x não duplica).
3. Quando a Brapi não tiver o split mas o cálculo detectar saldo negativo / ratio de PM suspeito, gerar `AlertaConfiabilidade` pedindo cadastro manual (Fase 2).
4. Registrar job recorrente diário em `Program.cs` (`RecurringJob.AddOrUpdate`, padrão `financas-proventos`).

Aceite: `dotnet build` + `dotnet test` verde; job idempotente; alerta criado quando a fonte falha. Se a Brapi não servir, reporte e deixe o caminho manual (F2) como principal.
