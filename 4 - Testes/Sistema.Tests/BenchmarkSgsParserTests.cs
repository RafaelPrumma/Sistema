using Sistema.APP.Services;

namespace Sistema.Tests;

/// <summary>
/// Testa o parser PURO da resposta do BCB SGS (data dd/MM/yyyy + valor com vírgula OU ponto).
/// </summary>
public class BenchmarkSgsParserTests
{
    [Fact]
    public void Parse_ValorComVirgula_PtBr()
    {
        var p = BenchmarkSgsParser.Parse("02/01/2025", "0,043739");
        Assert.NotNull(p);
        Assert.Equal(new DateTime(2025, 1, 2), p!.Value.Data);
        Assert.Equal(0.043739m, p.Value.Valor, 6);
    }

    [Fact]
    public void Parse_ValorComPonto_EnUs()
    {
        var p = BenchmarkSgsParser.Parse("15/03/2024", "0.5");
        Assert.NotNull(p);
        Assert.Equal(new DateTime(2024, 3, 15), p!.Value.Data);
        Assert.Equal(0.5m, p.Value.Valor, 6);
    }

    [Fact]
    public void Parse_DataInvalida_RetornaNull()
        => Assert.Null(BenchmarkSgsParser.Parse("2025-01-02", "0,04")); // formato ISO não é aceito pelo SGS

    [Fact]
    public void Parse_ValorVazio_RetornaNull()
        => Assert.Null(BenchmarkSgsParser.Parse("02/01/2025", ""));

    [Fact]
    public void ParseMuitos_DescartaMalformados()
    {
        var itens = new (string?, string?)[]
        {
            ("02/01/2025", "0,04"),
            ("lixo", "0,04"),       // data inválida → descartada
            ("03/01/2025", null),    // valor nulo → descartado
            ("06/01/2025", "0.05"),
        };

        var pontos = BenchmarkSgsParser.ParseMuitos(itens);
        Assert.Equal(2, pontos.Count);
        Assert.Equal(0.04m, pontos[0].Valor, 6);
        Assert.Equal(0.05m, pontos[1].Valor, 6);
    }
}
