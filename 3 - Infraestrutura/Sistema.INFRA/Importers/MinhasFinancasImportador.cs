using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Importers;

public partial class MinhasFinancasImportador(AppDbContext context, IConfiguration configuration, ILogger<MinhasFinancasImportador> logger) : IMinhasFinancasImportador
{
    private static readonly string[] CryptoQuotes = ["BRL", "USDT", "USDC", "FDUSD", "BTC", "ETH", "BNB"];

    private readonly AppDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MinhasFinancasImportador> _logger = logger;

    public async Task GarantirCargaInicialAsync(CancellationToken cancellationToken = default)
    {
        var source = ResolverFonteJson();
        if (source is null)
            return;

        var sha = Sha256(source.Value.JsonBytes);
        if (await _context.CargasFinanceiras.AnyAsync(x => x.JsonSha256 == sha, cancellationToken))
            return;

        await ImportarAsync(source.Value.JsonBytes, sha, source.Value.SourcePath, cancellationToken);
    }

    private async Task ImportarAsync(byte[] jsonBytes, string sha, string sourcePath, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(jsonBytes);
        var root = document.RootElement;
        var summaryJson = GetObjectRaw(root, "summary") ?? "{}";
        var dashboardJson = CarregarDashboardJson();

        var carga = new CargaFinanceira
        {
            SchemaVersion = GetString(root, "schemaVersion") ?? "unknown",
            JsonSha256 = sha,
            SourcePath = sourcePath,
            GeneratedAt = TryGetDate(GetProperty(root, "summary"), "generatedAt"),
            ImportedAt = DateTime.UtcNow,
            Status = StatusDocumentoFinanceiro.Processado,
            SummaryJson = summaryJson,
            DashboardJson = dashboardJson,
            UsuarioInclusao = "minhas-financas-importador"
        };

        await _context.CargasFinanceiras.AddAsync(carga, cancellationToken);
        var documentos = ImportarDocumentos(root, carga);
        var ativos = await CarregarAtivosAsync(cancellationToken);
        ImportarB3(root, carga, documentos, ativos);
        ImportarBinance(root, carga, documentos, ativos);
        ImportarAgregados(root, carga);
        ImportarDashboardData(carga, dashboardJson);
        ImportarAlertas(root, carga);

        await _context.SaveChangesAsync(cancellationToken);
        MinhasFinancasImportadorLogMessages.CargaFinanceiraImportada(_logger, sourcePath, sha);
    }

    private Dictionary<string, DocumentoFinanceiro> ImportarDocumentos(JsonElement root, CargaFinanceira carga)
    {
        var lookup = new Dictionary<string, DocumentoFinanceiro>(StringComparer.OrdinalIgnoreCase);
        var documents = GetProperty(root, "documents");
        ImportarColecaoDocumentos(GetProperty(documents, "root"), "root", carga, lookup);
        ImportarColecaoDocumentos(GetProperty(documents, "fromZip"), "fromZip", carga, lookup);
        return lookup;
    }

    private void ImportarColecaoDocumentos(JsonElement? docs, string colecao, CargaFinanceira carga, Dictionary<string, DocumentoFinanceiro> lookup)
    {
        if (docs is null || docs.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var doc in docs.Value.EnumerateArray())
        {
            var metadata = GetProperty(doc, "metadata");
            if (metadata is null)
                continue;

            var documento = new DocumentoFinanceiro
            {
                CargaFinanceira = carga,
                Colecao = colecao,
                Path = GetString(metadata.Value, "path") ?? string.Empty,
                FileName = GetString(metadata.Value, "fileName") ?? string.Empty,
                FileType = GetString(metadata.Value, "type") ?? string.Empty,
                Source = colecao,
                Sha256 = GetString(metadata.Value, "sha256") ?? string.Empty,
                SizeBytes = (long)(GetDecimal(metadata.Value, "sizeBytes") ?? 0m),
                ReferenceYear = ExtrairAno(GetString(metadata.Value, "path") ?? GetString(metadata.Value, "fileName")),
                PageCount = GetInt(metadata.Value, "pageCount"),
                Status = MapStatusDocumento(GetString(metadata.Value, "status")),
                RawMetadataJson = metadata.Value.GetRawText(),
                UsuarioInclusao = "minhas-financas-importador"
            };

            _context.DocumentosFinanceiros.Add(documento);
            RegistrarDocumentoLookup(lookup, documento);
            ImportarConteudosDocumento(doc, documento);
        }
    }

