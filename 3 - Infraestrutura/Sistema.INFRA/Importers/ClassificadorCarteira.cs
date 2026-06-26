using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco) da classificação de um ativo na árvore de carteiras (F-I —
/// specs/investimentos.spec.md). Mantida separada do <see cref="FinancasImportador"/> para ser
/// testável sem DbContext, no estilo de <see cref="ExtratoB3Materializador"/> e <see cref="CriptoNetting"/>.
///
/// Estrutura alvo (topo → subcarteira-folha):
/// <list type="bullet">
/// <item>Bancário e Seguridade (flat, sem sub) → BBAS3, BBDC4, ITUB4, CXSE3</item>
/// <item>FIIs → Papel / Tijolo</item>
/// <item>Comodities e energia → Petróleo / Mineração e Metais / Energia</item>
/// <item>Criptomoedas → BTC / Altcoins / Memecoins</item>
/// </list>
///
/// Devolve o caminho (topo + sub opcional). Quando não há mapa nem fallback aplicável (ação/ETF
/// fora do mapa), devolve <c>null</c> → o ativo NÃO entra em nenhuma carteira (não inventa grupo).
/// </summary>
public static class ClassificadorCarteira
{
    // Slugs estáveis das carteiras-topo (ParentId nulo).
    public const string SlugBancario = "bancario-seguridade";
    public const string SlugFiis = "fiis";
    public const string SlugComodities = "comodities-energia";
    public const string SlugCripto = "criptomoedas";

    // Slugs estáveis das subcarteiras (folhas). Prefixados pelo slug do pai p/ unicidade global.
    public const string SlugBancos = "bancario-seguridade-bancos";
    public const string SlugSeguridade = "bancario-seguridade-seguros";
    public const string SlugFiisPapel = "fiis-papel";
    public const string SlugFiisTijolo = "fiis-tijolo";
    public const string SlugComoditiesPetroleo = "comodities-energia-petroleo";
    public const string SlugComoditiesMineracao = "comodities-energia-mineracao";
    public const string SlugComoditiesEnergia = "comodities-energia-energia";
    public const string SlugCriptoBtc = "criptomoedas-btc";
    public const string SlugCriptoAltcoins = "criptomoedas-altcoins";
    public const string SlugCriptoMemecoins = "criptomoedas-memecoins";

    /// <summary>Carteira-topo (Nome/Slug/Ordem) e suas subcarteiras (na ordem de exibição).</summary>
    public sealed record CarteiraSpec(string Nome, string Slug, int Ordem, IReadOnlyList<SubcarteiraSpec> Subs);

    public sealed record SubcarteiraSpec(string Nome, string Slug, int Ordem);

    /// <summary>Resultado da classificação: slug da topo + slug da folha (nulo quando a topo é flat).</summary>
    public sealed record Classificacao(string SlugTopo, string? SlugFolha);

    // Memecoins conhecidas (fallback p/ criptos novas) — lista do spec F-I.
    private static readonly HashSet<string> Memecoins = new(StringComparer.OrdinalIgnoreCase)
    {
        "DOGE", "SHIB", "PEPE", "FLOKI", "BONK", "WIF"
    };

    // Mapa explícito ticker → caminho na árvore (custódia B3 2026-maio + cripto §10). Editável na tela depois.
    private static readonly Dictionary<string, Classificacao> MapaPorSigla = new(StringComparer.OrdinalIgnoreCase)
    {
        // Bancário e Seguridade → Bancos / Seguridade
        ["BBAS3"] = new(SlugBancario, SlugBancos),
        ["BBDC4"] = new(SlugBancario, SlugBancos),
        ["ITUB4"] = new(SlugBancario, SlugBancos),
        ["CXSE3"] = new(SlugBancario, SlugSeguridade),
        // FIIs · Papel
        ["AFHI11"] = new(SlugFiis, SlugFiisPapel),
        ["AFHI12"] = new(SlugFiis, SlugFiisPapel),
        ["CPTS11"] = new(SlugFiis, SlugFiisPapel),
        ["DEVA11"] = new(SlugFiis, SlugFiisPapel),
        ["FYTO11"] = new(SlugFiis, SlugFiisPapel),
        ["KNSC11"] = new(SlugFiis, SlugFiisPapel),
        ["RECR11"] = new(SlugFiis, SlugFiisPapel),
        ["RECR12"] = new(SlugFiis, SlugFiisPapel),
        ["RZAK11"] = new(SlugFiis, SlugFiisPapel),
        // FIIs · Tijolo
        ["HGLG11"] = new(SlugFiis, SlugFiisTijolo),
        // Comodities e energia
        ["PETR4"] = new(SlugComodities, SlugComoditiesPetroleo),
        ["VALE3"] = new(SlugComodities, SlugComoditiesMineracao),
        ["GOLD11"] = new(SlugComodities, SlugComoditiesMineracao),
        ["TAEE4"] = new(SlugComodities, SlugComoditiesEnergia),
        // Criptomoedas
        ["BTC"] = new(SlugCripto, SlugCriptoBtc),
        ["WBETH"] = new(SlugCripto, SlugCriptoAltcoins),
        ["BNSOL"] = new(SlugCripto, SlugCriptoAltcoins),
        ["XRP"] = new(SlugCripto, SlugCriptoAltcoins),
        ["BNB"] = new(SlugCripto, SlugCriptoAltcoins),
        ["DOGE"] = new(SlugCripto, SlugCriptoMemecoins)
    };

    /// <summary>
    /// Definição da árvore-alvo (topo + subcarteiras, com nomes e ordem). A auto-sugestão usa isto
    /// para criar/garantir as carteiras idempotentemente.
    /// </summary>
    public static IReadOnlyList<CarteiraSpec> Arvore { get; } =
    [
        new("Bancário e Seguridade", SlugBancario, 10,
        [
            new("Bancos", SlugBancos, 10),
            new("Seguridade", SlugSeguridade, 20)
        ]),
        new("FIIs", SlugFiis, 20,
        [
            new("Papel", SlugFiisPapel, 10),
            new("Tijolo", SlugFiisTijolo, 20)
        ]),
        new("Minério e energia", SlugComodities, 30,
        [
            new("Petróleo", SlugComoditiesPetroleo, 10),
            new("Mineração e Metais", SlugComoditiesMineracao, 20),
            new("Energia", SlugComoditiesEnergia, 30)
        ]),
        new("Criptomoedas", SlugCripto, 40,
        [
            new("BTC", SlugCriptoBtc, 10),
            new("Altcoins", SlugCriptoAltcoins, 20),
            new("Memecoins", SlugCriptoMemecoins, 30)
        ])
    ];

    /// <summary>
    /// Classifica um ativo na árvore. Devolve o caminho (topo + folha) ou <c>null</c> quando o ativo
    /// não se encaixa (ação/ETF fora do mapa — fica sem carteira até classificação manual).
    /// </summary>
    public static Classificacao? Classificar(AtivoFinanceiro ativo)
    {
        var ticker = ExtrairTicker(ativo);
        return Classificar(ticker, ativo.Nome, ativo.Classe, ativo.EhCripto || ativo.Classe == ClasseAtivo.Cripto);
    }

    /// <summary>Sobrecarga pura (sem entidade) — facilita os testes.</summary>
    public static Classificacao? Classificar(string? ticker, string? nome, ClasseAtivo classe, bool isCripto)
    {
        var t = NormalizarTicker(ticker);

        // 1) Mapa explícito por ticker.
        if (!string.IsNullOrEmpty(t) && MapaPorSigla.TryGetValue(t, out var mapeado))
            return mapeado;

        // 2) Fallback cripto.
        if (isCripto)
        {
            var simbolo = SimboloCripto(t, nome);
            if (string.Equals(simbolo, "BTC", StringComparison.OrdinalIgnoreCase))
                return new(SlugCripto, SlugCriptoBtc);
            if (Memecoins.Contains(simbolo))
                return new(SlugCripto, SlugCriptoMemecoins);
            return new(SlugCripto, SlugCriptoAltcoins);
        }

        // 3) Fallback FII: nome com RECEBÍVEIS/CRI/SECURITIES → Papel; senão → Tijolo.
        if (classe == ClasseAtivo.FII || (!string.IsNullOrEmpty(t) && t.EndsWith("11", StringComparison.OrdinalIgnoreCase)))
        {
            var nomeUpper = (nome ?? string.Empty).ToUpperInvariant();
            var ehPapel = nomeUpper.Contains("RECEB") || nomeUpper.Contains("CRI") || nomeUpper.Contains("SECURITIES");
            return new(SlugFiis, ehPapel ? SlugFiisPapel : SlugFiisTijolo);
        }

        // 4) Ação/ETF sem mapa → sem carteira (não inventa grupo).
        return null;
    }

    private static string ExtrairTicker(AtivoFinanceiro ativo)
        => !string.IsNullOrWhiteSpace(ativo.Sigla) ? ativo.Sigla!
            : !string.IsNullOrWhiteSpace(ativo.Chave) ? ativo.Chave
            : ativo.Nome ?? string.Empty;

    private static string NormalizarTicker(string? ticker)
        => (ticker ?? string.Empty).Trim().ToUpperInvariant();

    // Símbolo cripto base (sem par/quote). Ex.: "BTCUSDT"/"BTC/BRL" → "BTC".
    private static string SimboloCripto(string ticker, string? nome)
    {
        var bruto = !string.IsNullOrEmpty(ticker) ? ticker : (nome ?? string.Empty);
        bruto = bruto.Trim().ToUpperInvariant();
        var corte = bruto.IndexOfAny(['/', '-', ' ']);
        if (corte > 0)
            bruto = bruto[..corte];
        return bruto;
    }
}
