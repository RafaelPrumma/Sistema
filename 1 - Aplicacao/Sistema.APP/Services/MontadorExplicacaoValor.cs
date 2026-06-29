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

    // F-Q (2ª fatia) — sinais de UMA carteira já agregados pelo service (mesmos números da ilha de
    // Carteiras: composição por fonte do preço + peso atual/alvo + reconciliação que recaiu nos ativos).
    public readonly record struct EntradaCarteira(
        string Nome,
        string Tipo,
        decimal ValorMercado,
        decimal ComCotacao,
        decimal ComFechamentoB3,
        decimal ComCusto,
        int QtdAtivos,
        int QtdComCotacao,
        int QtdComFechamentoB3,
        int QtdComCusto,
        decimal PesoAtual,
        decimal? PesoAlvo,
        bool TemAjusteReconciliacao,
        decimal ValorAjusteReconciliacao);

    // Monta a explicação de UMA carteira: composição do valor por fonte do preço, peso atual vs alvo e
    // a parcela de reconciliação. Reusa os números da ilha (sem cálculo paralelo). À prova de falha: o
    // service trata try-catch e decide encontrada/não encontrada.
    public static ExplicacaoCarteiraDto Carteira(EntradaCarteira e)
    {
        string Parte(decimal valor) => e.ValorMercado == 0m ? "0,00%" : Pct(valor / e.ValorMercado * 100m);

        var linhas = new List<ExplicacaoLinhaDto>
        {
            new("Valor da carteira", Money(e.ValorMercado), "neutro"),
            new($"Cotação ao vivo ({e.QtdComCotacao} ativo(s))", $"{Money(e.ComCotacao)} · {Parte(e.ComCotacao)}", "positivo"),
            new($"Fechamento B3 ({e.QtdComFechamentoB3} ativo(s))", $"{Money(e.ComFechamentoB3)} · {Parte(e.ComFechamentoB3)}", "neutro"),
            new($"Custo / fallback ({e.QtdComCusto} ativo(s))", $"{Money(e.ComCusto)} · {Parte(e.ComCusto)}", e.ComCusto > 0m ? "atencao" : "neutro"),
        };

        if (e.QtdComCusto > 0)
            linhas.Add(new("", "Ativos sem cotação utilizável entram pelo custo (preço médio) — valor é piso, não preço de mercado.", "atencao"));

        // Peso atual no patrimônio vs peso-alvo (PesoAlvo). O desvio (p.p.) ajuda a decidir aporte.
        linhas.Add(new("Peso atual (no patrimônio)", Pct(e.PesoAtual), "neutro"));
        if (e.PesoAlvo.HasValue)
        {
            var desvio = e.PesoAtual - e.PesoAlvo.Value;
            linhas.Add(new("Peso-alvo", Pct(e.PesoAlvo.Value), "neutro"));
            linhas.Add(new(
                "Desvio (atual − alvo)",
                $"{(desvio >= 0 ? "+" : "")}{Pct(desvio)} p.p.",
                Math.Abs(desvio) < 0.01m ? "neutro" : "atencao"));
        }
        else
        {
            linhas.Add(new("Peso-alvo", "— (não definido)", "neutro"));
        }

        if (e.TemAjusteReconciliacao)
        {
            linhas.Add(new(
                "Ajuste de reconciliação (B3)",
                $"{(e.ValorAjusteReconciliacao >= 0 ? "+" : "")}{Money(e.ValorAjusteReconciliacao)}",
                "atencao"));
            linhas.Add(new("", "Diferença entre o calculado e a custódia oficial B3 que recaiu sobre ativos desta carteira.", "atencao"));
        }

        return new ExplicacaoCarteiraDto(
            Encontrada: true,
            Nome: e.Nome,
            Tipo: e.Tipo,
            ValorMercado: Math.Round(e.ValorMercado, 2),
            ComCotacao: Math.Round(e.ComCotacao, 2),
            ComFechamentoB3: Math.Round(e.ComFechamentoB3, 2),
            ComCusto: Math.Round(e.ComCusto, 2),
            QtdAtivos: e.QtdAtivos,
            QtdComCotacao: e.QtdComCotacao,
            QtdComFechamentoB3: e.QtdComFechamentoB3,
            QtdComCusto: e.QtdComCusto,
            PesoAtual: Math.Round(e.PesoAtual, 2),
            PesoAlvo: e.PesoAlvo.HasValue ? Math.Round(e.PesoAlvo.Value, 2) : null,
            TemAjusteReconciliacao: e.TemAjusteReconciliacao,
            ValorAjusteReconciliacao: Math.Round(e.ValorAjusteReconciliacao, 2),
            Linhas: linhas);
    }

    public static ExplicacaoCarteiraDto CarteiraNaoEncontrada()
        => new(false, "—", string.Empty, 0m, 0m, 0m, 0m, 0, 0, 0, 0, 0m, null, false, 0m, []);

    // F-Q (2ª fatia) — sinais do card de Proventos já agregados pelo service. As quebras por fonte/tipo
    // vêm prontas (o service reusa RotuloFonteProvento/RotuloTipoProvento), cada uma como (rótulo, valor,
    // contagem); o montador só formata as linhas e adiciona a nota de precedência.
    public readonly record struct GrupoProvento(string Rotulo, decimal Valor, int Quantidade);

    public readonly record struct EntradaProventos(
        decimal TotalRecebido,
        int Quantidade,
        DateTime? PeriodoInicio,
        DateTime? PeriodoFim,
        IReadOnlyList<GrupoProvento> PorFonte,
        IReadOnlyList<GrupoProvento> PorTipo);

    public static ExplicacaoProventosDto Proventos(EntradaProventos e)
    {
        string Parte(decimal valor) => e.TotalRecebido == 0m ? "0,00%" : Pct(valor / e.TotalRecebido * 100m);
        string Data(DateTime d) => d.ToString("dd/MM/yyyy", Br);

        var linhas = new List<ExplicacaoLinhaDto>
        {
            new("Recebido (12 meses)", Money(e.TotalRecebido), "positivo"),
            new("Lançamentos", e.Quantidade.ToString("N0", Br), "neutro"),
        };

        if (e.PeriodoInicio.HasValue && e.PeriodoFim.HasValue)
            linhas.Add(new("Período coberto", $"{Data(e.PeriodoInicio.Value)} a {Data(e.PeriodoFim.Value)}", "neutro"));

        if (e.PorFonte.Count > 0)
        {
            linhas.Add(new("Por fonte do dado", "", "neutro"));
            foreach (var f in e.PorFonte)
                linhas.Add(new($"  {f.Rotulo} ({f.Quantidade} lançamento(s))", $"{Money(f.Valor)} · {Parte(f.Valor)}", "neutro"));
        }

        if (e.PorTipo.Count > 0)
        {
            linhas.Add(new("Por tipo", "", "neutro"));
            foreach (var t in e.PorTipo)
                linhas.Add(new($"  {t.Rotulo} ({t.Quantidade} lançamento(s))", $"{Money(t.Valor)} · {Parte(t.Valor)}", "neutro"));
        }

        // Nota de confiança/precedência: a B3 é a fonte primária; Brapi só complementa onde a B3 não
        // cobre. FII vem da B3 porque o informe de IR só cobre ações.
        linhas.Add(new("", "Precedência: a B3 manda quando há extrato; Brapi entra só como complemento. FII vem da B3 (o informe de IR só cobre ações).", "atencao"));

        return new ExplicacaoProventosDto(
            TemDados: e.Quantidade > 0,
            TotalRecebido: Math.Round(e.TotalRecebido, 2),
            Quantidade: e.Quantidade,
            PeriodoInicio: e.PeriodoInicio.HasValue ? Data(e.PeriodoInicio.Value) : null,
            PeriodoFim: e.PeriodoFim.HasValue ? Data(e.PeriodoFim.Value) : null,
            Linhas: linhas);
    }

    public static ExplicacaoProventosDto ProventosVazio()
        => new(false, 0m, 0, null, null, []);

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