    private void ImportarConteudosDocumento(JsonElement doc, DocumentoFinanceiro documento)
    {
        var pages = GetProperty(doc, "pages");
        if (IsArray(pages))
        {
            foreach (var page in EnumerarArray(pages))
            {
                _context.ConteudosBrutosFinanceiros.Add(new ConteudoBrutoFinanceiro
                {
                    DocumentoFinanceiro = documento,
                    ContentType = TipoConteudoBrutoFinanceiro.TextoPagina,
                    PageNumber = GetInt(page, "page"),
                    RawText = GetString(page, "text"),
                    RawJson = page.GetRawText(),
                    UsuarioInclusao = "minhas-financas-importador"
                });
            }
        }

        var sheets = GetProperty(doc, "sheets");
        if (IsArray(sheets))
        {
            foreach (var sheet in EnumerarArray(sheets))
            {
                _context.ConteudosBrutosFinanceiros.Add(new ConteudoBrutoFinanceiro
                {
                    DocumentoFinanceiro = documento,
                    ContentType = TipoConteudoBrutoFinanceiro.Planilha,
                    SheetName = GetString(sheet, "sheetName"),
                    RawJson = sheet.GetRawText(),
                    UsuarioInclusao = "minhas-financas-importador"
                });
            }
        }
    }

    private async Task<Dictionary<string, AtivoFinanceiro>> CarregarAtivosAsync(CancellationToken cancellationToken)
    {
        var ativos = await _context.AtivosFinanceiros.ToListAsync(cancellationToken);
        return ativos.ToDictionary(x => x.AssetKey, StringComparer.OrdinalIgnoreCase);
    }

