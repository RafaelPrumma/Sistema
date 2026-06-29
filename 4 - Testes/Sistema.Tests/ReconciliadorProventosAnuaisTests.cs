using Sistema.APP.Services;

namespace Sistema.Tests;

// F-V — lógica pura da reconciliação ANUAL de proventos (oficial anual B3 × materializado),
// por ano e por ticker×tipo, na base de valor líquido. Sem DbContext.
public class ReconciliadorProventosAnuaisTests
{
    private static ReconciliadorProventosAnuais.OficialAnual Oficial(int ano, string ticker, string tipo, decimal valor)
        => new(ano, ticker, tipo, valor);

    private static ReconciliadorProventosAnuais.MaterializadoAnual Mat(int ano, string? ticker, string tipo, decimal liquido)
        => new(ano, ticker, tipo, liquido);

    [Fact]
    public void SemOficiais_RetornaVazio()
    {
        var dto = ReconciliadorProventosAnuais.Reconciliar([], [Mat(2024, "BBAS3", "Dividendo", 100m)]);

        Assert.False(dto.TemDados);
        Assert.Empty(dto.Anos);
    }

    [Fact]
    public void OficialEMaterializadoIguais_Bate()
    {
        var oficiais = new[] { Oficial(2024, "BBAS3", "Dividendo", 59.77m) };
        var mats = new[] { Mat(2024, "BBAS3", "Dividendo", 59.77m) };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        Assert.True(dto.TemDados);
        var ano = Assert.Single(dto.Anos);
        var linha = Assert.Single(ano.Linhas);
        Assert.Equal(ReconciliadorProventosAnuais.StatusBate, linha.Status);
        Assert.Equal(0m, linha.Diferenca);
        Assert.False(dto.TemDivergencia);
        Assert.Equal(ReconciliadorProventosAnuais.StatusBate, ano.Status);
    }

    [Fact]
    public void PequenaDiferencaGrossNet_DentroDaTolerancia_Bate()
    {
        // Brapi traz bruto e a B3 líquido: diferença pequena (< 2% / < R$5) ainda "bate".
        var oficiais = new[] { Oficial(2024, "PETR4", "Dividendo", 196.91m) };
        var mats = new[] { Mat(2024, "PETR4", "Dividendo", 199.00m) }; // +2,09 (~1%)

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        Assert.Equal(ReconciliadorProventosAnuais.StatusBate, dto.Anos[0].Linhas[0].Status);
    }

    [Fact]
    public void MaterializadoMenor_AlemDaTolerancia_FaltaMaterializado()
    {
        // Buraco grande (ex.: um mês não materializou): oficial 327 vs materializado 200.
        var oficiais = new[] { Oficial(2024, "IRDM11", "Rendimento", 327.12m) };
        var mats = new[] { Mat(2024, "IRDM11", "Rendimento", 200.00m) };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        var linha = dto.Anos[0].Linhas[0];
        Assert.Equal(ReconciliadorProventosAnuais.StatusFaltaMaterializado, linha.Status);
        Assert.True(linha.Diferenca < 0m);
        Assert.True(dto.TemDivergencia);
        Assert.Equal(1, dto.Anos[0].LinhasDivergentes);
    }

    [Fact]
    public void MaterializadoMaior_AlemDaTolerancia_SobraMaterializado()
    {
        // Dupla contagem (B3 + Brapi não suprimida): materializado maior que o oficial.
        var oficiais = new[] { Oficial(2024, "CPTS11", "Rendimento", 517.35m) };
        var mats = new[] { Mat(2024, "CPTS11", "Rendimento", 700.00m) };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        var linha = dto.Anos[0].Linhas[0];
        Assert.Equal(ReconciliadorProventosAnuais.StatusSobraMaterializado, linha.Status);
        Assert.True(linha.Diferenca > 0m);
    }

    [Fact]
    public void OficialSemNenhumMaterializado_SemAtivo()
    {
        // Ativo oficial que não casou com nada materializado (ticker não bate / nada importado).
        var oficiais = new[] { Oficial(2024, "NCHB11", "Rendimento", 328.68m) };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, []);

        var linha = dto.Anos[0].Linhas[0];
        Assert.Equal(ReconciliadorProventosAnuais.StatusSemAtivo, linha.Status);
        Assert.Equal(328.68m, linha.Oficial);
        Assert.Equal(0m, linha.Materializado);
    }

    [Fact]
    public void CasaPorTickerETipo_NormalizandoCaixaEEspacos()
    {
        // Ticker/tipo casam ignorando caixa e espaços (oficial " bbas3 "/"jcp" × mat "BBAS3"/"JCP").
        var oficiais = new[] { Oficial(2024, " bbas3 ", "JCP", 299.01m) };
        var mats = new[] { Mat(2024, "BBAS3", "jcp", 299.01m) };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        var linha = dto.Anos[0].Linhas[0];
        Assert.Equal(ReconciliadorProventosAnuais.StatusBate, linha.Status);
        Assert.Equal(299.01m, linha.Materializado);
    }

    [Fact]
    public void TotaisDoAno_SomamOficialEMaterializado_EOrdenamDescrescente()
    {
        var oficiais = new[]
        {
            Oficial(2023, "BBAS3", "Dividendo", 100m),
            Oficial(2024, "BBAS3", "Dividendo", 60m),
            Oficial(2024, "PETR4", "Dividendo", 40m)
        };
        var mats = new[]
        {
            Mat(2023, "BBAS3", "Dividendo", 100m),
            Mat(2024, "BBAS3", "Dividendo", 60m),
            Mat(2024, "PETR4", "Dividendo", 40m)
        };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        // Anos em ordem decrescente (2024 primeiro).
        Assert.Equal(2024, dto.Anos[0].Ano);
        Assert.Equal(2023, dto.Anos[1].Ano);
        // Total do ano 2024 = 100 oficial e 100 materializado.
        Assert.Equal(100m, dto.Anos[0].TotalOficial);
        Assert.Equal(100m, dto.Anos[0].TotalMaterializado);
        // Total global.
        Assert.Equal(200m, dto.TotalOficial);
        Assert.Equal(200m, dto.TotalMaterializado);
    }

    [Fact]
    public void MaterializadoSemOficialCorrespondente_NaoInventaLinha_MasContaNoTotalDoAno()
    {
        // Earn cripto materializado (sem linha oficial anual da B3): não vira linha, mas o total
        // materializado do ano o reflete → a diferença do TOTAL fica visível.
        var oficiais = new[] { Oficial(2024, "BBAS3", "Dividendo", 60m) };
        var mats = new[]
        {
            Mat(2024, "BBAS3", "Dividendo", 60m),
            Mat(2024, "BTC", "Rendimento", 25m) // earn — sem oficial
        };

        var dto = ReconciliadorProventosAnuais.Reconciliar(oficiais, mats);

        // Só a linha oficial (BBAS3) aparece.
        Assert.Single(dto.Anos[0].Linhas);
        Assert.Equal("BBAS3", dto.Anos[0].Linhas[0].Ticker);
        // Mas o total materializado do ano inclui o earn (85), revelando a diferença no total.
        Assert.Equal(60m, dto.Anos[0].TotalOficial);
        Assert.Equal(85m, dto.Anos[0].TotalMaterializado);
        Assert.Equal(25m, dto.Anos[0].Diferenca);
    }
}
