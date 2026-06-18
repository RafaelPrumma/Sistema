using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;
using Sistema.INFRA.Services;
using UglyToad.PdfPig;

namespace Sistema.INFRA.Importers;

public partial class FinancasImportador(AppDbContext context, IConfiguration configuration, IConfiguracaoLeitura config, IHostEnvironment hostEnvironment, ILogger<FinancasImportador> logger, FinancasDataRepairService repairService) : IFinancasImportador
{
    private static readonly string[] CryptoQuotes = ["BRL", "USDT", "USDC", "FDUSD", "BTC", "ETH", "BNB"];
    private const string ParserVersion = "financeiro-v1";

    private readonly AppDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IConfiguracaoLeitura _config = config;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly ILogger<FinancasImportador> _logger = logger;
    private readonly FinancasDataRepairService _repairService = repairService;

    public async Task GarantirCargaInicialAsync(CancellationToken cancellationToken = default)
    {
        var source = ResolverFonteJson();
        if (source is not null)
        {
            var sha = Sha256(source.Value.JsonBytes);
            if (!await _context.CargasFinanceiras.AnyAsync(x => x.JsonSha256 == sha, cancellationToken))
                await ImportarAsync(source.Value.JsonBytes, sha, source.Value.SourcePath, cancellationToken);
        }

        await _repairService.RepararAsync(cancellationToken);
        await GarantirCarteirasPadraoAsync(cancellationToken);

        var watchedFolder = await _config.ObterTextoAsync("Financas", "WatchedFolderPath", null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(watchedFolder)
            && Directory.Exists(watchedFolder)
            && !await _context.ImportacoesFinanceirasArquivo.AnyAsync(x => x.SourceFolder == watchedFolder, cancellationToken))
        {
            await ImportarPastaMonitoradaAsync(cancellationToken);
        }

        // Garante que a tabela única reflita a regra de materialização atual.
        // Barato quando já está na versão certa; faz o resync completo quando a regra muda.
        await RessincronizarPorVersaoAsync(cancellationToken);
    }

    public async Task ImportarPastaMonitoradaAsync(CancellationToken cancellationToken = default)
    {
        var watchedFolder = await _config.ObterTextoAsync("Financas", "WatchedFolderPath", null, cancellationToken);
        if (string.IsNullOrWhiteSpace(watchedFolder) || !Directory.Exists(watchedFolder))
            return;

        var files = Directory.EnumerateFiles(watchedFolder)
            .Where(IsSupportedFile)
            .OrderBy(Path.GetFileName)
            .ToList();

        var carga = await _context.CargasFinanceiras.OrderByDescending(x => x.ImportedAt).FirstOrDefaultAsync(cancellationToken);
        if (carga is null)
        {
            carga = new CargaFinanceira
            {
                SchemaVersion = "folder-v1",
                JsonSha256 = Sha256(Encoding.UTF8.GetBytes(watchedFolder)),
                SourcePath = watchedFolder,
                ImportedAt = DateTime.UtcNow,
                Status = StatusDocumentoFinanceiro.Processado,
                SummaryJson = "{}",
                UsuarioInclusao = "financas-importador"
            };
            _context.CargasFinanceiras.Add(carga);
        }

        var batch = new ImportacaoFinanceiraArquivo
        {
            SourceFolder = watchedFolder,
            StartedAt = DateTime.UtcNow,
            FilesDiscovered = files.Count,
            UsuarioInclusao = "financas-importador"
        };
        _context.ImportacoesFinanceirasArquivo.Add(batch);

        var ativos = await CarregarAtivosAsync(cancellationToken);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessarZipMonitoradoAsync(file, carga, batch, ativos, cancellationToken);
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            var sha = Sha256(bytes);
            if (await _context.DocumentosFinanceiros.AnyAsync(x => x.Sha256 == sha, cancellationToken))
            {
                batch.FilesSkipped++;
                continue;
            }

            var documentKind = ClassificarDocumento(file);
            var documento = new DocumentoFinanceiro
            {
                CargaFinanceira = carga,
                ImportacaoFinanceiraArquivo = batch,
                Colecao = "watched-folder",
                Path = file,
                StoredPath = file,
                FileName = Path.GetFileName(file),
                FileType = Path.GetExtension(file).TrimStart('.').ToLowerInvariant(),
                Source = "watched-folder",
                Sha256 = sha,
                SizeBytes = bytes.LongLength,
                ReferenceYear = ExtrairAno(file),
                DocumentKind = documentKind,
                ParserVersion = ParserVersion,
                ParseStatus = StatusParseDocumentoFinanceiro.Pendente,
                Status = StatusDocumentoFinanceiro.Importado,
                RawMetadataJson = JsonSerializer.Serialize(new { path = file, sha256 = sha, documentKind }),
                UsuarioInclusao = "financas-importador"
            };
            _context.DocumentosFinanceiros.Add(documento);

            var structuredRows = await ProcessarDocumentoMonitoradoAsync(documento, file, carga, ativos, cancellationToken);
            batch.StructuredRowsImported += structuredRows;
            batch.FilesImported++;
        }

        await GarantirCarteirasPadraoAsync(cancellationToken);
        batch.FinishedAt = DateTime.UtcNow;
        batch.Status = batch.FilesImported == 0
            ? StatusImportacaoFinanceira.Concluida
            : batch.StructuredRowsImported == 0 ? StatusImportacaoFinanceira.ConcluidaComAlertas : StatusImportacaoFinanceira.Concluida;

