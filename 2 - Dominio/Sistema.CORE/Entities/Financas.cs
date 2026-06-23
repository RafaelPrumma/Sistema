namespace Sistema.CORE.Entities;

public enum ClasseAtivo
{
    Acao = 1,
    FII = 2,
    ETF = 3,
    BDR = 4,
    RendaFixa = 5,
    Cripto = 6,
    Caixa = 7,
    Outro = 99
}

public enum TipoOperacaoFinanceira
{
    Compra = 1,
    Venda = 2,
    Deposito = 3,
    Saque = 4,
    Conversao = 5,
    Trade = 6,
    Taxa = 7,
    Rendimento = 8,
    Outro = 99
}

public enum OrigemTransacao
{
    Importacao = 1,
    Manual = 2
}

public enum NivelConfianca
{
    Alta = 1,
    Media = 2,
    Baixa = 3,
    PendenteValidacao = 4
}

public enum StatusDocumentoFinanceiro
{
    Importado = 1,
    Processado = 2,
    ParcialmenteProcessado = 3,
    Falhou = 4,
    PendenteValidacao = 5
}

public enum TipoConteudoBrutoFinanceiro
{
    TextoPagina = 1,
    Planilha = 2,
    LinhaPlanilha = 3,
    Json = 4,
    Outro = 99
}

public enum StatusEstimativaPosicao
{
    AbertaOuResidual = 1,
    EncerradaPorOperacoes = 2,
    Inconsistente = 3,
    PendenteValidacao = 4
}

public enum SeveridadeAlerta
{
    Informacao = 1,
    Atencao = 2,
    Critico = 3
}

public enum TipoDocumentoFinanceiro
{
    Desconhecido = 0,
    JsonConsolidado = 1,
    NotaNegociacaoB3 = 2,
    ExtratoInvestimentosNubank = 3,
    ExtratoContaNubank = 4,
    BinanceTransactions = 5,
    BinanceSpotTrades = 6,
    BinanceSpotOrders = 7,
    BinanceConvertOrders = 8,
    BinanceDeposits = 9,
    CsvBinance = 10,
    InformeRendimentos = 11,
    ExtratoConsolidadoB3 = 12
}

public enum StatusParseDocumentoFinanceiro
{
    Pendente = 0,
    Processado = 1,
    ParcialmenteProcessado = 2,
    SemDadosEstruturados = 3,
    Falhou = 4
}

public enum StatusImportacaoFinanceira
{
    Iniciada = 1,
    Concluida = 2,
    ConcluidaComAlertas = 3,
    Falhou = 4
}

public enum ProvedorCotacao
{
    Manual = 0,
    Brapi = 1,
    Binance = 2
}

public enum StatusCotacao
{
    Atual = 1,
    Desatualizada = 2,
    Falhou = 3,
    SemToken = 4,
    NaoSuportada = 5
}

