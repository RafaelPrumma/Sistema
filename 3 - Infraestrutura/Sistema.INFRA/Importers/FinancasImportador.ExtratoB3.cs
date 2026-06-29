using System.Globalization;
using System.Text.Json;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

// Importador do extrato consolidado da B3 (specs/importador-b3.spec.md).
// F1: ler o workbook (6+ abas, resolvendo shared strings), derivar o ano-mês do nome
//     e gravar o conteúdo BRUTO em ConteudosBrutosFinanceiros.
// F2 (este arquivo, além do bruto): povoar o staging NegociacaoMensalB3 (aba Negociações)
//     e materializar os proventos (aba Proventos Recebidos) via UpsertRendimento.
//     A materialização das Negociações em TransacaoFinanceira fica em
//     SincronizarTransacoesCanonicasAsync (FinancasImportador.cs), aplicando a precedência §3.1.
// A lógica PURA (parsing/precedência) vive em ExtratoB3Materializador (testável sem DbContext).
public partial class FinancasImportador
{
    private const string FonteExtratoB3 = ExtratoB3Materializador.Fonte;

    private async Task<int> ProcessarExtratoB3Async(DocumentoFinanceiro documento, string file, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
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

        // Guarda o ano-mês derivado no RawMetadataJson.
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
                    row = ExtratoB3Materializador.MapearLinha(headers, celulas)
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

        // F2 — staging das Negociações (agregado mensal) e materialização dos proventos.
        // Sem período derivável não dá pra datar nem chavear o agregado → só fica o bruto.
        if (periodo is not null)
        {
            PovoarNegociacoesMensaisB3(extrato, documento, carga, periodo.Value, ativos);
            await MaterializarProventosExtratoB3(extrato, documento, carga, ativos, cancellationToken);
        }

        documento.ParseStatus = totalLinhas > 0
            ? StatusParseDocumentoFinanceiro.Processado
            : StatusParseDocumentoFinanceiro.SemDadosEstruturados;

        return totalLinhas;
    }