    private void ImportarB3(JsonElement root, CargaFinanceira carga, Dictionary<string, DocumentoFinanceiro> documentos, Dictionary<string, AtivoFinanceiro> ativos)
    {
        var b3 = GetProperty(root, "b3");
        if (b3 is null)
            return;

        var ops = GetProperty(b3.Value, "operationsDetailedRawIncludingDuplicateSources") ?? GetProperty(b3.Value, "operationsDetailed");
        if (IsArray(ops))
        {
            foreach (var op in EnumerarArray(ops))
            {
                var assetKey = GetString(op, "assetKey") ?? GetString(op, "assetTitle") ?? "SEM_ATIVO";
                var assetTitle = GetString(op, "assetTitle") ?? assetKey;
                var ativo = ObterOuCriarAtivo(ativos, assetKey, assetTitle, MapClasseAtivo(GetString(op, "assetClass")), false);
                var sourceFile = GetString(op, "sourceFile") ?? string.Empty;
                var isDuplicate = GetBool(op, "isDuplicateAcrossSources");

                _context.OperacoesB3.Add(new OperacaoB3
                {
                    CargaFinanceira = carga,
                    SourceDocument = ResolverDocumento(documentos, sourceFile),
                    TradeDate = TryParseDate(GetString(op, "tradeDate")),
                    NoteNumber = GetString(op, "noteNumber"),
                    PageNumber = GetInt(op, "page"),
                    Broker = GetString(op, "rawBroker") ?? string.Empty,
                    Market = GetString(op, "market") ?? string.Empty,
                    OperationType = MapOperacaoB3(GetString(op, "sideCode"), GetString(op, "side")),
                    Asset = ativo,
                    OriginalAssetName = assetTitle,
                    Quantity = GetDecimal(op, "quantity") ?? 0m,
                    UnitPrice = GetDecimal(op, "unitPrice") ?? 0m,
                    GrossAmount = GetDecimal(op, "grossValue") ?? 0m,
                    NetAmount = GetDecimal(op, "grossValue") ?? 0m,
                    DebitCredit = GetString(op, "debitCredit") ?? string.Empty,
                    IsCanonical = !isDuplicate,
                    DuplicateGroupKey = GetString(op, "duplicateGroupId"),
                    ConfidenceLevel = isDuplicate ? NivelConfianca.Baixa : NivelConfianca.Media,
                    SourceFile = sourceFile,
                    RawJson = op.GetRawText(),
                    UsuarioInclusao = "minhas-financas-importador"
                });
            }
        }

        var positions = GetProperty(GetProperty(b3.Value, "aggregates"), "byAssetEstimated");
        if (IsArray(positions))
        {
            foreach (var item in EnumerarArray(positions))
            {
                var assetKey = GetString(item, "assetKey") ?? "SEM_ATIVO";
                var assetTitle = GetString(item, "assetTitle") ?? assetKey;
                var ativo = ObterOuCriarAtivo(ativos, assetKey, assetTitle, MapClasseAtivo(GetString(item, "assetClass")), false);

                _context.EstimativasPosicaoCarteira.Add(new EstimativaPosicaoCarteira
                {
                    CargaFinanceira = carga,
                    Asset = ativo,
                    Quantity = GetDecimal(item, "netQuantity") ?? 0m,
                    AveragePrice = GetDecimal(item, "averageBuyPriceGross") ?? 0m,
                    TotalInvested = GetDecimal(item, "buyGrossValue") ?? 0m,
                    TotalSold = GetDecimal(item, "sellGrossValue") ?? 0m,
                    RealizedResult = (GetDecimal(item, "sellGrossValue") ?? 0m) - (GetDecimal(item, "buyGrossValue") ?? 0m),
                    EstimatedCurrentPosition = GetDecimal(item, "netInvestedGross") ?? 0m,
                    Status = MapStatusPosicao(GetString(item, "statusEstimate")),
                    ConfidenceLevel = NivelConfianca.PendenteValidacao,
                    LastOperationDate = TryParseDate(GetString(item, "lastDate")),
                    RawJson = item.GetRawText(),
                    UsuarioInclusao = "minhas-financas-importador"
                });
            }
        }
    }

    private void ImportarBinance(JsonElement root, CargaFinanceira carga, Dictionary<string, DocumentoFinanceiro> documentos, Dictionary<string, AtivoFinanceiro> ativos)
    {
        var binance = GetProperty(root, "binance");
        if (binance is null)
            return;

        ImportarTransacoesBinance(GetProperty(binance.Value, "transactions"), carga, documentos, ativos, "transaction");
        ImportarTransacoesBinance(GetProperty(binance.Value, "spotTrades"), carga, documentos, ativos, "spot");
        ImportarTransacoesBinance(GetProperty(binance.Value, "convertOrders"), carga, documentos, ativos, "convert");
        ImportarTransacoesBinance(GetProperty(binance.Value, "deposits"), carga, documentos, ativos, "deposit");
    }

