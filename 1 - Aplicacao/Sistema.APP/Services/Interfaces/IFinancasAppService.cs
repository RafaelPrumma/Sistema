using Sistema.APP.DTOs;
using Sistema.CORE.Common;

namespace Sistema.APP.Services.Interfaces;

public interface IFinancasAppService
{
    Task<FinancasDashboardDto> ObterDashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<DocumentoFinanceiroDto>> BuscarDocumentosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<(DocumentoFinanceiroDto? Documento, IReadOnlyList<ConteudoBrutoFinanceiroDto> Conteudos)> ObterDocumentoAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<OperacaoB3Dto>> BuscarOperacoesB3Async(int page, int pageSize, string? termo, int? ano, string? classe, CancellationToken cancellationToken = default);
    Task<PagedResult<TransacaoCriptoDto>> BuscarTransacoesCriptoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosicaoFinanceiraDto>> BuscarPosicoesAsync(bool? somenteAbertas, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertaConfiabilidadeDto>> BuscarAlertasAsync(CancellationToken cancellationToken = default);
    Task ImportarPastaMonitoradaAsync(int? usuarioId = null, CancellationToken cancellationToken = default);
    Task AtualizarCotacoesAsync(CancellationToken cancellationToken = default);
    Task AtualizarProventosAsync(CancellationToken cancellationToken = default);
    Task<ProventosPaginaDto> BuscarProventosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);

    Task<EvolucaoPatrimonioDto> ObterEvolucaoPatrimonioAsync(CancellationToken cancellationToken = default);
    Task<ResumoAnaliticoDto> ObterResumoAnaliticoAsync(DateTime? inicio, DateTime? fim, CancellationToken cancellationToken = default);
    Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default);
    Task<PagedResult<TransacaoFinanceiraDto>> BuscarTransacoesAsync(int page, int pageSize, string? termo, string? origem, CancellationToken cancellationToken = default);
    Task<DataTablesResponse<TransacaoFinanceiraDto>> BuscarTransacoesDataTableAsync(DataTablesRequest request, string? origem, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> RegistrarTransacaoManualAsync(NovaTransacaoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> EditarTransacaoAsync(int id, NovaTransacaoInput input, CancellationToken cancellationToken = default);
    Task<ResultadoOperacao> ExcluirTransacaoAsync(int id, CancellationToken cancellationToken = default);
}
