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
        var cripto = new CriptoExteriorIrDto(
            Alienacoes: new[]
            {
                new AlienacaoCriptoIrDto(3, new DateTime(2025, 3, 10), "BTC", 0.1m, 40000m, 30000m, 10000m),
            },
            Rewards: new[]
            {
                new RewardCriptoIrDto(3, new DateTime(2025, 3, 15), "BTC", 0.001m, 300m),
            },
            GanhoCapitalLiquido: 10000m,
            Aliquota: 0.15m,
            ImpostoGanhoCapital: 1500m,
            TotalRewards: 300m,
            MesesIN1888: new[]
            {
                new MesIN1888Dto(3, 40000m, true),
            });

        var ap = new ApuracaoIrDto(
            2025,
            new[]
            {
                new ApuracaoMensalIrDto(2025, 2, "Ações", 35000m, 5000m, 0m, 5000m, 0.15m, 750m, false),
            },
            new[]
            {
                new BemDireitoIrDto("PETR4", "Acao", 100m, 3000m, "", 2500m, 90m),
                new BemDireitoIrDto("BTC", "Cripto", 0.1m, 30000m, "08-01", 0m, 0m),
            },
            new[] { new RendimentoIrDto("Dividendos", 50m) },
            new[] { new RendimentoIrDto("JCP", 100m) },
            750m,
            cripto);

        var bytes = ExcelApuracaoIr.Gerar(ap);
        using var ms = new MemoryStream(bytes);
        var doc = ExtratoConsolidadoB3Reader.Ler(ms);

        var nomes = doc.Abas.Select(a => a.Nome).ToList();
        Assert.Contains("Ganhos B3", nomes);
        Assert.Contains("Cripto", nomes);
        Assert.Contains("Bens e Direitos", nomes);
        Assert.Contains("Rendimentos isentos", nomes);
        Assert.Contains("Tributacao exclusiva (JCP)", nomes);
        Assert.Contains("IN 1888", nomes);

        var b3 = doc.Aba("Ganhos B3")!;
        Assert.Contains(b3.Linhas, l => l.Contains("Ações"));
        Assert.DoesNotContain(b3.Linhas, l => l.Contains("Cripto")); // cripto não entra nos ganhos mensais B3

        var bens = doc.Aba("Bens e Direitos")!;
        Assert.Contains(bens.Linhas, l => l.Contains("PETR4"));
        Assert.Contains(bens.Linhas, l => l.Contains("08-01")); // código RFB do BTC (grupo 08)

        var in1888 = doc.Aba("IN 1888")!;
        Assert.Contains(in1888.Linhas, l => l.Contains("Sim")); // mês de março ultrapassa R$ 30k
    }
}
