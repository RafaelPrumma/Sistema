namespace Sistema.APP.DTOs;

public record FinanceiroKpiDto(string Titulo, decimal Valor, string Subtitulo, string Icone, bool Monetario = true);

public record FinanceiroSerieDto(string Label, decimal ValorCompra, decimal ValorVenda, decimal Saldo, int Operacoes);

public record FinanceiroDistribuicaoDto(string Label, decimal Valor, int Operacoes);

public record DocumentoFinanceiroDto(
    int Id,
    string FileName,
    string FileType,
    string Source,
    string Sha256,
    long SizeBytes,
    int? ReferenceYear,
    int? PageCount,
    string Status);

public record ConteudoBrutoFinanceiroDto(
    int Id,
    string ContentType,
    int? PageNumber,
    string? SheetName,
    int? RowNumber,
    string? RawText,
    string? RawJson);

public record OperacaoB3Dto(
    int Id,
    DateTime? TradeDate,
    string Tipo,
    string Ativo,
    string Classe,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal ValorBruto,
    string Mercado,
    string Arquivo,
    int? Pagina,
    bool Canonica,
    string Confianca);

public record TransacaoCriptoDto(
    int Id,
    DateTime? Data,
    string Tipo,
    string Ativo,
    string? Par,
    decimal Quantidade,
    decimal? Preco,
    decimal? Total,
    string Fonte,
    string RawType);

public record PosicaoFinanceiraDto(
    int Id,
    string Ativo,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal TotalInvestido,
    decimal TotalVendido,
    decimal ResultadoRealizado,
    string Status,
    string Confianca,
    DateTime? UltimaOperacao);

public record RendimentoInvestimentoDto(string Tipo, string Fonte, decimal Valor, string Tributacao);

public record AlertaConfiabilidadeDto(
    int Id,
    string Entidade,
    string Severidade,
    string Codigo,
    string Mensagem,
    string? Detalhes,
    DateTime CriadoEm);

public class MinhasFinancasDashboardDto
{
    public string GeradoEm { get; set; } = string.Empty;
    public string Fonte { get; set; } = string.Empty;
    public IReadOnlyList<FinanceiroKpiDto> Kpis { get; set; } = [];
    public IReadOnlyList<FinanceiroSerieDto> B3PorAno { get; set; } = [];
    public IReadOnlyList<FinanceiroSerieDto> B3PorMes { get; set; } = [];
    public IReadOnlyList<FinanceiroDistribuicaoDto> B3PorClasse { get; set; } = [];
    public IReadOnlyList<FinanceiroDistribuicaoDto> BinanceMoedas { get; set; } = [];
    public IReadOnlyList<OperacaoB3Dto> UltimasOperacoesB3 { get; set; } = [];
    public IReadOnlyList<TransacaoCriptoDto> UltimasTransacoesCripto { get; set; } = [];
    public IReadOnlyList<PosicaoFinanceiraDto> PosicoesAbertas { get; set; } = [];
    public IReadOnlyList<PosicaoFinanceiraDto> PosicoesEncerradas { get; set; } = [];
    public IReadOnlyList<AlertaConfiabilidadeDto> Alertas { get; set; } = [];
    public string? DashboardJson { get; set; }
}
