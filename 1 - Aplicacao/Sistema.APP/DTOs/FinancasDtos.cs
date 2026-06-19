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

public record ProventoDto(
    int Id,
    DateTime? DataPagamento,
    DateTime? DataCom,
    string Ticker,
    string Nome,
    string Classe,
    string Tipo,
    decimal? Quantidade,
    decimal? ValorPorAcao,
    decimal Valor,
    decimal ImpostoRetido,
    decimal ValorLiquido,
    string Tributacao,
    string Fonte);

public record ProventosResumoDto(
    decimal RecebidoMes,
    decimal RecebidoAno,
    decimal RecebidoTotal,
    decimal AReceber,
    int Quantidade);

public record ProventoPeriodoDto(
    string Codigo,
    string Rotulo,
    decimal Recebido,
    decimal AReceber,
    int Quantidade,
    decimal Percentual);

public record ProventoBaldeDto(
    string Codigo,
    string Rotulo,
    decimal Valor,
    decimal Percentual,
    int Quantidade,
    string Status);

public record ProventoMensalDto(
    string Rotulo,
    int Ano,
    int Mes,
    decimal Recebido,
    decimal AReceber);

public record ProventosPaginaDto(
    IReadOnlyList<ProventoDto> Itens,
    int Page,
    int PageSize,
    int TotalCount,
    ProventosResumoDto Resumo,
    IReadOnlyList<ProventoPeriodoDto> Periodos,
    IReadOnlyList<ProventoBaldeDto> Baldes,
    IReadOnlyList<ProventoMensalDto> Mensais);

public record AlertaConfiabilidadeDto(
    int Id,
    string Entidade,
    string Severidade,
    string Codigo,
    string Mensagem,
    string? Detalhes,
    DateTime CriadoEm);

public record CotacaoAtivoDto(
    int AtivoId,
    string Ativo,
    string Classe,
    string Symbol,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal? PrecoAtual,
    decimal ValorMercado,
    decimal CustoEstimado,
    decimal ResultadoNaoRealizado,
    decimal ResultadoNaoRealizadoPercentual,
    decimal? VariacaoDiaPercentual,
    DateTime? AtualizadoEm,
    string Status,
    string Confianca);

public record CarteiraAtivoResumoDto(
    int AtivoId,
    string Ativo,
    string Classe,
    string Symbol,
    decimal Quantidade,
    decimal ValorMercado,
    decimal PercentualCarteira,
    decimal? VariacaoDiaPercentual,
    decimal ResultadoNaoRealizado,
    decimal ResultadoNaoRealizadoPercentual,
    string Status);

public record CarteiraFinanceiraResumoDto(
    int Id,
    string Nome,
    string Tipo,
    decimal ValorMercado,
    decimal CustoEstimado,
    decimal ResultadoNaoRealizado,
    decimal ResultadoNaoRealizadoPercentual,
    decimal VariacaoDiaPercentual,
    decimal PercentualPatrimonio,
    int Ativos,
    IReadOnlyList<CarteiraAtivoResumoDto> Itens);

public record PeriodoPerformanceDto(string Codigo, string Label, decimal VariacaoPercentual, decimal VariacaoValor);

public record ValidacaoAtivoResultado(
    bool Valido,
    string Ticker,
    string Nome,
    string Classe,
    string Provedor,
    bool IsCrypto,
    decimal? PrecoAtual,
    string? Mensagem);

public record TransacaoFinanceiraDto(
    int Id,
    string Origem,
    string Fonte,
    string Ativo,
    string Ticker,
    string Classe,
    DateTime Data,
    string Tipo,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal ValorTotal,
    decimal Taxas,
    string Corretora,
    string? Observacao);

public record NovaTransacaoInput(
    string Ticker,
    string Tipo,
    decimal Quantidade,
    decimal PrecoUnitario,
    DateTime Data,
    decimal Taxas = 0m,
    string? Corretora = null,
    string? Observacao = null);

public record ResultadoOperacao(bool Sucesso, string? Mensagem, int? Id = null);