    private void ImportarTransacoesBinance(JsonElement? rows, CargaFinanceira carga, Dictionary<string, DocumentoFinanceiro> documentos, Dictionary<string, AtivoFinanceiro> ativos, string tipo)
    {
        if (rows is null || rows.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var row in EnumerarArray(rows))
        {
            var source = GetString(row, "_source") ?? string.Empty;
            var symbol = tipo switch
            {
                "spot" => ExtrairMoedaBase(GetString(row, "Par")) ?? GetString(row, "Par") ?? string.Empty,
                "convert" => GetString(row, "Compra")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? GetString(row, "Par") ?? string.Empty,
                _ => GetString(row, "Moeda") ?? GetString(row, "Coin") ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(symbol) && tipo == "deposit" && (GetString(row, "Tempo") ?? string.Empty).Contains("Nenhum dado", StringComparison.OrdinalIgnoreCase))
                continue;

            var ativo = ObterOuCriarAtivo(ativos, symbol, symbol, ClasseAtivo.Cripto, true);
            var amount = tipo switch
            {
                "spot" => ExtrairDecimalInicial(GetString(row, "Executado")) ?? 0m,
                "convert" => ExtrairDecimalInicial(GetString(row, "Compra")) ?? 0m,
                _ => ParseDecimal(GetString(row, "Alterar") ?? GetString(row, "Change") ?? GetString(row, "Quantidade")) ?? 0m
            };

            _context.TransacoesCripto.Add(new TransacaoCripto
            {
                CargaFinanceira = carga,
                SourceDocument = ResolverDocumento(documentos, source),
                TransactionDate = TryParseDateTime(GetString(row, "Tempo") ?? GetString(row, "UTC_Time")),
                Exchange = "Binance",
                OperationType = MapOperacaoCripto(tipo, GetString(row, "Operação") ?? GetString(row, "Operation") ?? GetString(row, "Lado")),
                AssetSymbol = ativo.AssetKey,
                Pair = GetString(row, "Par"),
                Amount = amount,
                Price = ParseDecimal(GetString(row, "Preço")),
                Total = ExtrairDecimalInicial(GetString(row, "Quantidade")),
                FeeAsset = ExtrairMoedaFinal(GetString(row, "Taxa")),
                FeeAmount = ExtrairDecimalInicial(GetString(row, "Taxa")),
                RawType = GetString(row, "Operação") ?? GetString(row, "Operation") ?? tipo,
                SourceFile = source,
                RawJson = row.GetRawText(),
                UsuarioInclusao = "minhas-financas-importador"
            });
        }
    }

    private void ImportarAgregados(JsonElement root, CargaFinanceira carga)
    {
        var b3Aggregates = GetProperty(GetProperty(root, "b3"), "aggregates");
        ImportarAgregadoArray(GetProperty(b3Aggregates, "byYear"), carga, "b3-year", "year", "buyGrossValue", "sellGrossValue", "operationCount");
        ImportarAgregadoArray(GetProperty(b3Aggregates, "byMonth"), carga, "b3-month", "month", "buyGrossValue", "sellGrossValue", "operationCount");
        ImportarAgregadoArray(GetProperty(b3Aggregates, "byClass"), carga, "b3-class", "assetClass", "buyGrossValue", "sellGrossValue", "operationCount");

        var binanceAggregates = GetProperty(GetProperty(root, "binance"), "aggregates");
        var coins = GetProperty(binanceAggregates, "coinSummaryFromTransactions");
        if (IsArray(coins))
        {
            foreach (var coin in EnumerarArray(coins))
            {
                _context.AgregadosFinanceiros.Add(new AgregadoFinanceiro
                {
                    CargaFinanceira = carga,
                    Dimensao = "binance-coin",
                    Chave = GetString(coin, "coin") ?? string.Empty,
                    Saldo = GetDecimal(coin, "netChange"),
                    Quantidade = GetDecimal(coin, "netChange"),
                    Contagem = GetInt(coin, "rowCount"),
                    RawJson = coin.GetRawText(),
                    UsuarioInclusao = "minhas-financas-importador"
                });
            }
        }
    }

    private void ImportarAgregadoArray(JsonElement? rows, CargaFinanceira carga, string dimensao, string keyField, string buyField, string sellField, string countField)
    {
        if (rows is null || rows.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var row in EnumerarArray(rows))
        {
            var chave = GetString(row, keyField) ?? GetInt(row, keyField)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            var compras = GetDecimal(row, buyField) ?? 0m;
            var vendas = GetDecimal(row, sellField) ?? 0m;
            _context.AgregadosFinanceiros.Add(new AgregadoFinanceiro
            {
                CargaFinanceira = carga,
                Dimensao = dimensao,
                Chave = chave,
                Ano = dimensao == "b3-year" ? GetInt(row, keyField) : null,
                Mes = dimensao == "b3-month" ? chave : null,
                ClasseAtivo = dimensao == "b3-class" ? MapClasseAtivo(chave) : null,
                ValorCompra = compras,
                ValorVenda = vendas,
                Saldo = compras - vendas,
                Contagem = GetInt(row, countField),
                RawJson = row.GetRawText(),
                UsuarioInclusao = "minhas-financas-importador"
            });
        }
    }

