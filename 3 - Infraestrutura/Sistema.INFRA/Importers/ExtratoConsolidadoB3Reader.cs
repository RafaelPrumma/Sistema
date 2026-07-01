using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Leitor puro (sem banco) do extrato consolidado mensal da Área do Investidor B3.
/// O workbook .xlsx usa shared strings (células <c t="s"><v>índice</v>) — o leitor
/// genérico de XLSX da Binance (que lê só sheet1.xml e ignora sharedStrings) não serve.
/// Itera TODAS as abas presentes (a quantidade/nome varia por mês: BDR, ETF, Renda Fixa,
/// Tesouro Direto aparecem só em alguns; alguns meses nem têm a aba Negociações) cruzando
/// xl/workbook.xml (nome + r:id) com xl/_rels/workbook.xml.rels (r:id → target).
/// </summary>
public static class ExtratoConsolidadoB3Reader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rels = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace OfficeRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly IReadOnlyDictionary<string, int> MesesPt = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["janeiro"] = 1,
        ["fevereiro"] = 2,
        ["marco"] = 3,
        ["abril"] = 4,
        ["maio"] = 5,
        ["junho"] = 6,
        ["julho"] = 7,
        ["agosto"] = 8,
        ["setembro"] = 9,
        ["outubro"] = 10,
        ["novembro"] = 11,
        ["dezembro"] = 12
    };

    /// <summary>
    /// Lê o workbook do stream e devolve, para cada aba presente, suas linhas (cada linha = lista
    /// de células-texto já com shared strings resolvidos). Não persiste nada e não materializa.
    /// </summary>
    public static ExtratoConsolidadoB3Documento Ler(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        var shared = CarregarSharedStrings(zip);
        var relsPorId = CarregarRelacionamentos(zip);

        var abas = new List<AbaExtratoB3>();
        var workbookEntry = zip.GetEntry("xl/workbook.xml");
        if (workbookEntry is null)
            return new ExtratoConsolidadoB3Documento(abas);

        var workbook = XDocument.Load(workbookEntry.Open());
        foreach (var sheet in workbook.Descendants(Main + "sheet"))
        {
            var nome = (string?)sheet.Attribute("name") ?? string.Empty;
            var rid = (string?)sheet.Attribute(OfficeRel + "id");
            if (string.IsNullOrEmpty(rid) || !relsPorId.TryGetValue(rid, out var target))
                continue;

            var sheetEntry = zip.GetEntry(NormalizarTarget(target));
            if (sheetEntry is null)
                continue;

            var linhas = LerLinhasDaAba(sheetEntry, shared);
            abas.Add(new AbaExtratoB3(nome, linhas));
        }

        return new ExtratoConsolidadoB3Documento(abas);
    }

    /// <summary>
    /// Deriva (ano, mês) do nome do arquivo no padrão
    /// "relatorio-consolidado-mensal-AAAA-&lt;mês-pt&gt;.xlsx". Case/acento-insensível.
    /// Retorna null quando não bate (mês ausente/desconhecido nunca deve quebrar a importação).
    /// </summary>
    public static (int Ano, int Mes)? DerivarPeriodo(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var nome = Path.GetFileNameWithoutExtension(fileName);
        var normalizado = RemoverAcentos(nome).ToLowerInvariant();

        var anoMatch = System.Text.RegularExpressions.Regex.Match(normalizado, @"20\d{2}");
        if (!anoMatch.Success || !int.TryParse(anoMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ano))
            return null;

        foreach (var (mesNome, mesNumero) in MesesPt)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(normalizado, $@"\b{mesNome}\b"))
                return (ano, mesNumero);
        }

        return null;
    }

    /// <summary>
    /// Deriva o ANO do relatório consolidado ANUAL no padrão
    /// "relatorio-consolidado-anual-AAAA.xlsx" (sem mês — é um agregado do ano inteiro).
    /// Distinto de <see cref="DerivarPeriodo"/> (mensal): o anual NÃO tem aba Negociações nem
    /// datas nos proventos; serve só de VERDADE OFICIAL do total do ano (reconciliação).
    /// Retorna null quando não bate o padrão "anual" (nunca quebra a importação).
    /// </summary>
    public static int? DerivarAno(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var nome = Path.GetFileNameWithoutExtension(fileName);
        var normalizado = RemoverAcentos(nome).ToLowerInvariant();

        // Precisa conter "anual" para não confundir com o mensal (que também tem ano no nome).
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizado, @"\banual\b"))
            return null;

        var anoMatch = System.Text.RegularExpressions.Regex.Match(normalizado, @"20\d{2}");
        return anoMatch.Success && int.TryParse(anoMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ano)
            ? ano
            : null;
    }

    private static List<List<string>> LerLinhasDaAba(ZipArchiveEntry entry, IReadOnlyList<string> shared)
    {
        var document = XDocument.Load(entry.Open());
        var linhas = new List<List<string>>();

        foreach (var row in document.Descendants(Main + "row"))
        {
            var celulas = new List<string>();
            foreach (var cell in row.Elements(Main + "c"))
                celulas.Add(ResolverCelula(cell, shared));

            linhas.Add(celulas);
        }

        return linhas;
    }

    private static string ResolverCelula(XElement cell, IReadOnlyList<string> shared)
    {
        var tipo = (string?)cell.Attribute("t");

        // Inline string: <c t="inlineStr"><is><t>...</t></is>
        var inline = cell.Element(Main + "is");
        if (inline is not null)
            return string.Concat(inline.Descendants(Main + "t").Select(x => x.Value));

        var valor = cell.Element(Main + "v")?.Value ?? string.Empty;
        if (tipo == "s")
        {
            return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
                   && idx >= 0 && idx < shared.Count
                ? shared[idx]
                : string.Empty;
        }

        return valor;
    }

    private static IReadOnlyList<string> CarregarSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return Array.Empty<string>();

        var document = XDocument.Load(entry.Open());
        // Cada <si> pode ter um único <t> ou vários runs <r><t>...</t></r>.
        return document.Descendants(Main + "si")
            .Select(si => string.Concat(si.Descendants(Main + "t").Select(t => t.Value)))
            .ToList();
    }

    private static IReadOnlyDictionary<string, string> CarregarRelacionamentos(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/_rels/workbook.xml.rels");
        if (entry is null)
            return new Dictionary<string, string>();

        var document = XDocument.Load(entry.Open());
        return document.Descendants(Rels + "Relationship")
            .Where(r => (string?)r.Attribute("Id") is not null && (string?)r.Attribute("Target") is not null)
            .GroupBy(r => (string)r.Attribute("Id")!)
            .ToDictionary(g => g.Key, g => (string)g.First().Attribute("Target")!, StringComparer.Ordinal);
    }

    private static string NormalizarTarget(string target)
    {
        // Targets vêm relativos a xl/ (ex.: "worksheets/sheet1.xml"); às vezes absolutos ("/xl/...").
        var limpo = target.Replace('\\', '/').TrimStart('/');
        if (limpo.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            return limpo;
        return "xl/" + limpo;
    }

    private static string RemoverAcentos(string value)
        => string.Concat(value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark))
            .Normalize(System.Text.NormalizationForm.FormC);
}

/// <summary>Resultado bruto do leitor: as abas presentes e suas linhas.</summary>
public sealed record ExtratoConsolidadoB3Documento(IReadOnlyList<AbaExtratoB3> Abas)
{
    public bool TemDados => Abas.Any(a => a.Linhas.Count > 0);

    public AbaExtratoB3? Aba(string nome)
        => Abas.FirstOrDefault(a => string.Equals(a.Nome, nome, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Uma aba (sheet) e suas linhas já com shared strings resolvidos.</summary>
public sealed record AbaExtratoB3(string Nome, IReadOnlyList<IReadOnlyList<string>> Linhas);
