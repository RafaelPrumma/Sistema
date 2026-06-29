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

public enum StatusPosicaoAtivo
{
    Aberta = 1,
    Encerrada = 2,
    Inconsistente = 3
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
    Binance = 2,
    // Preço de Fechamento da aba Posição do extrato consolidado B3 (custódia oficial). Usado como
    // cotação de mercado dos ativos B3 quando não há token Brapi (ação/FII não cota de graça).
    B3Custodia = 3
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
    public string Chave { get; set; } = string.Empty;
    public string? Sigla { get; set; }
    public string Nome { get; set; } = string.Empty;
    public ClasseAtivo Classe { get; set; } = ClasseAtivo.Outro;
    public string Mercado { get; set; } = string.Empty;
    public string Moeda { get; set; } = "BRL";
    public bool EhCripto { get; set; }
    public bool Ativo { get; set; } = true;
    public string? PapelConceitual { get; set; }
}

public class CarteiraFinanceira : AuditableEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Tipo { get; set; } = "Carteira";
    public bool EhSistema { get; set; }
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; }

    // Hierarquia (F-I): carteira-topo tem ParentId nulo; subcarteira aponta para a topo.
    // Self-FK nullable, OnDelete restrict (ver CarteiraFinanceiraMap).
    public int? CarteiraPaiId { get; set; }
    public CarteiraFinanceira? CarteiraPai { get; set; }
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
    public string Simbolo { get; set; } = string.Empty;
    public string Moeda { get; set; } = "BRL";
    public decimal Preco { get; set; }
    public decimal PrecoBRL { get; set; }
    public decimal? Variacao { get; set; }
    public decimal? VariacaoPercentual { get; set; }
    public DateTime? HorarioMercado { get; set; }
    public DateTime ConsultadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiraEm { get; set; }
    public StatusCotacao Status { get; set; } = StatusCotacao.Atual;
    public string? MensagemErro { get; set; }
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

// Agregado OFICIAL ANUAL de proventos do extrato consolidado ANUAL da B3
// (relatorio-consolidado-anual-AAAA.xlsx, aba "Proventos Recebidos"). É um total por
// ticker × tipo de evento, SEM datas → NÃO entra em RendimentoInvestimento (corromperia o
// calendário mensal). Serve de VERDADE OFICIAL do total do ano para reconciliação/validação:
// confronta-se com a soma dos RendimentoInvestimento materializados do mesmo ano.
// A ChaveNatural (ano|assetId|tipo) torna o upsert idempotente: reimportar o anual atualiza o
// valor, não duplica.
public class ProventoAnualB3 : AuditableEntity
{
    public int Id { get; set; }
    public int? CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int? SourceDocumentId { get; set; }
    public DocumentoFinanceiro? SourceDocument { get; set; }
    public int AssetId { get; set; }
    public AtivoFinanceiro? Asset { get; set; }
    // Ano de referência do agregado (ex.: 2024). Vem do nome do arquivo anual.
    public int Year { get; set; }
    // Tipo do evento normalizado (Dividendo/JCP/Rendimento/Amortização) — mesma normalização do mensal.
    public string Tipo { get; set; } = string.Empty;
    // Valor LÍQUIDO total do ano (a planilha já traz líquido). Base da reconciliação.
    public decimal ValorLiquido { get; set; }
    // Chave natural (ano|assetId|tipo) — índice único filtrado, idempotência do upsert.
    public string? ChaveNatural { get; set; }
    public string RawJson { get; set; } = "{}";

    /// <summary>Chave natural canônica do agregado anual: ano|assetId|tipo (case/trim-normalizado).</summary>
    public static string GerarChaveNatural(int year, int assetId, string tipo)
        => $"{year:D4}|{assetId}|{(tipo ?? string.Empty).Trim().ToUpperInvariant()}";
}

