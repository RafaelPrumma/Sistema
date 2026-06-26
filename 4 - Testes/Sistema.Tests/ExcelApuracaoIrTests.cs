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
    public void ExcelApuracaoIr_EspelhaAsAbasDoConsolidado_ComLinhasChave()
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
        Assert.Equal(
            new[] { "Resumo", "Como_Usar", "Bens_Direitos", "Aplic_Fin_Exterior", "Operacoes_Ganho", "Rendimentos_Rewards", "Resumo_Mensal", "Regras_Fontes" },
            nomes.ToArray());

        // Resumo: imposto total = DARF B3 (750) + cripto exterior (1500) = 2250.
        var resumo = doc.Aba("Resumo")!;
        Assert.Contains(resumo.Linhas, l => l.Contains("Imposto total estimado") && l.Contains("2250"));
        Assert.Contains(resumo.Linhas, l => l.Contains("Março")); // mês que passou de R$30k (IN 1888)

        // Aplic_Fin_Exterior: alíquota 15% e imposto sobre ganho de capital cripto.
        var exterior = doc.Aba("Aplic_Fin_Exterior")!;
        Assert.Contains(exterior.Linhas, l => l.Contains("0.15"));
        Assert.Contains(exterior.Linhas, l => l.Contains("Imposto sobre ganho de capital (R$)") && l.Contains("1500"));

        // Operacoes_Ganho: a alienação de BTC com ganho de 10000.
        var operacoes = doc.Aba("Operacoes_Ganho")!;
        Assert.Contains(operacoes.Linhas, l => l.Contains("BTC") && l.Contains("10000") && l.Contains("10/03/2025"));

        // Rendimentos_Rewards: o reward de 300 BRL.
        var rewards = doc.Aba("Rendimentos_Rewards")!;
        Assert.Contains(rewards.Linhas, l => l.Contains("BTC") && l.Contains("300"));

        // Bens_Direitos: PETR4 (B3) e o código RFB 08-01 do BTC; cripto com custo 30k é obrigatório.
        var bens = doc.Aba("Bens_Direitos")!;
        Assert.Contains(bens.Linhas, l => l.Contains("PETR4"));
        Assert.Contains(bens.Linhas, l => l.Contains("08-01") && l.Contains("Sim"));

        // Resumo_Mensal: ganho B3 de Ações (fev) + alienação de cripto de março com flag IN 1888.
        var mensal = doc.Aba("Resumo_Mensal")!;
        Assert.Contains(mensal.Linhas, l => l.Contains("Ações") && l.Contains("Fevereiro"));
        Assert.Contains(mensal.Linhas, l => l.Contains("Março") && l.Contains("Sim"));

        // Abas de texto presentes.
        Assert.True(doc.Aba("Como_Usar")!.Linhas.Count > 1);
        Assert.Contains(doc.Aba("Regras_Fontes")!.Linhas, l => l.Contains("Lei 14.754/2023"));
    }
}