public record EventoCorporativoDto(
    int Id,
    string Ticker,
    string AtivoNome,
    string Tipo,
    DateTime Data,
    decimal Fator,
    string Fonte);

public record NovoEventoCorporativoInput(
    string Ticker,
    string Tipo,
    DateTime Data,
    decimal Fator,
    string? Fonte = null);

// Série de evolução: eixo de datas compartilhado + arrays de valores paralelos (payload enxuto).
// VariacaoDia e ValorAtual vêm das cotações ao vivo (não do histórico diário).
public record SerieEvolucaoDto(
    string Chave,
    string Rotulo,
    IReadOnlyList<decimal> Valores,
    decimal VariacaoDia,
    decimal ValorAtual);

public record EvolucaoPatrimonioDto(
    IReadOnlyList<string> Datas,
    IReadOnlyList<decimal> Total,
    decimal VariacaoDiaTotal,
    decimal ValorAtualTotal,
    IReadOnlyList<SerieEvolucaoDto> Setores,
    IReadOnlyList<PeriodoPerformanceDto> Periodos);

public record ResumoAtivoDto(
    string Ticker,
    string Nome,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal Custo,
    decimal? PrecoAtual,
    decimal ValorMercado,
    decimal PlNaoRealizado,
    decimal PlPercentual,
    decimal ResultadoRealizadoPeriodo,
    decimal ProventosPeriodo,
    decimal RetornoTotalPeriodo);

public record VendaRealizadaDto(
    DateTime Data,
    string Ticker,
    decimal Quantidade,
    decimal PrecoVenda,
    decimal PrecoMedio,
    decimal Resultado,
    bool Boa);

public record ResumoAnaliticoDto(
    string PeriodoLabel,
    DateTime Inicio,
    DateTime Fim,
    decimal TotalComprado,
    decimal TotalVendido,
    decimal AporteLiquido,
    decimal ResultadoRealizado,
    int NumeroOperacoes,
    decimal CustoTotal,
    decimal ValorMercadoTotal,
    decimal PlNaoRealizadoTotal,
    decimal ProventosRecebidos,
    decimal RetornoTotal,
    IReadOnlyList<ResumoAtivoDto> Ativos,
    IReadOnlyList<VendaRealizadaDto> Vendas);

public record ImportacaoFinanceiraResumoDto(
    DateTime? UltimaImportacao,
    int DocumentosMonitorados,
    int DocumentosProcessados,
    int DocumentosComAlerta,
    string? PastaMonitorada);

public record FinancasPatrimonioDto(
    decimal ValorMercadoTotal,
    decimal CustoEstimadoTotal,
    decimal ResultadoNaoRealizadoTotal,
    EvolucaoPatrimonioDto Evolucao);

public record FinancasCarteirasDto(
    IReadOnlyList<CarteiraFinanceiraResumoDto> Carteiras);

public record FinancasImportacaoDto(
    IReadOnlyList<FinanceiroKpiDto> Kpis,
    ImportacaoFinanceiraResumoDto ImportacaoArquivos,
    DateTime? CotacoesAtualizadasEm);

public record FinancasOperacionalDto(
    IReadOnlyList<PosicaoFinanceiraDto> PosicoesAbertas,
    IReadOnlyList<AlertaConfiabilidadeDto> Alertas);

public class FinancasDashboardDto
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
    public IReadOnlyList<CotacaoAtivoDto> AtivosCotados { get; set; } = [];
    public IReadOnlyList<CarteiraFinanceiraResumoDto> Carteiras { get; set; } = [];
    public IReadOnlyList<PeriodoPerformanceDto> Periodos { get; set; } = [];
    public ImportacaoFinanceiraResumoDto? ImportacaoArquivos { get; set; }
    public DateTime? CotacoesAtualizadasEm { get; set; }
    public decimal ValorMercadoTotal { get; set; }
    public decimal CustoEstimadoTotal { get; set; }
    public decimal ResultadoNaoRealizadoTotal { get; set; }
    public string? DashboardJson { get; set; }
    public IReadOnlyList<ProventoMensalDto> ProventosMensais { get; set; } = [];
}