        await _context.SaveChangesAsync(cancellationToken);
        await SincronizarTransacoesCanonicasAsync(cancellationToken);
    }

    private async Task ProcessarZipMonitoradoAsync(string zipFile, CargaFinanceira carga, ImportacaoFinanceiraArquivo batch, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(zipFile);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var importouArquivo = false;

        foreach (var entry in zip.Entries.Where(e => e.Length > 0 && IsSupportedFile(e.FullName) && !Path.GetExtension(e.FullName).Equals(".zip", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();
            var sha = Sha256(bytes);
            if (await _context.DocumentosFinanceiros.AnyAsync(x => x.Sha256 == sha, cancellationToken))
            {
                batch.FilesSkipped++;
                continue;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "SistemaFinancasImport");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid():N}{Path.GetExtension(entry.Name)}");
            await File.WriteAllBytesAsync(tempFile, bytes, cancellationToken);

            try
            {
                var sourcePath = $"{zipFile}!{entry.FullName}";
                var documentKind = ClassificarDocumento(sourcePath);
                var documento = new DocumentoFinanceiro
                {
                    CargaFinanceira = carga,
                    ImportacaoFinanceiraArquivo = batch,
                    Colecao = "watched-folder-zip",
                    Path = sourcePath,
                    StoredPath = zipFile,
                    FileName = entry.Name,
                    FileType = Path.GetExtension(entry.Name).TrimStart('.').ToLowerInvariant(),
                    Source = "watched-folder-zip",
                    Sha256 = sha,
                    SizeBytes = bytes.LongLength,
                    ReferenceYear = ExtrairAno(sourcePath),
                    DocumentKind = documentKind,
                    ParserVersion = ParserVersion,
                    ParseStatus = StatusParseDocumentoFinanceiro.Pendente,
                    Status = StatusDocumentoFinanceiro.Importado,
                    RawMetadataJson = JsonSerializer.Serialize(new { zipFile, entry = entry.FullName, sha256 = sha, documentKind }),
                    UsuarioInclusao = "financas-importador"
                };
                _context.DocumentosFinanceiros.Add(documento);

                var structuredRows = await ProcessarDocumentoMonitoradoAsync(documento, tempFile, carga, ativos, cancellationToken);
                batch.StructuredRowsImported += structuredRows;
                batch.FilesImported++;
                importouArquivo = true;
            }
            finally
            {
                TryDeleteTempFile(tempFile);
            }
        }

        if (!importouArquivo)
            batch.FilesSkipped++;
    }

    // Versão da regra de materialização. Ao incrementar, o resync apaga as transações de
    // importação e refaz a partir do staging com a regra nova (corrige cargas antigas sozinho).
    private const int MaterializacaoVersao = 5;

    // Materializa o staging bruto na tabela única TransacaoFinanceira (fonte de verdade).
    // B3: todas as notas canônicas (têm preço). Cripto: apenas negócios reais com preço
    // (spot trades e converts) — o restante do ledger da Binance (Simple Earn, staking, juros,
    // airdrops, taxas e o detalhamento sem preço dos trades) fica no staging para auditoria,
    // mas não polui a carteira. Idempotente por "{StagingTipo}#{StagingId}".
    private async Task SincronizarTransacoesCanonicasAsync(CancellationToken cancellationToken)
    {
        var jaMaterializadas = new HashSet<string>(
            await _context.TransacoesFinanceiras
                .Where(x => x.Origem == OrigemTransacao.Importacao && x.DuplicateGroupKey != null)
                .Select(x => x.DuplicateGroupKey!)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);

        // Chaves naturais já existentes: a mesma transação não entra duas vezes, mesmo vindo de
        // arquivos diferentes (ex.: reimportar junho parcial e depois junho completo).
        var chavesNaturais = new HashSet<string>(
            await _context.TransacoesFinanceiras
                .Where(x => x.Origem == OrigemTransacao.Importacao && x.ChaveNatural != null)
                .Select(x => x.ChaveNatural!)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);

        var novas = new List<TransacaoFinanceira>();

        var ops = await _context.OperacoesB3
            .Where(x => x.IsCanonical && x.AssetId != null)
            .ToListAsync(cancellationToken);
        foreach (var op in ops)
        {
            var chave = $"OperacaoB3#{op.Id}";
            if (!jaMaterializadas.Add(chave))
                continue;

            var quantidade = Math.Abs(op.Quantity);
            // Notas B3 não têm horário por trade: a chave usa a data (sem hora).
            var chaveNatural = op.TradeDate.HasValue
                ? GerarChaveNatural("Nubank B3", op.AssetId!.Value, op.TradeDate.Value, op.OperationType, quantidade, op.UnitPrice)
                : null;
            if (chaveNatural != null && !chavesNaturais.Add(chaveNatural))
                continue;

            novas.Add(new TransacaoFinanceira
            {
                Origem = OrigemTransacao.Importacao,
                AssetId = op.AssetId!.Value,
                Date = (op.TradeDate ?? op.DataInclusao).Date,
                OperationType = op.OperationType,
                Quantity = quantidade,
                UnitPrice = op.UnitPrice,
                GrossAmount = op.GrossAmount,
                Fees = op.Fees,
                Currency = "BRL",
                Broker = string.IsNullOrWhiteSpace(op.Broker) ? "NU Invest" : op.Broker,
                Fonte = "Nubank B3",
                SourceDocumentId = op.SourceDocumentId,
                CargaFinanceiraId = op.CargaFinanceiraId,
                StagingTipo = "OperacaoB3",
                StagingId = op.Id,
                DuplicateGroupKey = chave,
                ChaveNatural = chaveNatural,
                IsCanonical = true,
                ConfidenceLevel = op.ConfidenceLevel,
                RawJson = "{}",
                UsuarioInclusao = "financas-importador"
            });
        }

        var cryptos = await _context.TransacoesCripto
            .Where(x => x.Price != null && x.Price > 0m)
            .ToListAsync(cancellationToken);
        if (cryptos.Count > 0)
        {
            var assetIdPorChave = (await _context.AtivosFinanceiros
                    .Where(a => a.IsCrypto)
                    .ToListAsync(cancellationToken))
                .GroupBy(a => a.AssetKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            foreach (var tx in cryptos)
            {
                var chave = $"TransacaoCripto#{tx.Id}";
                if (!jaMaterializadas.Add(chave))
                    continue;
                if (string.IsNullOrWhiteSpace(tx.AssetSymbol) || !assetIdPorChave.TryGetValue(tx.AssetSymbol, out var assetId))
                    continue;

                var quantidade = Math.Abs(tx.Amount);
                if (quantidade <= 0m)
                    continue;

                var preco = tx.Price!.Value;
                var tipo = tx.OperationType is TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque
                    ? TipoOperacaoFinanceira.Venda
                    : TipoOperacaoFinanceira.Compra;

                // Cripto da Binance tem o horário exato (até o segundo): a chave usa data+hora.
                var chaveNatural = tx.TransactionDate.HasValue
                    ? GerarChaveNatural("Binance", assetId, tx.TransactionDate.Value, tipo, quantidade, preco)
                    : null;
                if (chaveNatural != null && !chavesNaturais.Add(chaveNatural))
                    continue;

                novas.Add(new TransacaoFinanceira
                {
                    Origem = OrigemTransacao.Importacao,
                    AssetId = assetId,
                    Date = tx.TransactionDate ?? tx.DataInclusao,
                    OperationType = tipo,
                    Quantity = quantidade,
                    UnitPrice = preco,
                    GrossAmount = tx.Total ?? quantidade * preco,
                    Fees = tx.FeeAmount ?? 0m,
                    Currency = "BRL",
                    Broker = string.IsNullOrWhiteSpace(tx.Exchange) ? "Binance" : tx.Exchange,
                    Fonte = "Binance",
                    SourceDocumentId = tx.SourceDocumentId,
                    CargaFinanceiraId = tx.CargaFinanceiraId,
                    StagingTipo = "TransacaoCripto",
                    StagingId = tx.Id,
                    DuplicateGroupKey = chave,
                    ChaveNatural = chaveNatural,
                    IsCanonical = true,
                    ConfidenceLevel = NivelConfianca.Media,
                    RawJson = "{}",
                    UsuarioInclusao = "financas-importador"
                });
            }
        }

        if (novas.Count > 0)
        {
            await _context.TransacoesFinanceiras.AddRangeAsync(novas, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Chave natural: identifica a transação pelo que ela é (independe do arquivo de origem).
    // B3 entra com a data (sem hora); cripto entra com data+hora (a Binance dá o segundo exato).
    private static string GerarChaveNatural(string fonte, int assetId, DateTime dataHora, TipoOperacaoFinanceira tipo, decimal quantidade, decimal preco)
    {
        var inv = CultureInfo.InvariantCulture;
        return $"{fonte}|{assetId}|{dataHora.ToString("yyyyMMddHHmmss", inv)}|{(int)tipo}|{quantidade.ToString("0.########", inv)}|{preco.ToString("0.############", inv)}";
    }

    // Apaga e refaz as transações de importação quando a regra de materialização muda de versão.
    private async Task RessincronizarPorVersaoAsync(CancellationToken cancellationToken)
    {
        const string agrupamento = "Financas";
        const string chave = "MaterializacaoVersao";
        var config = await _context.Configuracoes
            .FirstOrDefaultAsync(x => x.Agrupamento == agrupamento && x.Chave == chave, cancellationToken);
        var versaoAtual = int.TryParse(config?.Valor, out var v) ? v : 0;
        if (versaoAtual == MaterializacaoVersao)
            return;

        await _context.TransacoesFinanceiras
            .IgnoreQueryFilters()
            .Where(x => x.Origem == OrigemTransacao.Importacao)
            .ExecuteDeleteAsync(cancellationToken);

        if (config is null)
        {
            _context.Configuracoes.Add(new Configuracao
            {
                Agrupamento = agrupamento,
                Chave = chave,
                Valor = MaterializacaoVersao.ToString(CultureInfo.InvariantCulture),
                Descricao = "Versão interna da materialização de transações financeiras.",
                Ativo = true,
                UsuarioInclusao = "financas-importador"
            });
        }
        else
        {
            config.Valor = MaterializacaoVersao.ToString(CultureInfo.InvariantCulture);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await SincronizarTransacoesCanonicasAsync(cancellationToken);
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
            UsuarioInclusao = "financas-importador"
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
        await SincronizarTransacoesCanonicasAsync(cancellationToken);
        FinancasImportadorLogMessages.CargaFinanceiraImportada(_logger, sourcePath, sha);
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
                UsuarioInclusao = "financas-importador"
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
                    UsuarioInclusao = "financas-importador"
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
                    UsuarioInclusao = "financas-importador"
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
                var assetKeyRaw = GetString(op, "assetKey") ?? GetString(op, "assetTitle") ?? "SEM_ATIVO";
                var assetTitle = GetString(op, "assetTitle") ?? assetKeyRaw;
                var canonica = NormalizadorAtivoB3.ChaveCanonica(assetTitle);
                var alias = NormalizadorAtivoB3.Normalizar(assetTitle) ?? NormalizadorAtivoB3.Normalizar(assetKeyRaw);
                var assetKey = alias?.Ticker ?? canonica;
                var ativo = ObterOuCriarAtivo(ativos, assetKey, canonica, alias?.Classe ?? MapClasseAtivo(GetString(op, "assetClass")), false, alias?.Ticker);
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
                    UsuarioInclusao = "financas-importador"
                });
            }
        }

        var positions = GetProperty(GetProperty(b3.Value, "aggregates"), "byAssetEstimated");
        if (IsArray(positions))
        {
            foreach (var item in EnumerarArray(positions))
            {
                var assetKeyRaw = GetString(item, "assetKey") ?? "SEM_ATIVO";
                var assetTitle = GetString(item, "assetTitle") ?? assetKeyRaw;
                var canonica = NormalizadorAtivoB3.ChaveCanonica(assetTitle);
                var alias = NormalizadorAtivoB3.Normalizar(assetTitle) ?? NormalizadorAtivoB3.Normalizar(assetKeyRaw);
                var assetKey = alias?.Ticker ?? canonica;
                var ativo = ObterOuCriarAtivo(ativos, assetKey, canonica, alias?.Classe ?? MapClasseAtivo(GetString(item, "assetClass")), false, alias?.Ticker);

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
                    UsuarioInclusao = "financas-importador"
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
                UsuarioInclusao = "financas-importador"
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
                    UsuarioInclusao = "financas-importador"
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
                UsuarioInclusao = "financas-importador"
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
                    UsuarioInclusao = "financas-importador"
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
            UsuarioInclusao = "financas-importador"
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
                UsuarioInclusao = "financas-importador"
            });
        }
    }

    private async Task<int> ProcessarDocumentoMonitoradoAsync(DocumentoFinanceiro documento, string file, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        try
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".csv")
                return await ProcessarCsvAsync(documento, file, carga, ativos, cancellationToken);

            if (ext == ".xlsx")
                return await ProcessarXlsxAsync(documento, file, carga, ativos, cancellationToken);

            if (ext == ".pdf")
                return await ProcessarPdfAsync(documento, file, carga, ativos, cancellationToken);

            documento.ParseStatus = StatusParseDocumentoFinanceiro.SemDadosEstruturados;
            return 0;
        }
        catch (Exception ex)
        {
            documento.ParseStatus = StatusParseDocumentoFinanceiro.Falhou;
            _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
            {
                CargaFinanceira = carga,
                EntityType = nameof(DocumentoFinanceiro),
                EntityId = documento.Id,
                Severity = SeveridadeAlerta.Critico,
                Code = "DOCUMENT_PARSE_FAILED",
                Message = $"Falha ao processar {documento.FileName}.",
                Details = ex.Message,
                UsuarioInclusao = "financas-importador"
            });
            return 0;
        }
    }

    private async Task<int> ProcessarCsvAsync(DocumentoFinanceiro documento, string file, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(file, Encoding.UTF8, cancellationToken);
        if (lines.Length == 0)
        {
            documento.ParseStatus = StatusParseDocumentoFinanceiro.SemDadosEstruturados;
            return 0;
        }

        var headers = SplitCsvLine(lines[0]);
        var imported = 0;
        for (var i = 1; i < lines.Length; i++)
        {
            var values = SplitCsvLine(lines[i]);
            var row = ToRowDictionary(headers, values);
            if (row.Count == 0)
                continue;

            _context.ConteudosBrutosFinanceiros.Add(new ConteudoBrutoFinanceiro
            {
                DocumentoFinanceiro = documento,
                ContentType = TipoConteudoBrutoFinanceiro.LinhaPlanilha,
                SheetName = "csv",
                RowNumber = i + 1,
                RawJson = JsonSerializer.Serialize(row),
                UsuarioInclusao = "financas-importador"
            });

            imported += ImportarTransacaoBinanceRow(row, documento, carga, ativos, "transaction");
        }

        documento.ParseStatus = imported > 0 ? StatusParseDocumentoFinanceiro.Processado : StatusParseDocumentoFinanceiro.SemDadosEstruturados;
        return imported;
    }

    private async Task<int> ProcessarXlsxAsync(DocumentoFinanceiro documento, string file, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(file);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry is null)
        {
            documento.ParseStatus = StatusParseDocumentoFinanceiro.SemDadosEstruturados;
            return 0;
        }

        using var reader = new StreamReader(sheetEntry.Open(), Encoding.UTF8);
        var xml = await reader.ReadToEndAsync(cancellationToken);
        var rows = ExtrairLinhasXlsx(xml);
        var headerIndex = rows.FindIndex(IsHeaderRow);
        if (headerIndex < 0)
        {
            documento.ParseStatus = StatusParseDocumentoFinanceiro.SemDadosEstruturados;
            return 0;
        }

        var headers = rows[headerIndex].Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var tipo = TipoBinance(documento.DocumentKind);
        var imported = 0;
        for (var i = headerIndex + 1; i < rows.Count; i++)
        {
            var values = rows[i].Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (values.Count == 0)
                continue;

            var row = ToRowDictionary(headers, values);
            if (row.Count == 0)
                continue;

            _context.ConteudosBrutosFinanceiros.Add(new ConteudoBrutoFinanceiro
            {
                DocumentoFinanceiro = documento,
                ContentType = TipoConteudoBrutoFinanceiro.LinhaPlanilha,
                SheetName = "Sheet0",
                RowNumber = i + 1,
                RawJson = JsonSerializer.Serialize(row),
                UsuarioInclusao = "financas-importador"
            });

            imported += ImportarTransacaoBinanceRow(row, documento, carga, ativos, tipo);
        }

        documento.ParseStatus = imported > 0 ? StatusParseDocumentoFinanceiro.Processado : StatusParseDocumentoFinanceiro.SemDadosEstruturados;
        return imported;
    }

    private async Task<int> ProcessarPdfAsync(DocumentoFinanceiro documento, string file, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        var imported = 0;
        try
        {
            using var pdf = await AbrirPdfAsync(file, cancellationToken);
            documento.PageCount = pdf.NumberOfPages;
            foreach (var page in pdf.GetPages())
            {
                var text = page.Text ?? string.Empty;
                _context.ConteudosBrutosFinanceiros.Add(new ConteudoBrutoFinanceiro
                {
                    DocumentoFinanceiro = documento,
                    ContentType = TipoConteudoBrutoFinanceiro.TextoPagina,
                    PageNumber = page.Number,
                    RawText = text,
                    UsuarioInclusao = "financas-importador"
                });

                if (documento.DocumentKind == TipoDocumentoFinanceiro.NotaNegociacaoB3)
                    imported += ImportarOperacoesB3Pdf(text, documento, carga, ativos, page.Number);
                else if (documento.DocumentKind == TipoDocumentoFinanceiro.InformeRendimentos)
                    imported += ImportarInformeRendimentosPdf(text, documento, carga, ativos, page.Number);
            }
        }
        catch (Exception ex)
        {
            documento.ParseStatus = StatusParseDocumentoFinanceiro.Falhou;
            _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
            {
                CargaFinanceira = carga,
                EntityType = nameof(DocumentoFinanceiro),
                Severity = SeveridadeAlerta.Critico,
                Code = "PDF_TEXT_EXTRACTION_FAILED",
                Message = $"Não foi possível extrair texto do PDF {documento.FileName}.",
                Details = ex.Message,
                UsuarioInclusao = "financas-importador"
            });
            return 0;
        }

        documento.ParseStatus = imported > 0 ? StatusParseDocumentoFinanceiro.ParcialmenteProcessado : StatusParseDocumentoFinanceiro.SemDadosEstruturados;
        if (documento.DocumentKind is TipoDocumentoFinanceiro.ExtratoContaNubank or TipoDocumentoFinanceiro.ExtratoInvestimentosNubank && imported == 0)
        {
            _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
            {
                CargaFinanceira = carga,
                EntityType = nameof(DocumentoFinanceiro),
                Severity = SeveridadeAlerta.Informacao,
                Code = "NUBANK_STATEMENT_STORED_RAW",
                Message = $"O extrato {documento.FileName} foi armazenado para auditoria e processamento financeiro futuro.",
                UsuarioInclusao = "financas-importador"
            });
        }

        return imported;
    }

    private async Task<PdfDocument> AbrirPdfAsync(string file, CancellationToken cancellationToken)
    {
        var senhas = new List<string>();
        var senhaConfigurada = await _config.ObterTextoAsync("Financas", "InformeRendimentosSenha", null, cancellationToken)
            ?? await _config.ObterTextoAsync("Financas", "CpfPrimeiros4", null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(senhaConfigurada))
            senhas.Add(senhaConfigurada.Trim());

        if (senhas.Count == 0)
            return PdfDocument.Open(file);

        return PdfDocument.Open(file, new ParsingOptions { Passwords = senhas });
    }

    private int ImportarOperacoesB3Pdf(string text, DocumentoFinanceiro documento, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, int pageNumber)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var tradeDate = ExtrairDataPregao(lines) ?? ExtrairDataBr(text);
        var noteNumber = ExtrairNumeroNota(lines);
        var imported = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.Equals(lines[i], "B3 RV LISTADO", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var sideCode = lines.ElementAtOrDefault(i + 1) ?? string.Empty;
                var market = lines.ElementAtOrDefault(i + 2) ?? string.Empty;
                var j = i + 3;
                var titleParts = new List<string>();
                while (j + 3 < lines.Count)
                {
                    if (QuantityRegex().IsMatch(lines[j])
                        && MoneyRegex().IsMatch(lines[j + 1])
                        && MoneyRegex().IsMatch(lines[j + 2])
                        && (lines[j + 3] == "D" || lines[j + 3] == "C"))
                    {
                        var assetTitle = string.Join(' ', titleParts).Trim();
                        // Chave canônica (sem marcadores ex-dividendo) + ticker, pra não fragmentar o papel.
                        var canonica = NormalizadorAtivoB3.ChaveCanonica(assetTitle);
                        var tickerB3 = NormalizadorAtivoB3.Ticker(assetTitle);
                        var assetKey = tickerB3 ?? canonica;
                        var ativo = ObterOuCriarAtivo(ativos, assetKey, canonica, MapClasseAtivo(assetTitle), false, tickerB3);
                        var rawBlock = string.Join('\n', lines.Skip(i).Take(j + 4 - i));
                        var duplicateGroupKey = $"{tradeDate:yyyyMMdd}|{sideCode}|{assetKey}|{lines[j]}|{lines[j + 1]}|{lines[j + 2]}";
                        var isDuplicate = _context.OperacoesB3.Local.Any(x => x.DuplicateGroupKey == duplicateGroupKey && x.IsCanonical)
                            || _context.OperacoesB3.Any(x => x.DuplicateGroupKey == duplicateGroupKey && x.IsCanonical);

                        _context.OperacoesB3.Add(new OperacaoB3
                        {
                            CargaFinanceira = carga,
                            SourceDocument = documento,
                            TradeDate = tradeDate,
                            NoteNumber = noteNumber,
                            PageNumber = pageNumber,
                            Broker = "NU INVESTIMENTOS S.A - CTVM",
                            Market = market,
                            OperationType = MapOperacaoB3(sideCode, null),
                            Asset = ativo,
                            OriginalAssetName = assetTitle,
                            Quantity = ParseDecimal(lines[j]) ?? 0m,
                            UnitPrice = ParseDecimal(lines[j + 1]) ?? 0m,
                            GrossAmount = ParseDecimal(lines[j + 2]) ?? 0m,
                            NetAmount = ParseDecimal(lines[j + 2]) ?? 0m,
                            DebitCredit = lines[j + 3],
                            IsCanonical = !isDuplicate,
                            DuplicateGroupKey = duplicateGroupKey,
                            ConfidenceLevel = NivelConfianca.Media,
                            SourceFile = documento.FileName,
                            RawJson = JsonSerializer.Serialize(new { rawBlock, parser = ParserVersion }),
                            UsuarioInclusao = "financas-importador"
                        });
                        imported++;
                        break;
                    }

                    titleParts.Add(lines[j]);
                    j++;
                }
            }
            catch
            {
                // Keep the PDF raw text; uncertain rows will be reviewed later rather than guessed.
            }
        }

        return imported;
    }

    private int ImportarInformeRendimentosPdf(string text, DocumentoFinanceiro documento, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, int pageNumber)
    {
        var linhas = InformeRendimentosParser.Extrair(text);
        var imported = 0;
        foreach (var linha in linhas)
        {
            var ativo = ObterOuCriarAtivo(
                ativos,
                linha.Ticker,
                linha.Ticker,
                linha.Ticker.EndsWith("11", StringComparison.OrdinalIgnoreCase) ? ClasseAtivo.FII : ClasseAtivo.Acao,
                false,
                linha.Ticker);

            if (UpsertRendimento(
                    carga,
                    documento,
                    ativo.Id,
                    linha.DataPagamento,
                    linha.DataReferencia,
                    linha.Tipo,
                    "Informe de Rendimentos",
                    $"InformeIR{documento.ReferenceYear ?? linha.DataPagamento.Year}",
                    null,
                    null,
                    linha.Valor,
                    0m,
                    "BRL",
                    linha.Tributacao,
                    JsonSerializer.Serialize(new { page = pageNumber, raw = linha.RawText })))
            {
                imported++;
            }
        }

        if (imported == 0 && text.Contains("rendimentos", StringComparison.OrdinalIgnoreCase))
        {
            _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
            {
                CargaFinanceira = carga,
                EntityType = nameof(DocumentoFinanceiro),
                Severity = SeveridadeAlerta.Atencao,
                Code = "INFORME_RENDIMENTOS_SEM_LINHAS",
                Message = $"O informe {documento.FileName} foi lido, mas nenhum provento estruturado foi reconhecido na pagina {pageNumber}.",
                UsuarioInclusao = "financas-importador"
            });
        }

        return imported;
    }

    private bool UpsertRendimento(
        CargaFinanceira carga,
        DocumentoFinanceiro? documento,
        int assetId,
        DateTime? pagamento,
        DateTime? referencia,
        string tipo,
        string source,
        string fonte,
        decimal? quantidade,
        decimal? valorPorAcao,
        decimal valor,
        decimal irrf,
        string currency,
        string tributacao,
        string rawJson)
    {
        var chave = ProventoDedup.ChaveEconomica(assetId, referencia, pagamento, tipo);
        var existente = _context.RendimentosInvestimento.Local.FirstOrDefault(x => x.ChaveNatural == chave)
            ?? _context.RendimentosInvestimento.FirstOrDefault(x => x.ChaveNatural == chave);
        if (existente is not null)
        {
            if (!ProventoDedup.MesmoValor(existente.Amount, valor))
            {
                _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
                {
                    CargaFinanceira = carga,
                    EntityType = nameof(RendimentoInvestimento),
                    EntityId = existente.Id == 0 ? null : existente.Id,
                    Severity = SeveridadeAlerta.Atencao,
                    Code = "PROVENTO_DUPLICADO_VALOR_DIVERGENTE",
                    Message = $"Provento duplicado com valor divergente na fonte {fonte}. Evento preservado sem sobrescrever.",
                    Details = JsonSerializer.Serialize(new { chave, valorExistente = existente.Amount, novoValor = valor, fonte }),
                    UsuarioInclusao = "financas-importador"
                });
                return false;
            }

            existente.Source = string.IsNullOrWhiteSpace(existente.Source) ? source : existente.Source;
            existente.Fonte = existente.Fonte.Contains(fonte, StringComparison.OrdinalIgnoreCase)
                ? existente.Fonte
                : string.IsNullOrWhiteSpace(existente.Fonte) ? fonte : $"{existente.Fonte}+{fonte}";
            return false;
        }

        _context.RendimentosInvestimento.Add(new RendimentoInvestimento
        {
            CargaFinanceira = carga,
            SourceDocumentId = documento?.Id > 0 ? documento.Id : null,
            AssetId = assetId,
            PaymentDate = pagamento?.Date,
            ReferenceDate = referencia?.Date,
            IncomeType = tipo,
            Source = source,
            Fonte = fonte,
            Quantity = quantidade,
            RatePerShare = valorPorAcao,
            Amount = valor,
            TaxWithheld = irrf,
            Currency = currency,
            Taxation = tributacao,
            ChaveNatural = chave,
            RawJson = rawJson,
            UsuarioInclusao = "financas-importador"
        });
        return true;
    }

    private int ImportarTransacaoBinanceRow(Dictionary<string, string> row, DocumentoFinanceiro documento, CargaFinanceira carga, Dictionary<string, AtivoFinanceiro> ativos, string tipo)
    {
        var symbol = tipo switch
        {
            "spot" => ExtrairMoedaBase(GetValue(row, "Par")) ?? GetValue(row, "Par") ?? string.Empty,
            "convert" => GetValue(row, "Compra")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? GetValue(row, "Par") ?? string.Empty,
            _ => GetValue(row, "Moeda") ?? GetValue(row, "Coin") ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(symbol))
            return 0;

        var ativo = ObterOuCriarAtivo(ativos, symbol, symbol, ClasseAtivo.Cripto, true);
        var amount = tipo switch
        {
            "spot" => ExtrairDecimalInicial(GetValue(row, "Executado")) ?? 0m,
            "convert" => ExtrairDecimalInicial(GetValue(row, "Compra")) ?? 0m,
            _ => ParseDecimal(GetValue(row, "Alterar") ?? GetValue(row, "Change") ?? GetValue(row, "Quantidade")) ?? 0m
        };

        _context.TransacoesCripto.Add(new TransacaoCripto
        {
            CargaFinanceira = carga,
            SourceDocument = documento,
            TransactionDate = TryParseDateTime(GetValue(row, "Tempo") ?? GetValue(row, "UTC_Time")),
            Exchange = "Binance",
            OperationType = MapOperacaoCripto(tipo, GetValue(row, "Operação") ?? GetValue(row, "Operation") ?? GetValue(row, "Lado")),
            AssetSymbol = ativo.AssetKey,
            Pair = GetValue(row, "Par"),
            Amount = amount,
            Price = ParseDecimal(GetValue(row, "Preço")),
            Total = ExtrairDecimalInicial(GetValue(row, "Quantidade")),
            FeeAsset = ExtrairMoedaFinal(GetValue(row, "Taxa")),
            FeeAmount = ExtrairDecimalInicial(GetValue(row, "Taxa")),
            RawType = GetValue(row, "Operação") ?? GetValue(row, "Operation") ?? tipo,
            SourceFile = documento.FileName,
            RawJson = JsonSerializer.Serialize(row),
            UsuarioInclusao = "financas-importador"
        });

        return 1;
    }

    private AtivoFinanceiro ObterOuCriarAtivo(Dictionary<string, AtivoFinanceiro> ativos, string assetKey, string name, ClasseAtivo classe, bool isCrypto, string? tickerHint = null)
    {
        assetKey = string.IsNullOrWhiteSpace(assetKey) ? "SEM_ATIVO" : assetKey.Trim();
        if (ativos.TryGetValue(assetKey, out var ativo))
        {
            if (string.IsNullOrWhiteSpace(ativo.Ticker))
                ativo.Ticker = tickerHint ?? ExtrairTicker(assetKey) ?? ExtrairTicker(name);
            if (ativo.AssetClass == ClasseAtivo.Outro && classe != ClasseAtivo.Outro)
                ativo.AssetClass = classe;
            if (string.IsNullOrWhiteSpace(ativo.Market))
                ativo.Market = isCrypto ? "Binance" : "B3";
            return ativo;
        }

        ativo = new AtivoFinanceiro
        {
            AssetKey = assetKey,
            Ticker = isCrypto ? assetKey.ToUpperInvariant() : tickerHint ?? ExtrairTicker(assetKey) ?? ExtrairTicker(name),
            Name = string.IsNullOrWhiteSpace(name) ? assetKey : name.Trim(),
            AssetClass = classe,
            Market = isCrypto ? "Binance" : "B3",
            Currency = isCrypto ? "USD/BRL" : "BRL",
            IsCrypto = isCrypto,
            ConceptRole = isCrypto ? MapPapelCripto(assetKey) : null,
            UsuarioInclusao = "financas-importador"
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

    private async Task GarantirCarteirasPadraoAsync(CancellationToken cancellationToken)
    {
        var specs = new (string Nome, string Slug, string Tipo, int Ordem)[]
        {
            ("Bancário", "bancario", "Setor", 10),
            ("Petróleo", "petroleo", "Setor", 20),
            ("Elétrico", "eletrico", "Setor", 30),
            ("Commodities", "commodities", "Setor", 40),
            ("FIIs de papel", "fiis-papel", "Classe", 50),
            ("FIIs de tijolo", "fiis-tijolo", "Classe", 60),
            ("Cripto principais", "cripto-principais", "Tese", 70),
            ("Altcoins", "altcoins", "Tese", 80),
            ("Memecoins", "memecoins", "Tese", 90),
            ("Taxas Binance", "taxas-binance", "Uso", 100)
        };

        var carteiras = await _context.CarteirasFinanceiras.ToListAsync(cancellationToken);
        foreach (var spec in specs)
        {
            if (carteiras.Any(x => x.Slug == spec.Slug))
                continue;

            var carteira = new CarteiraFinanceira
            {
                Nome = spec.Nome,
                Slug = spec.Slug,
                Tipo = spec.Tipo,
                Ordem = spec.Ordem,
                IsSistema = true,
                UsuarioInclusao = "financas-importador"
            };
            _context.CarteirasFinanceiras.Add(carteira);
            carteiras.Add(carteira);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var ativos = await _context.AtivosFinanceiros.ToListAsync(cancellationToken);
        var links = await _context.CarteirasAtivosFinanceiros.ToListAsync(cancellationToken);
        foreach (var ativo in ativos)
        {
            if (links.Any(x => x.AtivoFinanceiroId == ativo.Id))
                continue;

            foreach (var slug in ResolverCarteirasAtivo(ativo))
            {
                var carteira = carteiras.FirstOrDefault(x => x.Slug == slug);
                if (carteira is null || links.Any(x => x.CarteiraFinanceiraId == carteira.Id && x.AtivoFinanceiroId == ativo.Id))
                    continue;

                var link = new CarteiraAtivoFinanceiro
                {
                    CarteiraFinanceiraId = carteira.Id,
                    AtivoFinanceiroId = ativo.Id,
                    UsuarioInclusao = "financas-importador"
                };
                _context.CarteirasAtivosFinanceiros.Add(link);
                links.Add(link);
            }
        }
    }

    private static IReadOnlyList<string> ResolverCarteirasAtivo(AtivoFinanceiro ativo)
    {
        var key = $"{ativo.AssetKey} {ativo.Ticker} {ativo.Name}".ToUpperInvariant();
        if (ativo.IsCrypto || ativo.AssetClass == ClasseAtivo.Cripto)
        {
            if (key.Contains("DOGE")) return ["memecoins"];
            if (key.Contains("BNB")) return ["taxas-binance", "altcoins"];
            if (key.Contains("BTC") || key.Contains("ETH") || key.Contains("SOL")) return ["cripto-principais"];
            return ["altcoins"];
        }

        if (key.Contains("ITUB") || key.Contains("ITSA") || key.Contains("BBAS") || key.Contains("BBDC") || key.Contains("SANB"))
            return ["bancario"];
        if (key.Contains("PETR") || key.Contains("PRIO"))
            return ["petroleo"];
        if (key.Contains("TAEE") || key.Contains("EGIE") || key.Contains("ELET") || key.Contains("CMIG") || key.Contains("CPLE") || key.Contains("CPFE"))
            return ["eletrico"];
        if (key.Contains("VALE") || key.Contains("GGBR") || key.Contains("GOAU") || key.Contains("CSNA") || key.Contains("USIM"))
            return ["commodities"];
        if (ativo.AssetClass == ClasseAtivo.FII)
        {
            if (key.Contains("DEVA") || key.Contains("FYTO") || key.Contains("MXRF") || key.Contains("KNCR") || key.Contains("KNIP") || key.Contains("VGIR") || key.Contains("CPTS"))
                return ["fiis-papel"];
            return ["fiis-tijolo"];
        }

        return [];
    }

    private static bool IsSupportedFile(string file)
        => Path.GetExtension(file).ToLowerInvariant() is ".pdf" or ".xlsx" or ".csv" or ".zip";

    private static void TryDeleteTempFile(string file)
    {
        try
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static TipoDocumentoFinanceiro ClassificarDocumento(string file)
    {
        var name = Path.GetFileName(file).ToLowerInvariant();
        if (name.Contains("transa")) return TipoDocumentoFinanceiro.BinanceTransactions;
        if (name.Contains("trades-spot")) return TipoDocumentoFinanceiro.BinanceSpotTrades;
        if (name.Contains("ordens-spot") || name.Contains("ordens spot")) return TipoDocumentoFinanceiro.BinanceSpotOrders;
        if (name.Contains("convert")) return TipoDocumentoFinanceiro.BinanceConvertOrders;
        if (name.Contains("dep")) return TipoDocumentoFinanceiro.BinanceDeposits;
        if (name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return TipoDocumentoFinanceiro.CsvBinance;
        if (name.Contains("informe") && name.Contains("rend"))
            return TipoDocumentoFinanceiro.InformeRendimentos;
        if (name.Contains("imposto") && name.Contains("renda"))
            return TipoDocumentoFinanceiro.InformeRendimentos;
        if (name.Contains("notas_de_negociacao") || name.Contains("nota"))
            return TipoDocumentoFinanceiro.NotaNegociacaoB3;
        if (name.StartsWith("nu_40648231", StringComparison.OrdinalIgnoreCase))
            return TipoDocumentoFinanceiro.ExtratoContaNubank;
        if (name.Contains("nubank"))
            return TipoDocumentoFinanceiro.ExtratoInvestimentosNubank;
        return TipoDocumentoFinanceiro.Desconhecido;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private static Dictionary<string, string> ToRowDictionary(IReadOnlyList<string> headers, IReadOnlyList<string> values)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Math.Min(headers.Count, values.Count); i++)
        {
            var header = headers[i].Trim();
            if (!string.IsNullOrWhiteSpace(header))
            {
                row[header] = values[i].Trim();
                var normalized = NormalizeHeader(header);
                if (!string.Equals(normalized, header, StringComparison.OrdinalIgnoreCase) && !row.ContainsKey(normalized))
                    row[normalized] = values[i].Trim();
            }
        }

        return row;
    }

    private static string NormalizeHeader(string header)
        => Regex.Replace(header, "[¹²³⁴⁵⁶⁷⁸⁹⁰]", string.Empty).Trim();

    private static List<List<string>> ExtrairLinhasXlsx(string xml)
    {
        var document = System.Xml.Linq.XDocument.Parse(xml);
        var ns = document.Root?.Name.Namespace ?? System.Xml.Linq.XNamespace.None;
        return document.Descendants(ns + "row")
            .Select(row => row.Elements(ns + "c")
                .Select(cell =>
                {
                    var inline = cell.Element(ns + "is");
                    if (inline is not null)
                        return string.Concat(inline.Descendants(ns + "t").Select(x => x.Value));

                    return cell.Element(ns + "v")?.Value ?? string.Empty;
                })
                .ToList())
            .ToList();
    }

    private static bool IsHeaderRow(IReadOnlyList<string> values)
    {
        var nonEmpty = values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        return nonEmpty.Count >= 3
            && (nonEmpty.Contains("Tempo")
                || nonEmpty.Contains("UTC_Time")
                || nonEmpty.Contains("ID do Usuário")
                || nonEmpty.Contains("Par")
                || nonEmpty.Contains("Moeda"));
    }

    private static string TipoBinance(TipoDocumentoFinanceiro kind)
        => kind switch
        {
            TipoDocumentoFinanceiro.BinanceSpotTrades or TipoDocumentoFinanceiro.BinanceSpotOrders => "spot",
            TipoDocumentoFinanceiro.BinanceConvertOrders => "convert",
            TipoDocumentoFinanceiro.BinanceDeposits => "deposit",
            _ => "transaction"
        };

    private static DateTime? ExtrairDataPregao(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.Equals(lines[i], "Data pregão", StringComparison.OrdinalIgnoreCase))
                continue;

            for (var j = i + 1; j < Math.Min(i + 8, lines.Count); j++)
            {
                var date = ParseDateBr(lines[j]);
                if (date.HasValue)
                    return date;
            }
        }

        return null;
    }

    private static DateTime? ExtrairDataBr(string text)
    {
        var match = Regex.Match(text, @"\d{2}/\d{2}/\d{4}");
        return match.Success ? ParseDateBr(match.Value) : null;
    }

    private static DateTime? ParseDateBr(string? value)
        => DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var parsed) ? parsed : null;

    private static string? ExtrairNumeroNota(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (Regex.IsMatch(lines[i], @"^\d+\s+\d+\s+\d{2}/\d{2}/\d{4}$"))
                return lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        return null;
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> row, string key)
        => row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string? ExtrairTicker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = Regex.Match(value.ToUpperInvariant(), @"\b[A-Z]{4}\d{1,2}\b");
        return match.Success ? match.Value : null;
    }

    private (byte[] JsonBytes, string SourcePath)? ResolverFonteJson()
    {
        var zipPath = _configuration["Financas:SeedZipPath"];
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

        var jsonPath = _configuration["Financas:SeedJsonPath"];
        return !string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath)
            ? (File.ReadAllBytes(jsonPath), jsonPath)
            : null;
    }

    private string? CarregarDashboardJson()
    {
        var htmlPath = _configuration["Financas:DashboardHtmlPath"];
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

        // Notação científica (ex.: earn da Binance vem como "6E-8" = 0,00000006). O regex de prefixo
        // abaixo cortaria no "E" e devolveria "6" (erro de 10^8); trata antes.
        if (Regex.IsMatch(text, @"^[+-]?\d+(\.\d+)?[eE][+-]?\d+$"))
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var sci) ? sci : null;

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

    [GeneratedRegex(@"^\d+(?:,\d+)?$")]
    private static partial Regex QuantityRegex();

    [GeneratedRegex(@"^\d{1,3}(?:\.\d{3})*,\d{2}$|^\d+,\d{2}$")]
    private static partial Regex MoneyRegex();
}

internal static partial class FinancasImportadorLogMessages
{
    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Carga financeira importada. Fonte={Fonte}, Sha={Sha}")]
    public static partial void CargaFinanceiraImportada(ILogger logger, string fonte, string sha);
}
