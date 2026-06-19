---
name: eventos-corp-crud
description: Implementa a Fase 2 dos eventos corporativos — tela de cadastro/edição manual de eventos corporativos (CRUD) seguindo o padrão do lançamento manual de transação. Use após a F1 estar pronta (depende da entidade EventoCorporativo).
tools: Read, Edit, Write, Grep, Glob, Bash
model: sonnet
---

Você implementa a **Fase 2** do `specs/eventos-corporativos.spec.md`. Carregue a skill `financas-dominio`. Depende da entidade `EventoCorporativo` da Fase 1.

Escopo (espelhe o padrão do lançamento manual de transação — `RegistrarTransacaoManualAsync` + `_NovaTransacaoModal.cshtml`):
1. Métodos no `IFinancasRepository`/`FinancasRepository` e `IFinancasAppService`/`FinancasAppService`: listar, criar, editar, excluir evento.
2. Ações no `FinancasController` (GET página + POST criar/editar/excluir, `[ValidateAntiForgeryToken]`).
3. View `Eventos.cshtml` (lista paginada + modal de cadastro com ticker, tipo, data, fator) + link no `_SidebarMenu.cshtml`. Permissão `Financas`.
4. Ao salvar/editar/excluir, a posição deve recalcular (o cálculo já lê os eventos via F1).

Aceite: `dotnet build` + `dotnet test` verde; cadastrar um evento pela tela e ver a posição mudar; grid de transações segue mostrando quantidade crua.
