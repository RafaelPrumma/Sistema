using Sistema.APP.DTOs;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

/// <summary>
/// Motor PURO de apuração de IR (sem banco): ganho de capital de renda variável B3 + cripto,
/// Bens e Direitos em 31/12 e proventos (isentos/JCP). Apoio/"cola" — NÃO substitui contador.
/// Regras: ver specs/ir.spec.md (regra cripto VIGENTE em 2026 — a MP 1.303/2025 foi rejeitada).
///
/// Simplificações de F1 (documentadas):
///  - não separa day-trade (tudo tratado como swing);
///  - cripto assume exchange NACIONAL (isenção R$35k/mês); exterior 15% anual (Lei 14.754/2023) não tratado;
///  - compensação de prejuízo é cronológica por natureza e atravessa anos (prejuízo de mês isento não é usado);
///  - proventos classificados pelo IncomeType (texto).
/// </summary>
public static class CalculadoraIr
{
    public static ApuracaoIrDto Apurar(
        int ano,
        IReadOnlyList<TransacaoFinanceira> transacoes,
        IReadOnlyList<RendimentoInvestimento> proventos)
    {
        var ganhos = ApurarGanhosMensais(ano, transacoes);
        var bensDireitos = MontarBensEDireitos(ano, transacoes);
        var (isentos, exclusiva) = ClassificarProventos(ano, proventos);
        var totalImposto = ganhos.Sum(g => g.Imposto);
        return new ApuracaoIrDto(ano, ganhos, bensDireitos, isentos, exclusiva, totalImposto);
    }

    // --- Ganho de capital mensal por natureza, com isenção por volume e compensação de prejuízo ---

    private static List<ApuracaoMensalIrDto> ApurarGanhosMensais(int ano, IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var vendas = ExtrairVendasRealizadas(transacoes);
        var saida = new List<ApuracaoMensalIrDto>();

        foreach (var natureza in vendas.GroupBy(v => v.Natureza))
        {
            var limiteIsencao = natureza.First().LimiteIsencao;
            var aliquotaFn = natureza.First().Aliquota;
            var prejuizo = 0m; // prejuízo acumulado da natureza (compensa meses futuros).

            // Cronológico (inclui anos anteriores) para a compensação atravessar anos corretamente.
            var meses = natureza
                .GroupBy(v => (v.Date.Year, v.Date.Month))
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            foreach (var mes in meses)
            {
                var totalVendas = mes.Sum(v => v.Proceeds);
                var bruto = mes.Sum(v => v.Gain);
                var isento = limiteIsencao > 0m && totalVendas <= limiteIsencao;

                decimal compensado = 0m, baseCalc = 0m, aliquota = 0m, imposto = 0m;
                if (!isento)
                {
                    if (bruto >= 0m)
                    {
                        compensado = Math.Min(prejuizo, bruto);
                        baseCalc = bruto - compensado;
                        prejuizo -= compensado;
                        aliquota = aliquotaFn(baseCalc);
                        imposto = Math.Round(baseCalc * aliquota, 2);
                    }
                    else
                    {
                        prejuizo += -bruto; // mês com prejuízo: acumula para compensar adiante.
                    }
                }

                if (mes.Key.Year == ano)
                {
                    saida.Add(new ApuracaoMensalIrDto(
                        mes.Key.Year, mes.Key.Month, natureza.Key,
                        Math.Round(totalVendas, 2), Math.Round(bruto, 2),
                        Math.Round(compensado, 2), Math.Round(baseCalc, 2),
                        aliquota, imposto, isento));
                }
            }
        }

        return saida.OrderBy(r => r.Mes).ThenBy(r => r.Natureza).ToList();
    }

    private sealed record Venda(DateTime Date, string Natureza, decimal LimiteIsencao, Func<decimal, decimal> Aliquota, decimal Proceeds, decimal Gain);

    private static List<Venda> ExtrairVendasRealizadas(IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var vendas = new List<Venda>();
        var estado = new Dictionary<int, (decimal Qtd, decimal Custo)>();

        foreach (var t in transacoes.Where(t => t.Asset is not null).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            var st = estado.TryGetValue(t.AssetId, out var s) ? s : (Qtd: 0m, Custo: 0m);
            var delta = Delta(t);
            if (delta > 0m)
            {
                st.Custo += t.Quantity * t.UnitPrice + t.Fees;
                st.Qtd += t.Quantity;
            }
            else if (delta < 0m)
            {
                var pm = st.Qtd > 0m ? st.Custo / st.Qtd : 0m;
                var reduz = Math.Min(t.Quantity, st.Qtd);
                var regra = Regra(t.Asset!);
                if (regra is not null)
                {
                    var proceeds = t.Quantity * t.UnitPrice;
                    var gain = proceeds - t.Fees - reduz * pm;
                    vendas.Add(new Venda(t.Date.Date, regra.Value.Natureza, regra.Value.Limite, regra.Value.Aliquota, proceeds, gain));
                }
                st.Custo -= reduz * pm;
                st.Qtd -= t.Quantity;
                if (st.Qtd <= 0.000000000001m) { st.Qtd = 0m; st.Custo = 0m; }
            }
            estado[t.AssetId] = st;
        }

        return vendas;
    }

