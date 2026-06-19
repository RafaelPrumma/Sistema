using System.Text.Json;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

// Fase 1 do importador do extrato consolidado da B3 (specs/importador-b3.spec.md).
// Escopo: ler o workbook (6+ abas, resolvendo shared strings), derivar o ano-mês do nome
// e gravar o conteúdo BRUTO em ConteudosBrutosFinanceiros. NÃO materializa em
// TransacaoFinanceira nem em proventos (isso é F2).
public partial class FinancasImportador
{
    private async Task<int> ProcessarExtratoB3Async(DocumentoFinanceiro documento, string file, CancellationToken cancellationToken)
    {
        ExtratoConsolidadoB3Documento extrato;
        await using (var stream = File.OpenRead(file))
        {
            extrato = ExtratoConsolidadoB3Reader.Ler(stream);
        }

        // Período vem do NOME do arquivo (as abas de Posição são snapshot sem data interna).
        var periodo = ExtratoConsolidadoB3Reader.DerivarPeriodo(documento.FileName);
        if (periodo is not null)
            documento.ReferenceYear = periodo.Value.Ano;

        // Guarda o ano-mês derivado no RawMetadataJson — nesta fase NÃO criamos coluna/migration.
        documento.RawMetadataJson = JsonSerializer.Serialize(new
        {
            path = file,
            sha256 = documento.Sha256,
            documentKind = documento.DocumentKind.ToString(),
            referenceYear = periodo?.Ano,
            referenceMonth = periodo?.Mes,
            referencePeriod = periodo is null ? null : $"{periodo.Value.Ano:D4}-{periodo.Value.Mes:D2}",
            sheetNames = extrato.Abas.Select(a => a.Nome).ToArray()
        });

        var totalLinhas = 0;
        foreach (var aba in extrato.Abas)
        {
            // Aba presente porém vazia não é erro; apenas não gera conteúdo bruto.
            if (aba.Linhas.Count == 0)
                continue;

            // Primeira linha = cabeçalho (nomes de coluna já em texto via shared strings).
            var headers = aba.Linhas[0].Select(c => (c ?? string.Empty).Trim()).ToList();

            for (var i = 0; i < aba.Linhas.Count; i++)
            {
                var celulas = aba.Linhas[i];
                var payload = new
                {
                    sheet = aba.Nome,
                    isHeader = i == 0,
                    cells = celulas,
                    row = MapearLinha(headers, celulas)
                };

                _context.ConteudosBrutosFinanceiros.Add(new ConteudoBrutoFinanceiro
                {
                    DocumentoFinanceiro = documento,
                    ContentType = TipoConteudoBrutoFinanceiro.LinhaPlanilha,
                    SheetName = aba.Nome,
                    RowNumber = i + 1,
                    RawJson = JsonSerializer.Serialize(payload),
                    UsuarioInclusao = "financas-importador"
                });

                totalLinhas++;
            }
        }

        documento.ParseStatus = totalLinhas > 0
            ? StatusParseDocumentoFinanceiro.Processado
            : StatusParseDocumentoFinanceiro.SemDadosEstruturados;

        // F1 não materializa: nenhuma TransacaoFinanceira/provento é criada aqui.
        return totalLinhas;
    }

    private static Dictionary<string, string> MapearLinha(IReadOnlyList<string> headers, IReadOnlyList<string> celulas)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < celulas.Count; i++)
        {
            if (i >= headers.Count)
                break;

            var header = headers[i];
            if (string.IsNullOrWhiteSpace(header) || row.ContainsKey(header))
                continue;

            row[header] = (celulas[i] ?? string.Empty).Trim();
        }

        return row;
    }
}
