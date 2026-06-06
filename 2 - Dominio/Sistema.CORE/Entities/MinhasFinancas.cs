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

public class DocumentoFinanceiro : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public string Colecao { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? ReferenceYear { get; set; }
    public int? PageCount { get; set; }
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

public class RendimentoInvestimento : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
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
    public string Currency { get; set; } = "BRL";
    public string Taxation { get; set; } = string.Empty;
    public string RawJson { get; set; } = "{}";
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
