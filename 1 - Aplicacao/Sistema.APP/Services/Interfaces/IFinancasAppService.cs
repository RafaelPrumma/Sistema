using Sistema.APP.DTOs;
using Sistema.CORE.Common;

namespace Sistema.APP.Services.Interfaces;

public interface IFinancasAppService
{
    Task PrepararDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasPatrimonioDto> ObterPatrimonioDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasCarteirasDto> ObterCarteirasDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasMetasDto> ObterMetasDashboardAsync(decimal aporteHipotetico = 0m, CancellationToken cancellationToken = default);

    // F-G: edição do peso-alvo por ativo-em-carteira (fecha o F-G ponta a ponta).
    Task<PesoAlvoEdicaoDto> ObterPesosAlvoAsync(CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> SalvarPesosAlvoAsync(SalvarPesosAlvoInput input, CancellationToken cancellationToken = default);
    Task<FinancasImportacaoDto> ObterImportacaoDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasPosicoesDashboardDto> ObterPosicoesDashboardAsync(CancellationToken cancellationToken = default);

    // F-Q — "Explique este valor": composição/fonte de um número do dashboard (lê só read models).
    Task<ExplicacaoPosicaoDto> ExplicarPosicaoAsync(int ativoId, CancellationToken cancellationToken = default);
    Task<ExplicacaoPatrimonioDto> ExplicarPatrimonioAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertaConfiabilidadeDto>> ObterAlertasDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasProventosDashboardDto> ObterProventosDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasCalendarioProventosDashboardDto> ObterCalendarioProventosDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasReconciliacaoDto> ObterReconciliacaoDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasSaudeCotacoesDto> ObterSaudeCotacoesDashboardAsync(CancellationToken cancellationToken = default);
    Task<FinancasDashboardDto> ObterDashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<DocumentoFinanceiroDto>> BuscarDocumentosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<(DocumentoFinanceiroDto? Documento, IReadOnlyList<ConteudoBrutoFinanceiroDto> Conteudos)> ObterDocumentoAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<OperacaoB3Dto>> BuscarOperacoesB3Async(int page, int pageSize, string? termo, int? ano, string? classe, CancellationToken cancellationToken = default);
    Task<PagedResult<TransacaoCriptoDto>> BuscarTransacoesCriptoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosicaoFinanceiraDto>> BuscarPosicoesAsync(bool? somenteAbertas, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertaConfiabilidadeDto>> BuscarAlertasAsync(CancellationToken cancellationToken = default);
    Task<int> ImportarPastaMonitoradaAsync(int? usuarioId = null, CancellationToken cancellationToken = default);
    Task AtualizarCotacoesAsync(CancellationToken cancellationToken = default);
    Task AtualizarProventosAsync(CancellationToken cancellationToken = default);
    Task<ProventosPaginaDto> BuscarProventosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);

    Task<EvolucaoPatrimonioDto> ObterEvolucaoPatrimonioAsync(CancellationToken cancellationToken = default);
    Task<ResumoAnaliticoDto> ObterResumoAnaliticoAsync(DateTime? inicio, DateTime? fim, CancellationToken cancellationToken = default);

    // Apuração de IR (ganho de capital B3+cripto, Bens e Direitos, proventos) do ano-calendário.
    Task<ApuracaoIrDto> ObterApuracaoIrAsync(int ano, CancellationToken cancellationToken = default);
    // Exporta a apuração de IR em .xlsx (uma aba por bloco) — a "cola" da declaração.
    Task<byte[]> ExportarApuracaoIrExcelAsync(int ano, CancellationToken cancellationToken = default);

    Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default);
    Task<PagedResult<TransacaoFinanceiraDto>> BuscarTransacoesAsync(int page, int pageSize, string? termo, string? origem, CancellationToken cancellationToken = default);
    Task<DataTablesResponse<TransacaoFinanceiraDto>> BuscarTransacoesDataTableAsync(DataTablesRequest request, string? origem, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> RegistrarTransacaoManualAsync(NovaTransacaoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> EditarTransacaoAsync(int id, NovaTransacaoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> ExcluirTransacaoAsync(int id, CancellationToken cancellationToken = default);

    // Eventos corporativos — CRUD manual.
    Task<PagedResult<EventoCorporativoDto>> BuscarEventosCorporativosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> RegistrarEventoCorporativoManualAsync(NovoEventoCorporativoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> EditarEventoCorporativoAsync(int id, NovoEventoCorporativoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> ExcluirEventoCorporativoAsync(int id, CancellationToken cancellationToken = default);

    // Alertas de preço (F-H) — CRUD manual.
    Task<PagedResult<AlertaPrecoDto>> BuscarAlertasPrecoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> RegistrarAlertaPrecoAsync(NovoAlertaPrecoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> EditarAlertaPrecoAsync(int id, NovoAlertaPrecoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> ExcluirAlertaPrecoAsync(int id, CancellationToken cancellationToken = default);
}