public class CargaFinanceira : AuditableEntity
{
    public int Id { get; set; }
    public string SchemaVersion { get; set; } = string.Empty;
    public string JsonSha256 { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public DateTime? GeneratedAt { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public StatusDocumentoFinanceiro Status { get; set; } = StatusDocumentoFinanceiro.Importado;
    public string SummaryJson { get; set; } = "{}";
    public string? DashboardJson { get; set; }
}

public class ImportacaoFinanceiraArquivo : AuditableEntity
{
    public int Id { get; set; }
    public string SourceFolder { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public StatusImportacaoFinanceira Status { get; set; } = StatusImportacaoFinanceira.Iniciada;
    public int FilesDiscovered { get; set; }
    public int FilesImported { get; set; }
    public int FilesSkipped { get; set; }
    public int StructuredRowsImported { get; set; }
    public string? Message { get; set; }
}

public class DocumentoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int? ImportacaoFinanceiraArquivoId { get; set; }
    public ImportacaoFinanceiraArquivo? ImportacaoFinanceiraArquivo { get; set; }
    public string Colecao { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? StoredPath { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? ReferenceYear { get; set; }
    public int? PageCount { get; set; }
    public TipoDocumentoFinanceiro DocumentKind { get; set; } = TipoDocumentoFinanceiro.Desconhecido;
    public StatusParseDocumentoFinanceiro ParseStatus { get; set; } = StatusParseDocumentoFinanceiro.Pendente;
    public string ParserVersion { get; set; } = string.Empty;
    public StatusDocumentoFinanceiro Status { get; set; } = StatusDocumentoFinanceiro.Importado;
    public string RawMetadataJson { get; set; } = "{}";
    public ICollection<ConteudoBrutoFinanceiro> ConteudosBrutos { get; set; } = new List<ConteudoBrutoFinanceiro>();
}

public class ConteudoBrutoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int DocumentoFinanceiroId { get; set; }
    public DocumentoFinanceiro? DocumentoFinanceiro { get; set; }
    public TipoConteudoBrutoFinanceiro ContentType { get; set; }
    public int? PageNumber { get; set; }
    public string? SheetName { get; set; }
    public int? RowNumber { get; set; }
    public string? RawText { get; set; }
    public string? RawJson { get; set; }
}

public class AtivoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public string AssetKey { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClasseAtivo AssetClass { get; set; } = ClasseAtivo.Outro;
    public string Market { get; set; } = string.Empty;
    public string Currency { get; set; } = "BRL";
    public bool IsCrypto { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConceptRole { get; set; }
}

public class CarteiraFinanceira : AuditableEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Tipo { get; set; } = "Carteira";
    public bool IsSistema { get; set; }
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; }

    // Hierarquia (F-I): carteira-topo tem ParentId nulo; subcarteira aponta para a topo.
    // Self-FK nullable, OnDelete restrict (ver CarteiraFinanceiraMap).
    public int? ParentId { get; set; }
    public CarteiraFinanceira? Parent { get; set; }
    public ICollection<CarteiraFinanceira> Filhas { get; set; } = new List<CarteiraFinanceira>();

    public ICollection<CarteiraAtivoFinanceiro> Ativos { get; set; } = new List<CarteiraAtivoFinanceiro>();
}

public class CarteiraAtivoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int CarteiraFinanceiraId { get; set; }
    public CarteiraFinanceira? CarteiraFinanceira { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public decimal? PesoAlvo { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; } = true;
}

public class CotacaoAtivoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public ProvedorCotacao Provedor { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Currency { get; set; } = "BRL";
    public decimal Price { get; set; }
    public decimal PriceBRL { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
    public DateTime? MarketTime { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public StatusCotacao Status { get; set; } = StatusCotacao.Atual;
    public string? ErrorMessage { get; set; }
    public string RawJson { get; set; } = "{}";
}

public class PrecoHistoricoAtivoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public ProvedorCotacao Provedor { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Interval { get; set; } = "1d";
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal CloseBRL { get; set; }
    public decimal? Volume { get; set; }
    public string RawJson { get; set; } = "{}";
}

public class OperacaoB3 : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int? SourceDocumentId { get; set; }
    public DocumentoFinanceiro? SourceDocument { get; set; }
    public DateTime? TradeDate { get; set; }
    public string? NoteNumber { get; set; }
    public int? PageNumber { get; set; }
    public string Broker { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public TipoOperacaoFinanceira OperationType { get; set; }
    public int? AssetId { get; set; }
    public AtivoFinanceiro? Asset { get; set; }
    public string OriginalAssetName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public string DebitCredit { get; set; } = string.Empty;
    public bool IsCanonical { get; set; } = true;
    public string? DuplicateGroupKey { get; set; }
    public NivelConfianca ConfidenceLevel { get; set; } = NivelConfianca.PendenteValidacao;
    public string SourceFile { get; set; } = string.Empty;
    public string RawJson { get; set; } = "{}";
}

public class TransacaoCripto : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int? SourceDocumentId { get; set; }
    public DocumentoFinanceiro? SourceDocument { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string Exchange { get; set; } = "Binance";
    public TipoOperacaoFinanceira OperationType { get; set; } = TipoOperacaoFinanceira.Outro;
    public string AssetSymbol { get; set; } = string.Empty;
    public string? Pair { get; set; }
    public decimal Amount { get; set; }
    public decimal? Price { get; set; }
    public decimal? Total { get; set; }
    public string? FeeAsset { get; set; }
    public decimal? FeeAmount { get; set; }
    public string RawType { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public string RawJson { get; set; } = "{}";
}

// Tabela única canônica de transações: recebe tanto a materialização da importação
// (OperacaoB3/TransacaoCripto continuam como staging bruto) quanto lançamentos manuais.
// É a fonte de verdade para posições, gráfico de evolução e resumo analítico.
public class TransacaoFinanceira : AuditableEntity
{
    public int Id { get; set; }
    public OrigemTransacao Origem { get; set; } = OrigemTransacao.Manual;
    public int AssetId { get; set; }
    public AtivoFinanceiro? Asset { get; set; }
    public DateTime Date { get; set; }
    public TipoOperacaoFinanceira OperationType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Broker { get; set; } = string.Empty;
    public string Fonte { get; set; } = string.Empty;
    public string? Observacao { get; set; }

    // Rastreio de origem (preenchido quando a transação veio de importação).
    public int? SourceDocumentId { get; set; }
    public DocumentoFinanceiro? SourceDocument { get; set; }
    public int? CargaFinanceiraId { get; set; }
    public string? StagingTipo { get; set; }
    public int? StagingId { get; set; }
    public string? DuplicateGroupKey { get; set; }
    public bool IsCanonical { get; set; } = true;
    public NivelConfianca ConfidenceLevel { get; set; } = NivelConfianca.Media;
    public string RawJson { get; set; } = "{}";

    // Chave natural da transação (fonte + ativo + data/hora + tipo + quantidade + preço).
    // Só é preenchida em importações; um índice único garante que o mesmo lançamento não
    // entre duas vezes mesmo vindo de arquivos diferentes. Lançamentos manuais ficam nulos.
    public string? ChaveNatural { get; set; }
}

// Staging do extrato consolidado da B3 (aba Negociações). É um AGREGADO mensal por ticker:
// até 1 compra + 1 venda por ticker/mês (preço = preço médio do mês). Materializa em
// TransacaoFinanceira só onde as notas (granular) NÃO cobrem o ticker×mês (precedência §3.1).
public class NegociacaoMensalB3 : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int? SourceDocumentId { get; set; }
    public DocumentoFinanceiro? SourceDocument { get; set; }
    public int AssetId { get; set; }
    public AtivoFinanceiro? Asset { get; set; }
    // Ano-mês do extrato no formato yyyyMM (ex.: 202209). Vem do nome do arquivo.
    public int AnoMes { get; set; }
    public TipoOperacaoFinanceira OperationType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public DateTime? PeriodoInicial { get; set; }
    public DateTime? PeriodoFinal { get; set; }
    public string Broker { get; set; } = string.Empty;
    // Chave natural do agregado (fonte + ticker + ano-mês + sentido + corretora) — índice único filtrado.
    public string? ChaveNatural { get; set; }
    public string RawJson { get; set; } = "{}";
}

public class EstimativaPosicaoCarteira : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int AssetId { get; set; }
    public AtivoFinanceiro? Asset { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalSold { get; set; }
    public decimal RealizedResult { get; set; }
    public decimal EstimatedCurrentPosition { get; set; }
    public StatusEstimativaPosicao Status { get; set; } = StatusEstimativaPosicao.PendenteValidacao;
    public NivelConfianca ConfidenceLevel { get; set; } = NivelConfianca.PendenteValidacao;
    public DateTime? LastOperationDate { get; set; }
    public string RawJson { get; set; } = "{}";
}

// Provento recebido (dividendo, JCP, rendimento de FII). Pode vir da importação (atrelado a uma
// carga) ou de busca automática na Brapi (CargaFinanceiraId nulo). A ChaveNatural torna o job
// recorrente idempotente: o mesmo provento não entra duas vezes mesmo rodando todo dia.
public class RendimentoInvestimento : AuditableEntity
{
    public int Id { get; set; }
    public int? CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int? SourceDocumentId { get; set; }
    public int? AssetId { get; set; }
    public AtivoFinanceiro? Asset { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? ReferenceDate { get; set; }
    public string IncomeType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxWithheld { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? RatePerShare { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Taxation { get; set; } = string.Empty;
    public string RawJson { get; set; } = "{}";

    // Origem do provento (Brapi|Manual|fonte do arquivo). Distingue o que foi buscado automaticamente.
    public string Fonte { get; set; } = string.Empty;
    // Chave natural (Fonte|Ativo|data-pagamento|tipo|valor-por-ação) — índice único filtrado.
    public string? ChaveNatural { get; set; }
}

public class AgregadoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public string Dimensao { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public int? Ano { get; set; }
    public string? Mes { get; set; }
    public ClasseAtivo? ClasseAtivo { get; set; }
    public decimal? ValorCompra { get; set; }
    public decimal? ValorVenda { get; set; }
    public decimal? Saldo { get; set; }
    public decimal? Quantidade { get; set; }
    public int? Contagem { get; set; }
    public string RawJson { get; set; } = "{}";
}

public class AlertaConfiabilidade : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public SeveridadeAlerta Severity { get; set; } = SeveridadeAlerta.Atencao;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TipoEventoCorporativo
{
    Desdobramento = 1,
    Grupamento = 2,
    Bonificacao = 3
}

// Evento corporativo (split/grupamento/bonificação) de um ativo financeiro.
// Fator > 1 = desdobramento (ex.: 8 = 1:8); Fator < 1 = grupamento (ex.: 0,1 = 1:10).
// A ChaveNatural garante idempotência: o mesmo evento não entra duas vezes.
public class EventoCorporativo : AuditableEntity
{
    public int Id { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public TipoEventoCorporativo Tipo { get; set; } = TipoEventoCorporativo.Desdobramento;
    // Data-ex do evento (data a partir da qual as cotas já são pós-evento).
    public DateTime Data { get; set; }
    // Fator multiplicador: transações pré-Data têm Quantity *= Fator e UnitPrice /= Fator.
    // Ex.: 8 = desdobramento 1:8; 0.1 = grupamento 10:1.
    public decimal Fator { get; set; }
    public string Fonte { get; set; } = string.Empty;
    // Chave natural: idempotência do seed/import (ticker|data|fator).
    public string? ChaveNatural { get; set; }

    // Chave natural canônica do evento. Independe da fonte (seed, manual ou Brapi geram a MESMA
    // chave para o mesmo evento) → o índice único deduplica entre fontes e evita aplicar o fator 2×.
    public static string GerarChaveNatural(string ticker, DateTime data, decimal fator)
        => $"{ticker.Trim().ToUpperInvariant()}|{data:yyyyMMdd}|{fator.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