    private void ImportarDashboardData(CargaFinanceira carga, string? dashboardJson)
    {
        if (string.IsNullOrWhiteSpace(dashboardJson))
            return;

        using var doc = JsonDocument.Parse(dashboardJson);
        var root = doc.RootElement;
        var incomes = GetProperty(root, "incomes");
        if (IsArray(incomes))
        {
            foreach (var income in EnumerarArray(incomes))
            {
                _context.RendimentosInvestimento.Add(new RendimentoInvestimento
                {
                    CargaFinanceira = carga,
                    IncomeType = GetString(income, "tipo") ?? string.Empty,
                    Source = GetString(income, "fonte") ?? string.Empty,
                    Amount = GetDecimal(income, "valor") ?? 0m,
                    Taxation = GetString(income, "tributacao") ?? string.Empty,
                    RawJson = income.GetRawText(),
                    UsuarioInclusao = "minhas-financas-importador"
                });
            }
        }
    }

    private void ImportarAlertas(JsonElement root, CargaFinanceira carga)
    {
        var validation = GetProperty(root, "validation");
        ImportarAlertasArray(GetProperty(validation, "warnings"), carga, "VALIDATION_WARNING", SeveridadeAlerta.Atencao);
        ImportarAlertasArray(GetProperty(validation, "suggestedNextSteps"), carga, "SUGGESTED_NEXT_STEP", SeveridadeAlerta.Informacao);

        _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
        {
            CargaFinanceira = carga,
            EntityType = "Dashboard",
            Severity = SeveridadeAlerta.Informacao,
            Code = "NO_INVESTMENT_ADVICE",
            Message = "As análises financeiras são informativas e não constituem recomendação de investimento.",
            Details = "As posições exibidas são estimadas quando não houver reconciliação completa com custódia atual.",
            UsuarioInclusao = "minhas-financas-importador"
        });
    }

    private void ImportarAlertasArray(JsonElement? rows, CargaFinanceira carga, string code, SeveridadeAlerta severidade)
    {
        if (rows is null || rows.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var row in EnumerarArray(rows))
        {
            _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
            {
                CargaFinanceira = carga,
                EntityType = "CargaFinanceira",
                Severity = severidade,
                Code = code,
                Message = row.ValueKind == JsonValueKind.String ? row.GetString() ?? string.Empty : row.GetRawText(),
                UsuarioInclusao = "minhas-financas-importador"
            });
        }
    }

    private AtivoFinanceiro ObterOuCriarAtivo(Dictionary<string, AtivoFinanceiro> ativos, string assetKey, string name, ClasseAtivo classe, bool isCrypto)
    {
        assetKey = string.IsNullOrWhiteSpace(assetKey) ? "SEM_ATIVO" : assetKey.Trim();
        if (ativos.TryGetValue(assetKey, out var ativo))
            return ativo;

        ativo = new AtivoFinanceiro
        {
            AssetKey = assetKey,
            Name = string.IsNullOrWhiteSpace(name) ? assetKey : name.Trim(),
            AssetClass = classe,
            Market = isCrypto ? "Binance" : "B3",
            Currency = isCrypto ? "USD/BRL" : "BRL",
            IsCrypto = isCrypto,
            ConceptRole = isCrypto ? MapPapelCripto(assetKey) : null,
            UsuarioInclusao = "minhas-financas-importador"
        };

        _context.AtivosFinanceiros.Add(ativo);
        ativos[assetKey] = ativo;
        return ativo;
    }

    private static DocumentoFinanceiro? ResolverDocumento(Dictionary<string, DocumentoFinanceiro> documentos, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var fileName = Path.GetFileName(source);
        return documentos.TryGetValue(source, out var exact) ? exact : documentos.GetValueOrDefault(fileName);
    }

    private static void RegistrarDocumentoLookup(Dictionary<string, DocumentoFinanceiro> lookup, DocumentoFinanceiro documento)
    {
        if (!string.IsNullOrWhiteSpace(documento.Path))
            lookup.TryAdd(documento.Path, documento);
        if (!string.IsNullOrWhiteSpace(documento.FileName))
            lookup.TryAdd(documento.FileName, documento);
    }

    private (byte[] JsonBytes, string SourcePath)? ResolverFonteJson()
    {
        var zipPath = _configuration["MinhasFinancas:SeedZipPath"];
        if (!string.IsNullOrWhiteSpace(zipPath) && File.Exists(zipPath))
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.Entries
                .Where(e => e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName.Contains("_min", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(e => e.Length)
                .FirstOrDefault();

            if (entry is not null)
            {
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return (ms.ToArray(), $"{zipPath}::{entry.FullName}");
            }
        }

        var jsonPath = _configuration["MinhasFinancas:SeedJsonPath"];
        return !string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath)
            ? (File.ReadAllBytes(jsonPath), jsonPath)
            : null;
    }

    private string? CarregarDashboardJson()
    {
        var htmlPath = _configuration["MinhasFinancas:DashboardHtmlPath"];
        if (string.IsNullOrWhiteSpace(htmlPath) || !File.Exists(htmlPath))
            return null;

        var html = File.ReadAllText(htmlPath, Encoding.UTF8);
        var match = DashboardDataRegex().Match(html);
        return match.Success ? match.Groups["json"].Value : null;
    }

    private static string Sha256(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static bool IsArray(JsonElement? element)
        => element.HasValue && element.Value.ValueKind == JsonValueKind.Array;

    private static IEnumerable<JsonElement> EnumerarArray(JsonElement? element)
    {
        if (!element.HasValue || element.Value.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<JsonElement>();

        return element.GetValueOrDefault().EnumerateArray();
    }

    private static JsonElement? GetProperty(JsonElement? element, string name)
    {
        if (!element.HasValue || element.Value.ValueKind != JsonValueKind.Object)
            return null;

        return element.Value.TryGetProperty(name, out var value) ? value : null;
    }

    private static JsonElement? GetProperty(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;

    private static string? GetObjectRaw(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value.GetRawText() : null;

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? GetInt(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : null;

    private static decimal? GetDecimal(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : ParseDecimal(value.ValueKind == JsonValueKind.String ? value.GetString() : null);
    }

    private static bool GetBool(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim().Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        var match = DecimalPrefixRegex().Match(text);
        if (match.Success)
            text = match.Value;

        if (text.Contains(','))
            text = text.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');

        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static decimal? ExtrairDecimalInicial(string? value)
        => ParseDecimal(value);

    private static string? ExtrairMoedaFinal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var match = Regex.Match(value.Trim(), @"[A-Za-z]+$");
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static string? ExtrairMoedaBase(string? pair)
    {
        if (string.IsNullOrWhiteSpace(pair))
            return null;
        var quote = CryptoQuotes.FirstOrDefault(pair.EndsWith);
        return quote is null ? pair : pair[..^quote.Length];
    }

    private static DateTime? TryGetDate(JsonElement? element, string name)
        => element is null ? null : TryParseDateTime(GetString(element.Value, name));

    private static DateTime? TryParseDate(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed.Date : null;

    private static DateTime? TryParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string[] formats = ["yy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-dd"];
        return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed) ? parsed : null;
    }

    private static int? ExtrairAno(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var match = Regex.Match(value, @"20\d{2}");
        return match.Success && int.TryParse(match.Value, out var year) ? year : null;
    }

    private static ClasseAtivo MapClasseAtivo(string? value)
    {
        var text = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (text.Contains("FII")) return ClasseAtivo.FII;
        if (text.Contains("BDR")) return ClasseAtivo.BDR;
        if (text.Contains("ETF")) return ClasseAtivo.ETF;
        if (text.Contains("CRIPTO")) return ClasseAtivo.Cripto;
        if (text.Contains("RENDA FIXA") || text.Contains("TESOURO") || text.Contains("CDB") || text.Contains("RDB")) return ClasseAtivo.RendaFixa;
        if (text.Contains("CAIXA") || text.Contains("CONTA")) return ClasseAtivo.Caixa;
        if (text.Contains("AÇÕES") || text.Contains("ACOES")) return ClasseAtivo.Acao;
        return ClasseAtivo.Outro;
    }

    private static TipoOperacaoFinanceira MapOperacaoB3(string? sideCode, string? side)
        => string.Equals(sideCode, "C", StringComparison.OrdinalIgnoreCase) || string.Equals(side, "Compra", StringComparison.OrdinalIgnoreCase)
            ? TipoOperacaoFinanceira.Compra
            : string.Equals(sideCode, "V", StringComparison.OrdinalIgnoreCase) || string.Equals(side, "Venda", StringComparison.OrdinalIgnoreCase)
                ? TipoOperacaoFinanceira.Venda
                : TipoOperacaoFinanceira.Outro;

    private static TipoOperacaoFinanceira MapOperacaoCripto(string tipo, string? raw)
    {
        var text = $"{tipo} {raw}".ToUpperInvariant();
        if (text.Contains("DEPOSIT")) return TipoOperacaoFinanceira.Deposito;
        if (text.Contains("WITHDRAW")) return TipoOperacaoFinanceira.Saque;
        if (text.Contains("CONVERT")) return TipoOperacaoFinanceira.Conversao;
        if (text.Contains("BUY") || text.Contains("COMPRA")) return TipoOperacaoFinanceira.Compra;
        if (text.Contains("SELL") || text.Contains("VENDA")) return TipoOperacaoFinanceira.Venda;
        if (text.Contains("FEE") || text.Contains("TAXA")) return TipoOperacaoFinanceira.Taxa;
        if (text.Contains("INTEREST") || text.Contains("REWARD")) return TipoOperacaoFinanceira.Rendimento;
        return TipoOperacaoFinanceira.Trade;
    }

    private static StatusDocumentoFinanceiro MapStatusDocumento(string? status)
        => string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase) ? StatusDocumentoFinanceiro.Processado : StatusDocumentoFinanceiro.PendenteValidacao;

    private static StatusEstimativaPosicao MapStatusPosicao(string? status)
        => string.Equals(status, "closed_by_operations", StringComparison.OrdinalIgnoreCase)
            ? StatusEstimativaPosicao.EncerradaPorOperacoes
            : string.Equals(status, "open_or_residual", StringComparison.OrdinalIgnoreCase)
                ? StatusEstimativaPosicao.AbertaOuResidual
                : StatusEstimativaPosicao.PendenteValidacao;

    private static string? MapPapelCripto(string assetKey)
        => assetKey.ToUpperInvariant() switch
        {
            "BTC" or "WBTC" => "Principal",
            "ETH" or "WBETH" => "Principal",
            "SOL" or "BNSOL" => "Principal",
            "XRP" => "Satelite",
            "BNB" => "Taxas Binance",
            "DOGE" => "Memecoin/aposta de ciclo",
            _ => null
        };

    [GeneratedRegex(@"const\s+DASHBOARD_DATA\s*=\s*(?<json>\{.*?\});\s*\$\(function", RegexOptions.Singleline)]
    private static partial Regex DashboardDataRegex();

    [GeneratedRegex(@"[-+]?\d+(?:[\.,]\d+)?")]
    private static partial Regex DecimalPrefixRegex();
}

internal static partial class MinhasFinancasImportadorLogMessages
{
    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Carga financeira importada. Fonte={Fonte}, Sha={Sha}")]
    public static partial void CargaFinanceiraImportada(ILogger logger, string fonte, string sha);
}
