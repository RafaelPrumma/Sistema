# Handoff — continuar o módulo Finanças (próximo chat)

Branch: **`feat/cripto-e-b3-base`** (não mergeado). Contexto técnico: skill `financas-dominio`. Estado/decisões: memória `financeiro-roadmap-estado`. Specs: `specs/` (`investimentos`, `importador-b3`, `cripto`, `ir`, `eventos-corporativos`, `financas`).

## Estado (jun/2026)
B3 virou a **fonte de verdade** das posições. Feito nesta branch (build + **113 testes** verdes; `MaterializacaoVersao = 10`; ~12 commits focados):
- Importador B3: F1 (parser) + F2 (materialização, precedência **B3 manda**) + **F3 reconciliação pela Posição + ativo `VARIACAO`** (à prova de falha).
- Fracionário "F" unificado (ITUB4F→ITUB4); alias **IRIM11→IRDM11**; **BNB** tirado de carteira duplicada (fix no banco).
- Eventos corporativos (split) completo (F1+F2+F3); motores de **IR** (cálculo+tela+Excel) e **rentabilidade** (TWR/MWR).
- Padronizacao PT-BR das tabelas financeiras escolhidas + `FinanceiroPosicaoAtivo` como read model recalculavel. `FinanceiroTransacao` segue como fonte da verdade; dashboard de patrimonio/carteiras/posicoes deve ler posicao pronta + cotacoes + carteiras, sem recalcular todas as transacoes em cada ilha. `RawJson` permanece onde ja existia; a nova projecao nao tem `RawJson`.
- ⚠️ **Pendente: validar no app rodando** — rebuild + restart dispara o resync (v10) + a reconciliação; depois conferir no banco se os fantasmas (NCHB11/EQIN11/CMIG3/IRDM11) zeraram e o VARIAÇÃO mostra a diferença.

## Próximas tarefas (pedidas pelo Rafael — ordem sugerida)
1. **BTC/cripto Earn** (`cripto.spec.md` F2) — **mais importante**: a maior parte do BTC está no **Earn** e não conta → o maior ativo aparece como minoria. Refazer o netting (Spot+Convert+ledger combinados sem perder trade; Earn/Staking conta como detido) **ou** reconciliar pelos saldos reais por moeda (mesmo mecanismo VARIAÇÃO). BTC Earn ≈ 0,054.
2. **Carteiras hierárquicas** (`investimentos.spec.md` F-I): topo = Bancário / FIIs / Criptomoedas / **Comodities e energia** (junta petróleo+minério+energia). Subcarteiras: FIIs→Papel/Tijolo; Comodities→por classificação; Criptos→BTC/Altcoins/Memecoins. Precisa de hierarquia na `FinanceiroCarteira` (`CarteiraPaiId`) + classificação (FII papel×tijolo; cripto categorias) + ajustar auto-sugestão e a UI do dashboard.
3. **Carteiras com valor zerado** (F-J): algumas exibem 0 — corrigir valoração (fallback custo / mapeamento ativo→carteira).
4. **Card de proventos no dashboard** (F-K): dados já existem (`RendimentoInvestimento`); só falta a ilha lazy-loaded.

## Como trabalhar (preferências do Rafael)
- Trabalhar **na branch**; **commit focado por subtrabalho**; **usar agents** pra implementar; **verificar build/test** antes de commitar (e independentemente).
- Banco acessível: SQL Server local `SISTEMA` (`sqlcmd -S localhost -E -d SISTEMA`). Usar pra diagnosticar posições reais.
- **Tudo que roda em `GarantirCargaInicialAsync` (load do dashboard) DEVE ser à prova de falha (try-catch)** — já houve 2 regressões que derrubaram o dashboard.
- O app costuma ficar **rodando** (trava a DLL da MVC no build da solução) → testar com `dotnet test "4 - Testes/Sistema.Tests/Sistema.Tests.csproj"`.
- **NÃO** versionar `arquivos/` (CPF/custódia; repo privado, sem push). **NÃO** rodar `dotnet ef database update` (o app aplica no startup).