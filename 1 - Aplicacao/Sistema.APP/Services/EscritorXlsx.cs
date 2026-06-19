using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Sistema.APP.Services;

/// <summary>Uma aba do .xlsx: nome + linhas (cada célula é string ou número).</summary>
public sealed record AbaXlsx(string Nome, IReadOnlyList<IReadOnlyList<object?>> Linhas);

/// <summary>
/// Escritor mínimo de .xlsx (OOXML via ZipArchive, inline strings) — sem dependência externa.
/// Suficiente para exportações simples (uma aba por bloco). Strings vão como inlineStr; números
/// (int/long/decimal/double) como célula numérica. Lido de volta pelo ExtratoConsolidadoB3Reader.
/// </summary>
public static class EscritorXlsx
{
    public static byte[] Gerar(IReadOnlyList<AbaXlsx> abas)
    {
        ArgumentNullException.ThrowIfNull(abas);
        var nomes = NomesUnicos(abas.Select(a => a.Nome).ToList());

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            Escrever(zip, "[Content_Types].xml", ContentTypes(abas.Count));
            Escrever(zip, "_rels/.rels", RelsRaiz());
            Escrever(zip, "xl/workbook.xml", Workbook(nomes));
            Escrever(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(abas.Count));
            for (var i = 0; i < abas.Count; i++)
                Escrever(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(abas[i].Linhas));
        }

        return ms.ToArray();
    }

    private static void Escrever(ZipArchive zip, string caminho, string conteudo)
    {
        var entry = zip.CreateEntry(caminho, CompressionLevel.Optimal);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write(conteudo);
    }

    private static string Sheet(IReadOnlyList<IReadOnlyList<object?>> linhas)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        for (var r = 0; r < linhas.Count; r++)
        {
            sb.Append("<row r=\"").Append(r + 1).Append("\">");
            var linha = linhas[r];
            for (var c = 0; c < linha.Count; c++)
            {
                var refCel = Coluna(c) + (r + 1);
                var valor = linha[c];
                if (valor is null)
                    continue;
                if (EhNumero(valor, out var numero))
                {
                    sb.Append("<c r=\"").Append(refCel).Append("\"><v>")
                      .Append(numero.ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
                }
                else
                {
                    sb.Append("<c r=\"").Append(refCel).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                      .Append(EscapeXml(valor.ToString() ?? string.Empty)).Append("</t></is></c>");
                }
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string Workbook(IReadOnlyList<string> nomes)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
        sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
        for (var i = 0; i < nomes.Count; i++)
            sb.Append("<sheet name=\"").Append(EscapeXml(nomes[i])).Append("\" sheetId=\"")
              .Append(i + 1).Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRels(int qtd)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (var i = 0; i < qtd; i++)
            sb.Append("<Relationship Id=\"rId").Append(i + 1)
              .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet")
              .Append(i + 1).Append(".xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string ContentTypes(int qtd)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        for (var i = 0; i < qtd; i++)
            sb.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i + 1)
              .Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RelsRaiz()
        => "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
         + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
         + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"
         + "</Relationships>";

    private static bool EhNumero(object valor, out decimal numero)
    {
        switch (valor)
        {
            case decimal d: numero = d; return true;
            case int i: numero = i; return true;
            case long l: numero = l; return true;
            case double db: numero = (decimal)db; return true;
            default: numero = 0m; return false;
        }
    }

    private static string Coluna(int indice)
    {
        var sb = new StringBuilder();
        indice++;
        while (indice > 0)
        {
            var resto = (indice - 1) % 26;
            sb.Insert(0, (char)('A' + resto));
            indice = (indice - 1) / 26;
        }
        return sb.ToString();
    }

    // Nome de aba: máx 31 chars, sem : \ / ? * [ ]; e único no workbook.
    private static List<string> NomesUnicos(IReadOnlyList<string> nomes)
    {
        var resultado = new List<string>();
        var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var nome in nomes)
        {
            var limpo = new string((nome ?? "Plan").Where(c => c is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray()).Trim();
            if (string.IsNullOrEmpty(limpo)) limpo = "Plan";
            if (limpo.Length > 31) limpo = limpo[..31];
            var final = limpo;
            var n = 1;
            while (!usados.Add(final))
            {
                var sufixo = " (" + (++n) + ")";
                final = limpo.Length + sufixo.Length > 31 ? limpo[..(31 - sufixo.Length)] + sufixo : limpo + sufixo;
            }
            resultado.Add(final);
        }
        return resultado;
    }

    private static string EscapeXml(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
