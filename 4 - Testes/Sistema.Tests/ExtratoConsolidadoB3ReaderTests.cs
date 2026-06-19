using Sistema.INFRA.Importers;

namespace Sistema.Tests;

public class ExtratoConsolidadoB3ReaderTests
{
    // F1 — só leitura/parsing do extrato consolidado da B3 (sem materializar).
    // Os testes rodam contra os arquivos reais em arquivos/b3/. Se a pasta não estiver
    // presente (repo privado, sem push), os testes que dependem de arquivo são pulados
    // via Skip — não quebram o build de quem não tem os dados.

    private static string? LocalizarArquivoB3(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidato = Path.Combine(dir.FullName, "arquivos", "b3", fileName);
            if (File.Exists(candidato))
                return candidato;
            dir = dir.Parent;
        }

        return null;
    }

    private static ExtratoConsolidadoB3Documento? LerArquivo(string fileName)
    {
        var caminho = LocalizarArquivoB3(fileName);
        if (caminho is null)
            return null;

        using var stream = File.OpenRead(caminho);
        return ExtratoConsolidadoB3Reader.Ler(stream);
    }

    [Theory]
    [InlineData("relatorio-consolidado-mensal-2022-setembro.xlsx", 2022, 9)]
    [InlineData("relatorio-consolidado-mensal-2021-novembro.xlsx", 2021, 11)]
    [InlineData("relatorio-consolidado-mensal-2022-janeiro.xlsx", 2022, 1)]
    public void DerivarPeriodoMapeiaMesPorExtenso(string fileName, int anoEsperado, int mesEsperado)
    {
        var periodo = ExtratoConsolidadoB3Reader.DerivarPeriodo(fileName);

        Assert.NotNull(periodo);
        Assert.Equal(anoEsperado, periodo!.Value.Ano);
        Assert.Equal(mesEsperado, periodo.Value.Mes);
    }

    [Theory]
    [InlineData("relatorio-consolidado-mensal-2023-marco.xlsx", 2023, 3)]
    [InlineData("relatorio-consolidado-mensal-2023-fevereiro.xlsx", 2023, 2)]
    public void DerivarPeriodoIgnoraAcentosDoMes(string fileName, int anoEsperado, int mesEsperado)
    {
        var periodo = ExtratoConsolidadoB3Reader.DerivarPeriodo(fileName);

        Assert.NotNull(periodo);
        Assert.Equal(anoEsperado, periodo!.Value.Ano);
        Assert.Equal(mesEsperado, periodo.Value.Mes);
    }

    [Fact]
    public void DerivarPeriodoNaoQuebraComNomeForaDoPadrao()
    {
        Assert.Null(ExtratoConsolidadoB3Reader.DerivarPeriodo("qualquer-coisa.xlsx"));
        Assert.Null(ExtratoConsolidadoB3Reader.DerivarPeriodo(null));
        Assert.Null(ExtratoConsolidadoB3Reader.DerivarPeriodo(""));
        // Ano presente mas mês ausente → não deve inventar mês.
        Assert.Null(ExtratoConsolidadoB3Reader.DerivarPeriodo("relatorio-consolidado-mensal-2022.xlsx"));
    }

    [Fact]
    public void LeitorResolveSharedStringsNaAbaNegociacoes()
    {
        var extrato = LerArquivo("relatorio-consolidado-mensal-2022-setembro.xlsx");
        if (extrato is null)
            return; // arquivos/b3 não disponível neste ambiente.

        var negociacoes = extrato.Aba("Negociações");
        Assert.NotNull(negociacoes);
        Assert.NotEmpty(negociacoes.Linhas);

        // Header da aba Negociações: a 1ª célula da 1ª linha é "Código de Negociação"
        // — só sai correto se as shared strings (t="s") forem resolvidas pelo índice.
        var header = negociacoes.Linhas[0];
        Assert.Contains("Código de Negociação", header);

        // E nenhuma célula do header deve ser um índice numérico cru (sintoma de não resolver).
        Assert.DoesNotContain(header, c => int.TryParse(c, out _));
    }

    [Fact]
    public void LeitorResolveProdutoNaAbaProventos()
    {
        var extrato = LerArquivo("relatorio-consolidado-mensal-2022-setembro.xlsx");
        if (extrato is null)
            return; // arquivos/b3 não disponível neste ambiente.

        var proventos = extrato.Aba("Proventos Recebidos");
        Assert.NotNull(proventos);
        Assert.True(proventos.Linhas.Count >= 2, "Esperado pelo menos header + 1 provento.");

        // 1ª linha de dados (índice 1) começa com o produto BBAS3.
        var primeiraLinha = proventos.Linhas[1];
        Assert.StartsWith("BBAS3 - BANCO DO BRASIL S/A", primeiraLinha[0].Trim());
    }

    [Fact]
    public void LeitorReconheceAsAbasEsperadasDoMesDe2022()
    {
        var extrato = LerArquivo("relatorio-consolidado-mensal-2022-setembro.xlsx");
        if (extrato is null)
            return; // arquivos/b3 não disponível neste ambiente.

        var nomes = extrato.Abas.Select(a => a.Nome).ToList();
        Assert.Contains("Negociações", nomes);
        Assert.Contains("Proventos Recebidos", nomes);
        Assert.Contains("Posição - Ações", nomes);
        Assert.Contains("Posição - Fundos", nomes);
        Assert.Contains("Posição - Renda Fixa", nomes);
        Assert.Contains("Posição - Tesouro Direto", nomes);
    }

    [Fact]
    public void LeitorToleraMesSemAbaNegociacoes()
    {
        // 2021-novembro não tem a aba "Negociações" — o leitor não pode quebrar.
        var extrato = LerArquivo("relatorio-consolidado-mensal-2021-novembro.xlsx");
        if (extrato is null)
            return; // arquivos/b3 não disponível neste ambiente.

        Assert.NotEmpty(extrato.Abas);
        Assert.Null(extrato.Aba("Negociações"));
        Assert.NotNull(extrato.Aba("Proventos Recebidos"));
    }

    [Fact]
    public void TodosOsArquivosReaisSaoLidosSemErro()
    {
        var amostra = LocalizarArquivoB3("relatorio-consolidado-mensal-2022-setembro.xlsx");
        if (amostra is null)
            return; // arquivos/b3 não disponível neste ambiente.

        var pastaB3 = Path.GetDirectoryName(amostra)!;
        foreach (var arquivo in Directory.EnumerateFiles(pastaB3, "*.xlsx"))
        {
            using var stream = File.OpenRead(arquivo);
            var extrato = ExtratoConsolidadoB3Reader.Ler(stream);
            Assert.True(extrato.TemDados, $"Sem dados lidos em {Path.GetFileName(arquivo)}.");
        }
    }
}