    // Aba "Negociações": agregado mensal por ticker (até 1 compra + 1 venda). Gera registros de
    // staging em NegociacaoMensalB3; a materialização em TransacaoFinanceira (com precedência vs
    // notas) acontece no resync. A aba pode estar ausente em alguns meses — isso não é erro.
    private void PovoarNegociacoesMensaisB3(ExtratoConsolidadoB3Documento extrato, DocumentoFinanceiro documento, CargaFinanceira carga, (int Ano, int Mes) periodo, Dictionary<string, AtivoFinanceiro> ativos)
    {
        var aba = extrato.Aba("Negociações");
        if (aba is null || aba.Linhas.Count < 2)
            return;

        var headers = aba.Linhas[0].Select(c => (c ?? string.Empty).Trim()).ToList();
        var anoMes = ExtratoB3Materializador.AnoMes(periodo.Ano, periodo.Mes);
        var ultimoDiaDoMes = new DateTime(periodo.Ano, periodo.Mes, DateTime.DaysInMonth(periodo.Ano, periodo.Mes));

        for (var i = 1; i < aba.Linhas.Count; i++)
        {
            var row = ExtratoB3Materializador.MapearLinha(headers, aba.Linhas[i]);
            foreach (var mov in ExtratoB3Materializador.InterpretarNegociacao(row))
            {
                // Ticker já vem canônico no extrato (não precisa do NormalizadorAtivoB3).
                var ativo = ObterOuCriarAtivo(ativos, mov.Ticker, mov.Ticker,
                    ExtratoB3Materializador.ClassePorTicker(mov.Ticker), false, mov.Ticker);

                // RF/TD/BDR/ETF ficam fora do cálculo (classes não modeladas) — não materializamos.
                if (ativo.Classe is ClasseAtivo.RendaFixa or ClasseAtivo.Caixa)
                    continue;

                var broker = string.IsNullOrWhiteSpace(mov.Broker) ? "NU Invest" : mov.Broker!;
                var chave = ExtratoB3Materializador.ChaveNegociacao(ativo.Chave, anoMes, mov.OperationType, broker);
                var data = (mov.PeriodoFinal ?? ultimoDiaDoMes).Date;
                var bruto = decimal.Round(mov.Quantity * mov.UnitPrice, 8);
                var rawJson = JsonSerializer.Serialize(new { data = data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), cells = aba.Linhas[i] });

                // "O extrato mais recente do mês manda" (Causa B). A chave natural NÃO inclui
                // quantidade/preço, então um extrato parcial (qtd 300) e depois o completo (qtd 500)
                // colidem na mesma chave. ANTES isto era pulado (continue) e a posição travava no
                // parcial. AGORA: se já existe staging com essa chave, ATUALIZAMOS o registro com os
                // valores do extrato sendo importado (o mais recente vence) e removemos a transação
                // canônica antiga (StagingTipo/StagingId) para o resync remateralizar com o novo valor.
                // Reimportar o MESMO arquivo é barrado antes (dedup por Sha256 do documento); aqui a
                // atualização só vale quando chega um documento NOVO para o mesmo ticker×mês.
                var existente = _context.NegociacoesMensaisB3.Local.FirstOrDefault(x => x.ChaveNatural == chave)
                    ?? _context.NegociacoesMensaisB3.FirstOrDefault(x => x.ChaveNatural == chave);
                if (existente is not null)
                {
                    if (!ExtratoB3Materializador.NegociacaoMudou(
                            existente.Quantity, existente.UnitPrice, existente.GrossAmount,
                            mov.Quantity, mov.UnitPrice, bruto))
                        continue; // mesmos valores → no-op (nada a remateralizar).

                    existente.Quantity = mov.Quantity;
                    existente.UnitPrice = mov.UnitPrice;
                    existente.GrossAmount = bruto;
                    existente.PeriodoInicial = mov.PeriodoInicial;
                    existente.PeriodoFinal = mov.PeriodoFinal;
                    existente.Broker = broker;
                    existente.SourceDocument = documento;   // passa a apontar para o extrato mais recente.
                    existente.CargaFinanceira = carga;
                    existente.RawJson = rawJson;

                    // Remove a transação canônica antiga deste staging para o resync reconstruí-la com o
                    // valor atualizado (o DuplicateGroupKey = "NegociacaoMensalB3#{Id}" continua o mesmo,
                    // então sem este delete o SincronizarTransacoesCanonicas pularia por já-materializada).
                    RemoverTransacaoCanonicaDoStaging("NegociacaoMensalB3", existente.Id);
                    continue;
                }

                _context.NegociacoesMensaisB3.Add(new NegociacaoMensalB3
                {
                    CargaFinanceira = carga,
                    SourceDocument = documento,
                    Asset = ativo,
                    AnoMes = anoMes,
                    OperationType = mov.OperationType,
                    Quantity = mov.Quantity,
                    UnitPrice = mov.UnitPrice,
                    GrossAmount = bruto,
                    PeriodoInicial = mov.PeriodoInicial,
                    PeriodoFinal = mov.PeriodoFinal,
                    Broker = broker,
                    ChaveNatural = chave,
                    RawJson = rawJson,
                    UsuarioInclusao = "financas-importador"
                });
            }
        }
    }

    // Aba "Proventos Recebidos": provento realizado oficial (inclui FII). Usa o UpsertRendimento
    // compartilhado (dedup econômico por ticker+data+tipo) — não reescreve a lógica de dedup.
    private async Task MaterializarProventosExtratoB3(ExtratoConsolidadoB3Documento extrato, DocumentoFinanceiro documento, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        var aba = extrato.Aba("Proventos Recebidos");
        if (aba is null || aba.Linhas.Count < 2)
            return;

        var headers = aba.Linhas[0].Select(c => (c ?? string.Empty).Trim()).ToList();
        for (var i = 1; i < aba.Linhas.Count; i++)
        {
            var row = ExtratoB3Materializador.MapearLinha(headers, aba.Linhas[i]);
            var provento = ExtratoB3Materializador.InterpretarProvento(row);
            if (provento is null)
                continue;

            var classe = ExtratoB3Materializador.ClassePorTicker(provento.Ticker);
            var ativo = ObterOuCriarAtivo(ativos, provento.Ticker, provento.Produto ?? provento.Ticker, classe, false, provento.Ticker);

            // Ativo recém-criado tem Id=0 até salvar; o provento referencia o AssetId por ESCALAR
            // (UpsertRendimento), então persistimos o ativo novo ANTES — senão o insert do
            // RendimentoInvestimento viola a FK FinanceiroRendimento→FinanceiroAtivo (AssetId=0).
            if (ativo.Id == 0)
                await _context.SaveChangesAsync(cancellationToken);

            UpsertRendimento(
                carga,
                documento,
                ativo.Id,
                provento.Pagamento,
                provento.Pagamento,
                provento.Tipo,
                "Extrato Consolidado B3",
                FonteExtratoB3,
                provento.Quantidade > 0m ? provento.Quantidade : null,
                provento.ValorPorAcao > 0m ? provento.ValorPorAcao : null,
                provento.Valor,
                0m,
                "BRL",
                string.Empty,
                JsonSerializer.Serialize(new { produto = provento.Produto, cells = aba.Linhas[i] }));
        }
    }
}
