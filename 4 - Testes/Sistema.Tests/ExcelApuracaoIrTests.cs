using Sistema.APP.DTOs;
using Sistema.APP.Services;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

/// <summary>
/// Testa o escritor de .xlsx (EscritorXlsx) e o mapeamento da apuração de IR (ExcelApuracaoIr),
/// validando por round-trip: escreve e lê de volta com o ExtratoConsolidadoB3Reader.
/// </summary>
public class ExcelApuracaoIrTests
{
    [Fact]
    public void EscritorXlsx_RoundTrip_LeDeVoltaAbasECelulas()
    {
        var abas = new List<AbaXlsx>
        {
            new("Resumo", new List<IReadOnlyList<object?>>
            {
                new object?[] { "Tipo", "Valor" },
                new object?[] { "Dividendos", 1500.5m },
            }),
            new("Outra", new List<IReadOnlyList<object?>>
            {
                new object?[] { "X", 10 },
            }),
        };

        var bytes = EscritorXlsx.Gerar(abas);
        using var ms = new MemoryStream(bytes);
        var doc = ExtratoConsolidadoB3Reader.Ler(ms);

        Assert.Equal(new[] { "Resumo", "Outra" }, doc.Abas.Select(a => a.Nome).ToArray());
        var resumo = doc.Aba("Resumo")!;
        Assert.Equal("Tipo", resumo.Linhas[0][0]);
        Assert.Equal("Dividendos", resumo.Linhas[1][0]);
        Assert.Equal("1500.5", resumo.Linhas[1][1]); // numérico volta como string invariante
    }

    [Fact]
    public void ExcelApuracaoIr_GeraUmaAbaPorBloco_SeparaB3DeCripto()
    {
        var ap = new ApuracaoIrDto(
            2025,
            new[]
            {
                new ApuracaoMensalIrDto(2025, 2, "Ações", 35000m, 5000m, 0m, 5000m, 0.15m, 750m, false),
                new ApuracaoMensalIrDto(2025, 3, "Cripto", 40000m, 30000m, 0m, 30000m, 0.15m, 4500m, false),
            },
            new[] { new BemDireitoIrDto("PETR4", "Acao", 100m, 3000m) },
            new[] { new RendimentoIrDto("Dividendos", 50m) },
            new[] { new RendimentoIrDto("JCP", 100m) },
            5250m);

        var bytes = ExcelApuracaoIr.Gerar(ap);
        using var ms = new MemoryStream(bytes);
        var doc = ExtratoConsolidadoB3Reader.Ler(ms);

        var nomes = doc.Abas.Select(a => a.Nome).ToList();
        Assert.Contains("Ganhos B3", nomes);
        Assert.Contains("Cripto", nomes);
        Assert.Contains("Bens e Direitos", nomes);

        var b3 = doc.Aba("Ganhos B3")!;
        Assert.Contains(b3.Linhas, l => l.Contains("Ações"));
        Assert.DoesNotContain(b3.Linhas, l => l.Contains("Cripto")); // cripto vai para a aba própria
    }
}
