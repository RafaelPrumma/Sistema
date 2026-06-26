using Sistema.APP.DTOs;

namespace Sistema.APP.Services;

/// <summary>
/// Monta o .xlsx da apuração de IR ("cola"), uma aba por bloco. Puro/testável.
/// Apoio — NÃO substitui contador (ver specs/ir.spec.md).
/// </summary>
public static class ExcelApuracaoIr
{
    public static byte[] Gerar(ApuracaoIrDto ap)
    {
        ArgumentNullException.ThrowIfNull(ap);
        var abas = new List<AbaXlsx>
        {
            GanhosCapital("Ganhos B3", ap.GanhosMensais.Where(g => g.Natureza != "Cripto")),
            GanhosCapital("Cripto", ap.GanhosMensais.Where(g => g.Natureza == "Cripto")),
            BensEDireitos(ap.Ano, ap.BensEDireitos),
            Rendimentos("Rendimentos isentos", ap.RendimentosIsentos),
            Rendimentos("Tributacao exclusiva (JCP)", ap.TributacaoExclusiva),
            IN1888(ap.CriptoExterior.MesesIN1888),
        };
        return EscritorXlsx.Gerar(abas);
    }

    private static AbaXlsx GanhosCapital(string nome, IEnumerable<ApuracaoMensalIrDto> linhas)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Ano", "Mês", "Natureza", "Total vendas", "Resultado", "Prejuízo compensado", "Base de cálculo", "Alíquota", "Imposto (DARF)", "Isento" }
        };
        foreach (var g in linhas.OrderBy(g => g.Mes).ThenBy(g => g.Natureza))
        {
            rows.Add(new object?[]
            {
                g.Ano, g.Mes, g.Natureza, g.TotalVendas, g.Resultado, g.PrejuizoCompensado,
                g.BaseCalculo, g.Aliquota, g.Imposto, g.Isento ? "Sim" : "Não"
            });
        }
        return new AbaXlsx(nome, rows);
    }

    private static AbaXlsx BensEDireitos(int ano, IReadOnlyList<BemDireitoIrDto> bens)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[]
            {
                "Ticker", "Classe", "Codigo RFB",
                $"Qtd 31/12/{ano - 1}", $"Custo 31/12/{ano - 1} (R$)",
                $"Qtd 31/12/{ano}", $"Custo 31/12/{ano} (R$)"
            }
        };
        foreach (var b in bens)
            rows.Add(new object?[]
            {
                b.Ticker, b.Classe, b.Codigo,
                b.QuantidadeAnterior, b.CustoAnterior,
                b.Quantidade, b.Custo
            });
        return new AbaXlsx("Bens e Direitos", rows);
    }

    private static AbaXlsx IN1888(IReadOnlyList<MesIN1888Dto> meses)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Mes", "Total alienacoes cripto (R$)", "Passou de R$30k (IN 1888)?" }
        };
        foreach (var m in meses)
            rows.Add(new object?[] { m.Mes, m.TotalAlienacoes, m.UltrapassaLimite ? "Sim" : "Nao" });
        return new AbaXlsx("IN 1888", rows);
    }

    private static AbaXlsx Rendimentos(string nome, IReadOnlyList<RendimentoIrDto> itens)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Tipo", "Valor (R$)" }
        };
        foreach (var r in itens)
            rows.Add(new object?[] { r.Tipo, r.Valor });
        return new AbaXlsx(nome, rows);
    }
}
