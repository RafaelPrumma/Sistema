using Sistema.APP.DTOs;

namespace Sistema.APP.Services;

// F-G — lógica pura de metas + rebalanceamento (sem DbContext, testável isolada).
// Recebe, por carteira-topo, o valor de mercado atual e o peso-alvo agregado (% do patrimônio),
// e produz o desvio (atual − alvo) + a sugestão de aporte para aproximar do alvo.
public static class CalculadoraMetasCarteira
{
    // Entrada mínima por carteira: identidade + valor de mercado + peso-alvo (% do patrimônio, ou null
    // quando a carteira não tem alvo definido). O cálculo do peso atual é responsabilidade desta classe.
    public readonly record struct EntradaMeta(int CarteiraId, string Nome, decimal ValorMercado, decimal? PesoAlvo);

    // aporteHipotetico: caixa novo a distribuir entre as carteiras abaixo do alvo (>= 0). Quando 0, o DTO
    // ainda traz "FaltaParaAlvo" (estado atual) — só não há sugestão de distribuição de aporte.
    public static FinancasMetasDto Calcular(IReadOnlyList<EntradaMeta> entradas, decimal aporteHipotetico = 0m)
    {
        entradas ??= [];
        if (aporteHipotetico < 0m)
            aporteHipotetico = 0m;

        var patrimonio = entradas.Sum(e => e.ValorMercado);
        // Só carteiras com alvo definido (> 0) viram "meta". Sem nenhuma meta → ilha não aparece.
        var comAlvo = entradas.Where(e => e.PesoAlvo is > 0m).ToList();
        if (comAlvo.Count == 0)
            return new FinancasMetasDto([], Math.Round(patrimonio, 2), 0m, Math.Round(aporteHipotetico, 2), SemMetas: true, AlvoForaDeCem: false);

        var somaAlvo = comAlvo.Sum(e => e.PesoAlvo!.Value);

        // Déficit (R$) por carteira no patrimônio ATUAL: quanto falta para o valor-alvo. Base da distribuição
        // do aporte hipotético (proporcional ao déficit, capada no próprio déficit — não ultrapassa o alvo).
        var deficits = comAlvo.ToDictionary(
            e => e.CarteiraId,
            e => Math.Max(0m, (e.PesoAlvo!.Value / 100m * patrimonio) - e.ValorMercado));
        var somaDeficit = deficits.Values.Sum();

        var carteiras = comAlvo
            .Select(e =>
            {
                var alvo = e.PesoAlvo!.Value;
                var pesoAtual = patrimonio == 0m ? 0m : e.ValorMercado / patrimonio * 100m;
                var desvioPontos = pesoAtual - alvo;
                var desvioPercentual = alvo == 0m ? 0m : desvioPontos / alvo * 100m;

                var valorAlvo = alvo / 100m * patrimonio;
                var falta = Math.Max(0m, valorAlvo - e.ValorMercado);
                var sobra = Math.Max(0m, e.ValorMercado - valorAlvo);

                // Aporte sugerido = fatia proporcional ao déficit desta carteira sobre o déficit total.
                var aporte = somaDeficit == 0m || aporteHipotetico == 0m
                    ? 0m
                    : Math.Min(falta, aporteHipotetico * (deficits[e.CarteiraId] / somaDeficit));

                return new MetaCarteiraDto(
                    e.CarteiraId,
                    e.Nome,
                    Math.Round(e.ValorMercado, 2),
                    Math.Round(pesoAtual, 2),
                    Math.Round(alvo, 2),
                    Math.Round(desvioPontos, 2),
                    Math.Round(desvioPercentual, 2),
                    Math.Round(falta, 2),
                    Math.Round(sobra, 2),
                    Math.Round(aporte, 2));
            })
            // Maior desvio absoluto primeiro (o que mais precisa de atenção).
            .OrderByDescending(m => Math.Abs(m.DesvioPontos))
            .ToList();

        // Sanidade: soma dos alvos deveria ser ~100%. Tolerância de 0,5 p.p. para arredondamentos.
        var alvoForaDeCem = Math.Abs(somaAlvo - 100m) > 0.5m;

        return new FinancasMetasDto(
            carteiras,
            Math.Round(patrimonio, 2),
            Math.Round(somaAlvo, 2),
            Math.Round(aporteHipotetico, 2),
            SemMetas: false,
            AlvoForaDeCem: alvoForaDeCem);
    }
}