public class EstimativaPosicaoCarteira : AuditableEntity
{
    public int Id { get; set; }
    public int CargaFinanceiraId { get; set; }
    public CargaFinanceira? CargaFinanceira { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal TotalInvestido { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal ResultadoRealizado { get; set; }
    public decimal PosicaoAtualEstimada { get; set; }
    public StatusEstimativaPosicao Status { get; set; } = StatusEstimativaPosicao.PendenteValidacao;
    public NivelConfianca NivelConfianca { get; set; } = NivelConfianca.PendenteValidacao;
    public DateTime? UltimaOperacaoEm { get; set; }
    public string RawJson { get; set; } = "{}";
}

public class PosicaoAtivo : AuditableEntity
{
    public int Id { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal CustoTotal { get; set; }
    public decimal TotalComprado { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal ResultadoRealizado { get; set; }
    public DateTime? UltimaOperacaoEm { get; set; }
    public StatusPosicaoAtivo Status { get; set; } = StatusPosicaoAtivo.Aberta;
    public DateTime CalculadoEm { get; set; } = DateTime.UtcNow;
    public string VersaoCalculo { get; set; } = string.Empty;
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

// Direção do gatilho de um alerta de preço.
//  Acima  = dispara quando o preço atual fica >= Limiar (rompimento de alta / preço-alvo de venda).
//  Abaixo = dispara quando o preço atual fica <= Limiar (queda / preço-alvo de compra).
public enum DirecaoAlertaPreco
{
    Acima = 1,
    Abaixo = 2
}

// Regra de alerta de preço (F-H): notifica internamente quando a cotação de um ativo cruza um limiar
// na direção configurada. Para não notificar a cada execução do job, guarda o estado de re-disparo:
// quando dispara, marca DispararadoEm + UltimoPreco; só re-arma quando o preço volta para o outro
// lado do limiar (histerese simples). Esse desenho mantém o "cruzou?" como lógica pura e testável
// (ver AvaliadorAlertaPreco).
public class AlertaPreco : AuditableEntity
{
    public int Id { get; set; }
    public int AtivoFinanceiroId { get; set; }
    public AtivoFinanceiro? AtivoFinanceiro { get; set; }
    public decimal Limiar { get; set; }
    public DirecaoAlertaPreco Direcao { get; set; } = DirecaoAlertaPreco.Acima;
    public bool Ativo { get; set; } = true;
    public string? Observacao { get; set; }

    // Controle de re-disparo: quando != null, o alerta já disparou e está "armado" (não notifica de novo
    // até re-armar). UltimoPreco guarda o preço observado no disparo (para diagnóstico/UI).
    public DateTime? DispararadoEm { get; set; }
    public decimal? UltimoPreco { get; set; }
}

// Índice de benchmark cuja série temporal alimenta a comparação de rentabilidade (F-B F2).
//  Cdi  = taxa diária em % a.d. (BCB SGS 12) — acumula por produtório dos dias úteis no período.
//  Ipca = taxa mensal em % a.m. (BCB SGS 433) — acumula por produtório dos meses no período.
//  Ibov = nível de fechamento do índice (BCB SGS 7 ou Brapi ^BVSP) — acumula por fim/início − 1.
public enum IndiceBenchmark
{
    Cdi = 1,
    Ipca = 2,
    Ibov = 3
}

// Ponto da série temporal de um benchmark (CDI/IPCA/Ibovespa), populado pelo job financas-benchmarks
// a partir do BCB SGS (público, sem token) e da Brapi (Ibov, opcional). NÃO reusa
// FinanceiroPrecoHistoricoAtivo de propósito: um índice não é um "ativo" da carteira (não tem posição,
// saúde de cotação, etc.). O significado de Valor depende do índice (ver IndiceBenchmark). A ChaveNatural
// (indice|data) torna o upsert idempotente: rodar o job todo dia não duplica o ponto.
public class SerieBenchmark : AuditableEntity
{
    public int Id { get; set; }
    public IndiceBenchmark Indice { get; set; }
    // Data do ponto (UTC, sem hora). Para o IPCA mensal, o 1º dia do mês de referência.
    public DateTime Date { get; set; }
    // Significado por índice: CDI = % a.d.; IPCA = % a.m.; Ibov = nível do índice (pontos).
    public decimal Valor { get; set; }
    public string Fonte { get; set; } = string.Empty;
    // Chave natural (indice|data) — índice único filtrado, idempotência do upsert.
    public string? ChaveNatural { get; set; }
    public string RawJson { get; set; } = "{}";

    /// <summary>Chave natural canônica do ponto da série: indice|data (yyyyMMdd).</summary>
    public static string GerarChaveNatural(IndiceBenchmark indice, DateTime data)
        => $"{indice}|{data:yyyyMMdd}";
}
