using System.Text.Json;
using Sistema.CORE.Entities;
using Sistema.INFRA.Services;

namespace Sistema.Tests;

/// <summary>
/// Testa o mapeamento de dividendsData.stockDividends da Brapi → EventoCorporativo e o dedup por janela.
/// </summary>
public class EventoCorporativoBrapiParserTests
{
    private static JsonElement Ev(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Mapear_Desdobramento_FatorDiretoEDataExAposCorte()
    {
        var r = EventoCorporativoBrapiParser.Mapear(
            Ev("""{"label":"DESDOBRAMENTO","factor":10,"lastDatePrior":"2023-09-25T00:00:00.000Z"}"""));

        Assert.NotNull(r);
        Assert.Equal(TipoEventoCorporativo.Desdobramento, r!.Tipo);
        Assert.Equal(10m, r.Fator);
        Assert.Equal(new DateTime(2023, 9, 26), r.Data); // corte 25 + 1 = data-ex 26
    }

    [Fact]
    public void Mapear_Grupamento_FatorInvertido()
    {
        var r = EventoCorporativoBrapiParser.Mapear(
            Ev("""{"label":"GRUPAMENTO","factor":10,"lastDatePrior":"2024-01-10T00:00:00.000Z"}"""));

        Assert.NotNull(r);
        Assert.Equal(TipoEventoCorporativo.Grupamento, r!.Tipo);
        Assert.Equal(0.1m, r.Fator); // grupamento 10:1 → 0,1
    }

    [Theory]
    [InlineData("""{"label":"BONIFICACAO","factor":10,"lastDatePrior":"2024-01-10"}""")] // ambíguo → não auto-insere
    [InlineData("""{"label":"DESDOBRAMENTO","lastDatePrior":"2024-01-10"}""")]            // sem factor
    [InlineData("""{"label":"DESDOBRAMENTO","factor":10}""")]                             // sem data
    public void Mapear_SemDadoUtilizavel_RetornaNull(string json)
        => Assert.Null(EventoCorporativoBrapiParser.Mapear(Ev(json)));

    [Fact]
    public void JaCoberto_JanelaCobreDiferencaDeConvencaoDeData()
    {
        var existentes = new[] { (AssetId: 1, Data: new DateTime(2023, 11, 6)) };

        // Brapi traria ex ~04/11 (corte 03 + 1); a janela de 7 dias casa com o seed 06/11 → não duplica.
        Assert.True(EventoCorporativoBrapiParser.JaCoberto(1, new DateTime(2023, 11, 4), existentes));
        Assert.False(EventoCorporativoBrapiParser.JaCoberto(1, new DateTime(2023, 12, 1), existentes)); // fora da janela
        Assert.False(EventoCorporativoBrapiParser.JaCoberto(2, new DateTime(2023, 11, 4), existentes)); // outro ativo
    }
}
