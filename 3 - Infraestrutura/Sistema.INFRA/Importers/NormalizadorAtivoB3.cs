using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

public static class NormalizadorAtivoB3
{
    private static readonly HashSet<string> Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ON", "PN", "PNA", "PNB", "PNC", "PND", "UNT", "UNITS", "DR1", "DR2", "DR3", "DRN", "CI"
    };

    private static readonly Dictionary<string, AtivoB3Alias> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AES BRASIL ON"] = new("AESB3", ClasseAtivo.Acao),
        ["ALPHABET DRN"] = new("GOGL34", ClasseAtivo.BDR),
        ["AMBEV S/A ON"] = new("ABEV3", ClasseAtivo.Acao),
        ["AMERICANAS ON"] = new("AMER3", ClasseAtivo.Acao),
        ["B3 ON"] = new("B3SA3", ClasseAtivo.Acao),
        ["BANRISUL ON"] = new("BRSR3", ClasseAtivo.Acao),
        ["BANRISUL PNB"] = new("BRSR6", ClasseAtivo.Acao),
        ["BBSEGURIDADE ON"] = new("BBSE3", ClasseAtivo.Acao),
        ["BRADESCO ON"] = new("BBDC3", ClasseAtivo.Acao),
        ["BRADESCO PN"] = new("BBDC4", ClasseAtivo.Acao),
        ["BRASIL ON"] = new("BBAS3", ClasseAtivo.Acao),
        ["BRASILAGRO ON"] = new("AGRO3", ClasseAtivo.Acao),
        ["BTGP BANCO UNT"] = new("BPAC11", ClasseAtivo.Acao),
        ["CAIXA SEGURI ON"] = new("CXSE3", ClasseAtivo.Acao),
        ["CAMIL ON"] = new("CAML3", ClasseAtivo.Acao),
        ["CEMIG ON"] = new("CMIG3", ClasseAtivo.Acao),
        ["CEMIG PN"] = new("CMIG4", ClasseAtivo.Acao),
        ["ENGIE BRASIL ON"] = new("EGIE3", ClasseAtivo.Acao),
        ["IRBBRASIL RE ON"] = new("IRBR3", ClasseAtivo.Acao),
        ["ITAUSA ON"] = new("ITSA3", ClasseAtivo.Acao),
        ["ITAUSA PN"] = new("ITSA4", ClasseAtivo.Acao),
        ["ITAUUNIBANCO ON"] = new("ITUB3", ClasseAtivo.Acao),
        ["ITAUUNIBANCO PN"] = new("ITUB4", ClasseAtivo.Acao),
        ["KEPLER WEBER ON"] = new("KEPL3", ClasseAtivo.Acao),
        ["KLABIN S/A ON"] = new("KLBN3", ClasseAtivo.Acao),
        ["LOJAS AMERIC PN"] = new("LAME4", ClasseAtivo.Acao),
        ["MAGAZ LUIZA ON"] = new("MGLU3", ClasseAtivo.Acao),
        ["MELIUZ ON"] = new("CASH3", ClasseAtivo.Acao),
        ["MERCADOLIBRE DRN"] = new("MELI34", ClasseAtivo.BDR),
        ["META PLAT DRN"] = new("M1TA34", ClasseAtivo.BDR),
        ["NU HOLDINGS DRN"] = new("ROXO34", ClasseAtivo.BDR),
        ["OI ON"] = new("OIBR3", ClasseAtivo.Acao),
        ["PETROBRAS ON"] = new("PETR3", ClasseAtivo.Acao),
        ["PETROBRAS PN"] = new("PETR4", ClasseAtivo.Acao),
        ["PETRORIO ON"] = new("PRIO3", ClasseAtivo.Acao),
        ["PORTO SEGURO ON"] = new("PSSA3", ClasseAtivo.Acao),
        ["SANEPAR ON"] = new("SAPR3", ClasseAtivo.Acao),
        ["SANTANDER BR ON"] = new("SANB3", ClasseAtivo.Acao),
        ["SUZANO S.A. ON"] = new("SUZB3", ClasseAtivo.Acao),
        ["TAESA ON"] = new("TAEE3", ClasseAtivo.Acao),
        ["TAESA PN"] = new("TAEE4", ClasseAtivo.Acao),
        ["TELEF BRASIL ON"] = new("VIVT3", ClasseAtivo.Acao),
        ["TIM ON"] = new("TIMS3", ClasseAtivo.Acao),
        ["TOTVS ON"] = new("TOTS3", ClasseAtivo.Acao),
        ["VALE ON"] = new("VALE3", ClasseAtivo.Acao),

        ["TREND OURO CI"] = new("GOLD11", ClasseAtivo.ETF),
        ["QR BITCOIN CI"] = new("QBTC11", ClasseAtivo.ETF),
        ["QR ETHER CI"] = new("QETH11", ClasseAtivo.ETF),

        ["FII BC FFII CI"] = new("BCFF11", ClasseAtivo.FII),
        ["FII BCFFII CI"] = new("BCFF11", ClasseAtivo.FII),
        ["FII CAP REIT CI"] = new("CPTS11", ClasseAtivo.FII),
        ["FII CENESP CI"] = new("CNES11", ClasseAtivo.FII),
        ["FII CSHG LOG CI"] = new("HGLG11", ClasseAtivo.FII),
        ["FII CSHG REAL CI"] = new("HGRE11", ClasseAtivo.FII),
        ["FII CSHG URB CI"] = new("HGRU11", ClasseAtivo.FII),
        ["FII DEVANT CI"] = new("DEVA11", ClasseAtivo.FII),
        ["FII FYTO CI"] = new("FYTO11", ClasseAtivo.FII),
        ["FII HEDGEBS CI"] = new("HGBS11", ClasseAtivo.FII),
        ["FII HECTARE CI"] = new("HCTR11", ClasseAtivo.FII),
        ["FII HSI MALL CI"] = new("HSML11", ClasseAtivo.FII),
        ["FII IRIDIUM CI"] = new("IRDM11", ClasseAtivo.FII),
        ["FII IRIM CI"] = new("IRIM11", ClasseAtivo.FII),
        ["FII KINEA IP CI"] = new("KNIP11", ClasseAtivo.FII),
        ["FII KINEA RI CI"] = new("KNCR11", ClasseAtivo.FII),
        ["FII KINEA SC CI"] = new("KNSC11", ClasseAtivo.FII),
        ["FII MAXI REN CI"] = new("MXRF11", ClasseAtivo.FII),
        ["FII MAUA CI"] = new("MCCI11", ClasseAtivo.FII),
        ["FII RBR PCRI CI"] = new("RBRY11", ClasseAtivo.FII),
        ["FII RBRALPHA CI"] = new("RBRF11", ClasseAtivo.FII),
        ["FII RBRHGRAD CI"] = new("RBRR11", ClasseAtivo.FII),
        ["FII REC RECEB CI"] = new("RECR11", ClasseAtivo.FII),
        ["FII REC REND CI"] = new("RECT11", ClasseAtivo.FII),
        ["FII RIZA TX CI"] = new("RZTR11", ClasseAtivo.FII),
        ["FII TORDESIL CI"] = new("TORD11", ClasseAtivo.FII),
        ["FII VALORA H CI"] = new("VGHF11", ClasseAtivo.FII),
        ["FII VALORAIP CI"] = new("VGIR11", ClasseAtivo.FII),
        ["FII VINCI LG CI"] = new("VILG11", ClasseAtivo.FII),
        ["FII VINCI OF CI"] = new("VINO11", ClasseAtivo.FII),
        ["FII VINCI SC CI"] = new("VISC11", ClasseAtivo.FII),
        ["FII XP LOG CI"] = new("XPLG11", ClasseAtivo.FII),
        ["FII XP MALLS CI"] = new("XPML11", ClasseAtivo.FII),

        // Chaves exatas vindas das notas/corretora deste portfólio (corrigem nomes que não batiam).
        ["FII AFHI CRI CI"] = new("AFHI11", ClasseAtivo.FII),
        ["FII BC FUND CI"] = new("BRCR11", ClasseAtivo.FII),
        ["FII CAPI SEC CI"] = new("CPTS11", ClasseAtivo.FII),
        ["FII GGRCOVEP CI"] = new("GGRC11", ClasseAtivo.FII),
        ["FII REC RECE CI"] = new("RECR11", ClasseAtivo.FII),
        ["FII RIZA AKN CI"] = new("RZAK11", ClasseAtivo.FII),
        ["FII SP DOWNT CI"] = new("SPTW11", ClasseAtivo.FII),
        ["FII TORDE EI CI"] = new("TORD11", ClasseAtivo.FII),
        ["FII VALOR HE CI"] = new("VGHF11", ClasseAtivo.FII),
        ["FII ARCTIUM CI"] = new("RZAT11", ClasseAtivo.FII),
        ["FII HGLG PAX CI"] = new("HGLG11", ClasseAtivo.FII),
        ["FII NCH BR CI"] = new("NCHB11", ClasseAtivo.FII),
        ["FII NCH EQI CI"] = new("EQIN11", ClasseAtivo.FII),
        ["NU-NUBANK DR3"] = new("ROXO34", ClasseAtivo.BDR)
    };

    public static IReadOnlyDictionary<string, AtivoB3Alias> AliasesConhecidos => Aliases;

    public static string ChaveCanonica(string? especificacao)
    {
        var s = (especificacao ?? string.Empty).Trim();
        if (s.Length == 0)
            return "SEM_ATIVO";

        var toks = s.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < toks.Length; i++)
            if (Classes.Contains(toks[i]))
                return string.Join(' ', toks.Take(i + 1));

        return s.ToUpperInvariant();
    }

    public static string? Ticker(string? especificacao)
        => Normalizar(especificacao)?.Ticker;

    public static AtivoB3Alias? Normalizar(string? especificacao)
    {
        var chave = ChaveCanonica(especificacao);
        if (Aliases.TryGetValue(chave, out var alias))
            return alias;

        var match = System.Text.RegularExpressions.Regex.Match(chave, @"\b[A-Z]{4}11\b");
        if (match.Success)
            return new AtivoB3Alias(match.Value, chave.StartsWith("FII ", StringComparison.OrdinalIgnoreCase) ? ClasseAtivo.FII : ClasseAtivo.ETF);

        match = System.Text.RegularExpressions.Regex.Match(chave, @"\b[A-Z]{4}\d{1,2}\b");
        return match.Success ? new AtivoB3Alias(match.Value, ClasseAtivo.Acao) : null;
    }
}

public sealed record AtivoB3Alias(string Ticker, ClasseAtivo Classe);
