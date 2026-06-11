using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IMinhasFinancasRepository
{
    Task<bool> ExisteCargaComShaAsync(string sha256, CancellationToken cancellationToken = default);
    Task<CargaFinanceira?> ObterCargaMaisRecenteAsync(CancellationToken cancellationToken = default);
    Task AdicionarCargaAsync(CargaFinanceira carga, CancellationToken cancellationToken = default);

    Task<PagedResult<DocumentoFinanceiro>> BuscarDocumentosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<DocumentoFinanceiro?> ObterDocumentoAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConteudoBrutoFinanceiro>> BuscarConteudosDocumentoAsync(int documentoId, CancellationToken cancellationToken = default);

    Task<PagedResult<OperacaoB3>> BuscarOperacoesB3Async(int page, int pageSize, string? termo, int? ano, string? classe, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperacaoB3>> BuscarUltimasOperacoesB3Async(int quantidade, CancellationToken cancellationToken = default);

    Task<PagedResult<TransacaoCripto>> BuscarTransacoesCriptoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransacaoCripto>> BuscarUltimasTransacoesCriptoAsync(int quantidade, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstimativaPosicaoCarteira>> BuscarPosicoesAsync(bool? somenteAbertas, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertaConfiabilidade>> BuscarAlertasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgregadoFinanceiro>> BuscarAgregadosAsync(string dimensao, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RendimentoInvestimento>> BuscarRendimentosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtivoFinanceiro>> BuscarAtivosComPosicaoAbertaAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CotacaoAtivoFinanceiro>> BuscarCotacoesAtuaisAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrecoHistoricoAtivoFinanceiro>> BuscarHistoricoPrecosAsync(DateTime inicio, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarteiraFinanceira>> BuscarCarteirasComAtivosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentoFinanceiro>> BuscarDocumentosMonitoradosAsync(CancellationToken cancellationToken = default);
    Task<ImportacaoFinanceiraArquivo?> ObterUltimaImportacaoArquivoAsync(CancellationToken cancellationToken = default);
}
