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
            BensEDireitos(ap.BensEDireitos),
            Rendimentos("Rendimentos isentos", ap.RendimentosIsentos),
            Rendimentos("Tributacao exclusiva (JCP)", ap.TributacaoExclusiva),
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

    private static AbaXlsx BensEDireitos(IReadOnlyList<BemDireitoIrDto> bens)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Ticker", "Classe", "Quantidade", "Custo (R$)" }
        };
        foreach (var b in bens)
            rows.Add(new object?[] { b.Ticker, b.Classe, b.Quantidade, b.Custo });
        return new AbaXlsx("Bens e Direitos", rows);
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
