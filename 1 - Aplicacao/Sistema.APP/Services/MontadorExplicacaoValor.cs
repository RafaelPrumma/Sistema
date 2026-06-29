using System.Globalization;
using Sistema.APP.DTOs;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

// F-Q — "Explique este valor". Lógica PURA (sem DbContext, sem API) que monta as linhas de explicação
// de uma posição e do patrimônio a partir de sinais já lidos dos read models. Reusa o
// ClassificadorSaudeCotacao para derivar o estado/fonte do preço (Brapi/Binance/B3Custódia/custo +
// fallback/vencida), exatamente como a valoração e a ilha de saúde fazem — sem reimplementar a regra.
//
// NÃO recalcula transações: recebe qtd/PM/custo/valorMercado/preço já calculados pela projeção
// (FinanceiroPosicaoAtivo) e a cotação escolhida (FinanceiroCotacaoAtivo). Devolve só texto pronto.
public static class MontadorExplicacaoValor
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    private static string Money(decimal v) => v.ToString("C2", Br);
    private static string Qtd(decimal v) => v.ToString("N8", Br).TrimEnd('0').TrimEnd(',');
    private static string Pct(decimal v) => v.ToString("N2", Br) + "%";

    // Sinais de uma posição já lidos dos read models. Espelha a escolha de CriarAtivosCotadosDaTabela:
    //  PrecoUsado: o preço que valorou a posição (cotação utilizável; null = caiu no custo).
    //  TemPrecoUtilizavel: existe cotação com PrecoBRL>0 para o ativo.
    //  Provedor/StatusCotacao/Vencida: da cotação escolhida (utilizável; senão a última tentada).
    public readonly record struct EntradaPosicao(
        string Ticker,
        string Nome,
        string Classe,
        bool EhCripto,
        decimal Quantidade,
        decimal PrecoMedio,
        decimal Custo,
        decimal ValorMercado,
        decimal? PrecoUsado,
        bool TemPrecoUtilizavel,
        ProvedorCotacao Provedor,
        StatusCotacao StatusCotacao,
        bool Vencida,
        DateTime? CotacaoEm,
        string? Simbolo,
        decimal ValorAjusteReconciliacao,
        bool TemAjusteReconciliacao);

    // Monta a explicação de UMA posição. Não derruba: recebe só dados; a leitura/try-catch fica no service.
    public static ExplicacaoPosicaoDto Posicao(EntradaPosicao e)
    {
        var saude = ClassificadorSaudeCotacao.Classificar(e.TemPrecoUtilizavel, e.Provedor, e.StatusCotacao, e.Vencida);
        var valoradoPeloCusto = !e.PrecoUsado.HasValue;
        var fontePreco = valoradoPeloCusto ? "Custo" : RotuloFontePreco(e.Provedor);
        var resultado = e.ValorMercado - e.Custo;
        var resultadoPct = e.Custo == 0m ? 0m : resultado / e.Custo * 100m;

        var linhas = new List<ExplicacaoLinhaDto>
        {
            new("Quantidade", Qtd(e.Quantidade)),
            new("Preço médio (PM)", Money(e.PrecoMedio)),
        };

        if (valoradoPeloCusto)
        {
            // Fallback: sem cotação utilizável, o valor de mercado é o próprio custo (piso). O motivo
            // (sem token / falhou / sem cotação) vem do status amigável do classificador.
            linhas.Add(new("Preço usado", "— (sem cotação)", "atencao"));
            linhas.Add(new("Fonte do preço", $"Custo · {saude.Status}", "atencao"));
            linhas.Add(new("Observação", "Valorado pelo custo (preço médio) por falta de cotação utilizável.", "atencao"));
        }
        else
        {
            linhas.Add(new("Preço usado", Money(e.PrecoUsado!.Value)));
            var tom = ClassificadorSaudeCotacao.RotuloSeveridade(saude.Nivel) == "ok" ? "neutro" : "atencao";
            linhas.Add(new("Fonte do preço", $"{fontePreco} · {saude.Status}", tom));
            if (!string.IsNullOrWhiteSpace(e.Simbolo))
                linhas.Add(new("Símbolo cotado", e.Simbolo!));
            if (e.CotacaoEm.HasValue)
                linhas.Add(new("Cotação de", e.CotacaoEm.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Br)));
        }

        linhas.Add(new("Valor de mercado", Money(e.ValorMercado), "neutro"));
        linhas.Add(new("Custo investido", Money(e.Custo), "neutro"));
        linhas.Add(new(
            "Resultado (mercado − custo)",
            $"{(resultado >= 0 ? "+" : "")}{Money(resultado)} ({(resultadoPct >= 0 ? "+" : "")}{Pct(resultadoPct)})",
            resultado >= 0 ? "positivo" : "negativo"));

        if (e.TemAjusteReconciliacao)
        {
            // Ajuste da reconciliação B3 (ativo virtual VARIACAO / Fonte="Reconciliação"): a diferença
            // entre o calculado pelas transações e o alvo da custódia oficial, tornada visível.
            linhas.Add(new(
                "Ajuste de reconciliação (B3)",
                $"{(e.ValorAjusteReconciliacao >= 0 ? "+" : "")}{Money(e.ValorAjusteReconciliacao)}",
                "atencao"));
            linhas.Add(new("", "Diferença entre o calculado e a custódia oficial B3, lançada no ativo VARIAÇÃO.", "atencao"));
        }

        return new ExplicacaoPosicaoDto(
            Encontrada: true,
            Ticker: e.Ticker,
            Nome: e.Nome,
            Classe: e.Classe,
            FontePreco: fontePreco,
            FonteStatus: saude.Status,
            FonteSeveridade: ClassificadorSaudeCotacao.RotuloSeveridade(saude.Nivel),
            ValoradoPeloCusto: valoradoPeloCusto,
            ValorMercado: Math.Round(e.ValorMercado, 2),
            Custo: Math.Round(e.Custo, 2),
            Resultado: Math.Round(resultado, 2),
            ResultadoPercentual: Math.Round(resultadoPct, 2),
            TemAjusteReconciliacao: e.TemAjusteReconciliacao,
            ValorAjusteReconciliacao: Math.Round(e.ValorAjusteReconciliacao, 2),
            BuscaTransacoes: string.IsNullOrWhiteSpace(e.Ticker) ? null : e.Ticker,
            Linhas: linhas);
    }

    public static ExplicacaoPosicaoDto PosicaoNaoEncontrada()
        => new(false, "—", string.Empty, string.Empty, "—", "—", "atencao", false, 0m, 0m, 0m, 0m, false, 0m, null, []);

    // Sinais do patrimônio já agregados pelo service (mesma composição do F-L + reconciliação do F-M).
    public readonly record struct EntradaPatrimonio(
        decimal Total,
        decimal ComCotacao,
        decimal ComFechamentoB3,
        decimal ComCusto,
        int QtdAtivos,
        int QtdComCotacao,
        int QtdComFechamentoB3,
        int QtdComCusto,
        bool TemReconciliacao,
        decimal ValorReconciliacao,
        int QtdAjustesReconciliacao);

    // Monta a explicação do card de Patrimônio. Reusa números que o dashboard JÁ calcula — sem cálculo
    // paralelo divergente. As linhas decompõem o total por fonte do preço + a reconciliação.
    public static ExplicacaoPatrimonioDto Patrimonio(EntradaPatrimonio e)
    {
        string Parte(decimal valor) => e.Total == 0m ? "0,00%" : Pct(valor / e.Total * 100m);

        var linhas = new List<ExplicacaoLinhaDto>
        {
            new("Patrimônio total", Money(e.Total), "neutro"),
            new($"Cotação ao vivo ({e.QtdComCotacao} ativo(s))", $"{Money(e.ComCotacao)} · {Parte(e.ComCotacao)}", "positivo"),
            new($"Fechamento B3 ({e.QtdComFechamentoB3} ativo(s))", $"{Money(e.ComFechamentoB3)} · {Parte(e.ComFechamentoB3)}", "neutro"),
            new($"Custo / fallback ({e.QtdComCusto} ativo(s))", $"{Money(e.ComCusto)} · {Parte(e.ComCusto)}", e.ComCusto > 0m ? "atencao" : "neutro"),
        };

        if (e.QtdComCusto > 0)
            linhas.Add(new("", "Ativos sem cotação utilizável entram pelo custo (preço médio) — valor é piso, não preço de mercado.", "atencao"));

        if (e.TemReconciliacao)
        {
            linhas.Add(new(
                $"Reconciliação B3 ({e.QtdAjustesReconciliacao} ajuste(s))",
                $"{(e.ValorReconciliacao >= 0 ? "+" : "")}{Money(e.ValorReconciliacao)}",
                "atencao"));
            linhas.Add(new("", "Diferença não explicada pelos relatórios, lançada no ativo virtual VARIAÇÃO (custódia oficial B3).", "atencao"));
        }

        return new ExplicacaoPatrimonioDto(
            TemDados: e.QtdAtivos > 0,
            Total: Math.Round(e.Total, 2),
            ComCotacao: Math.Round(e.ComCotacao, 2),
            ComFechamentoB3: Math.Round(e.ComFechamentoB3, 2),
            ComCusto: Math.Round(e.ComCusto, 2),
            QtdAtivos: e.QtdAtivos,
            QtdComCotacao: e.QtdComCotacao,
            QtdComFechamentoB3: e.QtdComFechamentoB3,
            QtdComCusto: e.QtdComCusto,
            TemReconciliacao: e.TemReconciliacao,
            ValorReconciliacao: Math.Round(e.ValorReconciliacao, 2),
            QtdAjustesReconciliacao: e.QtdAjustesReconciliacao,
            Linhas: linhas);
    }

    public static ExplicacaoPatrimonioDto PatrimonioVazio()
        => new(false, 0m, 0m, 0m, 0m, 0, 0, 0, 0, false, 0m, 0, []);

    // Mesmo rótulo de fonte do FinancasAppService.RotuloFontePreco (mantido aqui para a lógica ser pura).
    private static string RotuloFontePreco(ProvedorCotacao provedor) => provedor switch
    {
        ProvedorCotacao.Brapi => "Cotação (Brapi)",
        ProvedorCotacao.Binance => "Cotação (Binance)",
        ProvedorCotacao.B3Custodia => "Fechamento B3",
        ProvedorCotacao.Manual => "Cotação (manual)",
        _ => "Cotação"
    };
}
