---
name: eventos-corp-nucleo
description: Implementa a Fase 1 dos eventos corporativos (desdobramento/grupamento) — entidade EventoCorporativo + mapping + migration + ajuste de cotas no carregador central + seed dos splits conhecidos + testes. Use quando for executar a F1 do spec specs/eventos-corporativos.spec.md.
tools: Read, Edit, Write, Grep, Glob, Bash
model: sonnet
---

Você implementa a **Fase 1** do `specs/eventos-corporativos.spec.md`. Carregue a skill `financas-dominio` para o contexto técnico (arquivos, convenções de entidade/migration/seed).

Escopo:
1. Entidade `EventoCorporativo` + enum `TipoEventoCorporativo` em `2 - Dominio/Sistema.CORE/Entities/Financas.cs`; mapping em `FinancasMap.cs` (chave natural = índice único filtrado); `DbSet` no `AppDbContext`; migration `AddEventoCorporativo`.
2. Aplicar o ajuste **só** em `BuscarTodasTransacoesAsync` (`FinancasRepository`): para transação com `Date < Evento.Data`, `Quantity *= fator` e `UnitPrice /= fator` (eventos múltiplos = produto). Não tocar em `BuscarTransacoesAsync`/grids.
3. `EventoCorporativoSeed` com os fatores/datas do §3 do spec (BCFF11 1:8 28/11/2023; GGRC11 1:10 06/03/2024; e os derivados do ratio PM_banco/PM_real — CPTS11 ~1:2, KNSC11, CNES11 — **confirme cada um** antes de gravar).
4. Teste novo: compra pré-split + venda pós-split → saldo e preço médio corretos.

Aceite: `dotnet build` + `dotnet test` verde; nenhum `AssetClass=2` com saldo < 0; posições conferem com a tabela do §3. Reporte divergências que sobrarem (provável split ainda não cadastrado).