    // --- Bens e Direitos: posição em 31/12 do ano, ao custo médio acumulado ---

    private static List<BemDireitoIrDto> MontarBensEDireitos(int ano, IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var corte = new DateTime(ano, 12, 31);
        var estado = new Dictionary<int, (decimal Qtd, decimal Custo, AtivoFinanceiro Asset)>();

        foreach (var t in transacoes.Where(t => t.Asset is not null && t.Date.Date <= corte).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            var st = estado.TryGetValue(t.AssetId, out var s) ? s : (Qtd: 0m, Custo: 0m, Asset: t.Asset!);
            var delta = Delta(t);
            if (delta > 0m)
            {
                st.Custo += t.Quantity * t.UnitPrice + t.Fees;
                st.Qtd += t.Quantity;
            }
            else if (delta < 0m)
            {
                var pm = st.Qtd > 0m ? st.Custo / st.Qtd : 0m;
                var reduz = Math.Min(t.Quantity, st.Qtd);
                st.Custo -= reduz * pm;
                st.Qtd -= t.Quantity;
                if (st.Qtd <= 0.000000000001m) { st.Qtd = 0m; st.Custo = 0m; }
            }
            st.Asset = t.Asset!;
            estado[t.AssetId] = st;
        }

        return estado.Values
            .Where(v => v.Qtd > 0.000001m)
            .Select(v => new BemDireitoIrDto(
                v.Asset.Sigla ?? v.Asset.Chave ?? v.Asset.Nome ?? "?",
                v.Asset.Classe.ToString(),
                Math.Round(v.Qtd, 8),
                Math.Round(v.Custo, 2)))
            .OrderBy(b => b.Classe).ThenBy(b => b.Ticker)
            .ToList();
    }

    // --- Proventos: isentos (dividendos + FII) vs tributação exclusiva (JCP) ---

    private static (List<RendimentoIrDto> Isentos, List<RendimentoIrDto> Exclusiva) ClassificarProventos(
        int ano, IReadOnlyList<RendimentoInvestimento> proventos)
    {
        var doAno = proventos.Where(p => (p.PaymentDate ?? p.ReferenceDate)?.Year == ano);
        var isentos = new List<RendimentoIrDto>();
        var exclusiva = new List<RendimentoIrDto>();

        foreach (var g in doAno.GroupBy(p => ClassificarTipo(p.IncomeType)))
        {
            var total = Math.Round(g.Sum(p => p.Amount), 2);
            if (total <= 0m)
                continue;
            var item = new RendimentoIrDto(g.Key.Rotulo, total);
            if (g.Key.Categoria == CategoriaProvento.Exclusiva)
                exclusiva.Add(item);
            else if (g.Key.Categoria == CategoriaProvento.Isento)
                isentos.Add(item);
            // Tributável (earn cripto) fica fora destes blocos — entra na apuração de cripto.
        }

        return (isentos, exclusiva);
    }

    private enum CategoriaProvento { Isento, Exclusiva, Tributavel }

    private static (CategoriaProvento Categoria, string Rotulo) ClassificarTipo(string? incomeType)
    {
        var t = (incomeType ?? string.Empty).ToUpperInvariant();
        if (t.Contains("JCP") || t.Contains("JURO"))
            return (CategoriaProvento.Exclusiva, "Juros sobre Capital Próprio (JCP)");
        if (t.Contains("DIVIDEND"))
            return (CategoriaProvento.Isento, "Dividendos");
        if (t.Contains("EARN") || t.Contains("STAK") || t.Contains("CRIPTO"))
            return (CategoriaProvento.Tributavel, "Rendimentos de cripto (earn)");
        return (CategoriaProvento.Isento, "Rendimentos de FII / isentos");
    }

    // --- Helpers ---

    // Tabela progressiva de ganho de capital (Lei 13.259/2016), usada para cripto acima da isenção.
    private static decimal AliquotaGanhoCapital(decimal ganho) =>
        ganho <= 5_000_000m ? 0.15m :
        ganho <= 10_000_000m ? 0.175m :
        ganho <= 30_000_000m ? 0.20m : 0.225m;

    private static decimal Delta(TransacaoFinanceira t) => t.OperationType switch
    {
        TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => t.Quantity,
        TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -t.Quantity,
        _ => 0m
    };

    private readonly record struct RegraNatureza(string Natureza, decimal Limite, Func<decimal, decimal> Aliquota);

    private static RegraNatureza? Regra(AtivoFinanceiro a)
    {
        if (a.EhCripto || a.Classe == ClasseAtivo.Cripto)
            return new RegraNatureza("Cripto", 35000m, AliquotaGanhoCapital);

        return a.Classe switch
        {
            ClasseAtivo.Acao => new RegraNatureza("Ações", 20000m, _ => 0.15m),
            ClasseAtivo.FII => new RegraNatureza("FII", 0m, _ => 0.20m),
            ClasseAtivo.ETF => new RegraNatureza("ETF", 0m, _ => 0.15m),
            ClasseAtivo.BDR => new RegraNatureza("BDR", 0m, _ => 0.15m),
            _ => (RegraNatureza?)null // RendaFixa/Caixa/Outro: fora do escopo (tributação na fonte/à parte).
        };
    }
}
