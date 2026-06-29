using Sistema.INFRA.Importers;

namespace Sistema.Tests;

// Causa A — detecção BARATA de arquivo novo nas pastas monitoradas (sem recalcular Sha256).
// Decide se a varredura precisa re-rodar comparando os caminhos suportados presentes na pasta
// com os StoredPaths já importados. É a lógica pura por trás de ExistemArquivosNovosAsync.
public class DeteccaoNovosArquivosTests
{
    private static HashSet<string> Importados(params string[] paths)
        => new(paths, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void PastaNuncaVarrida_ComArquivos_DetectaNovo()
    {
        // Conjunto de importados vazio (1ª varredura): qualquer arquivo presente é novo.
        var arquivos = new[] { @"C:\arquivos\b3\relatorio-consolidado-mensal-2025-novembro.xlsx" };
        Assert.True(DeteccaoNovosArquivos.HaArquivoNovo(arquivos, Importados()));
    }

    [Fact]
    public void ArquivoLargadoDepois_EmPastaJaVarrida_DetectaNovo()
    {
        // O cenário do Rafael: novembro/2025 foi largado DEPOIS de a pasta já ter sido varrida.
        var arquivos = new[]
        {
            @"C:\arquivos\b3\relatorio-consolidado-mensal-2025-outubro.xlsx",
            @"C:\arquivos\b3\relatorio-consolidado-mensal-2025-novembro.xlsx",
        };
        var jaImportados = Importados(@"C:\arquivos\b3\relatorio-consolidado-mensal-2025-outubro.xlsx");
        Assert.True(DeteccaoNovosArquivos.HaArquivoNovo(arquivos, jaImportados));
    }

    [Fact]
    public void TodosJaImportados_NaoDetectaNovo()
    {
        // Nada novo → não re-varre (idempotente entre loads do dashboard).
        var arquivos = new[]
        {
            @"C:\arquivos\b3\relatorio-consolidado-mensal-2025-outubro.xlsx",
            @"C:\arquivos\b3\relatorio-consolidado-mensal-2025-novembro.xlsx",
        };
        var jaImportados = Importados(arquivos);
        Assert.False(DeteccaoNovosArquivos.HaArquivoNovo(arquivos, jaImportados));
    }

    [Fact]
    public void ComparacaoDeCaminho_EhCaseInsensitive()
    {
        // No Windows o mesmo arquivo pode vir com caixa diferente → não pode contar como novo.
        var arquivos = new[] { @"C:\Arquivos\B3\Relatorio.xlsx" };
        var jaImportados = Importados(@"c:\arquivos\b3\relatorio.xlsx");
        Assert.False(DeteccaoNovosArquivos.HaArquivoNovo(arquivos, jaImportados));
    }

    [Fact]
    public void ZipJaImportado_NaoReconta_PeloStoredPathDoZip()
    {
        // Um .zip gera vários DocumentoFinanceiro, mas todos com StoredPath = caminho do zip.
        // Comparar pelo StoredPath cobre o zip sem reler bytes: o mesmo zip não é "novo".
        var arquivos = new[] { @"C:\arquivos\financeiro\notas.zip" };
        var jaImportados = Importados(@"C:\arquivos\financeiro\notas.zip");
        Assert.False(DeteccaoNovosArquivos.HaArquivoNovo(arquivos, jaImportados));
    }

    [Fact]
    public void PastaVazia_NaoDetectaNovo()
        => Assert.False(DeteccaoNovosArquivos.HaArquivoNovo(Array.Empty<string>(), Importados()));

    [Fact]
    public void IgnoraEntradasEmBranco()
    {
        var arquivos = new[] { "", "   " };
        Assert.False(DeteccaoNovosArquivos.HaArquivoNovo(arquivos, Importados()));
    }
}
