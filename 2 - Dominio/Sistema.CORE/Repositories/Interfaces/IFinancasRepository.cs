using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IFinancasRepository
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

    // Proventos (dividendos/JCP/rendimentos), independentes de carga — incluem os buscados na Brapi.
    Task<PagedResult<RendimentoInvestimento>> BuscarProventosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RendimentoInvestimento>> BuscarProventosPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtivoFinanceiro>> BuscarAtivosComPosicaoAbertaAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CotacaoAtivoFinanceiro>> BuscarCotacoesAtuaisAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrecoHistoricoAtivoFinanceiro>> BuscarHistoricoPrecosAsync(DateTime inicio, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarteiraFinanceira>> BuscarCarteirasComAtivosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentoFinanceiro>> BuscarDocumentosMonitoradosAsync(CancellationToken cancellationToken = default);
    Task<ImportacaoFinanceiraArquivo?> ObterUltimaImportacaoArquivoAsync(CancellationToken cancellationToken = default);

    // Tabela única de transações (fonte de verdade): importação materializada + lançamentos manuais.
    Task<IReadOnlyList<TransacaoFinanceira>> BuscarTodasTransacoesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TransacaoFinanceira>> BuscarTransacoesAsync(int page, int pageSize, string? termo, OrigemTransacao? origem, CancellationToken cancellationToken = default);
    Task<DataTablesResponse<TransacaoFinanceira>> BuscarTransacoesDataTableAsync(DataTablesRequest request, OrigemTransacao? origem, CancellationToken cancellationToken = default);
    Task<TransacaoFinanceira?> ObterTransacaoAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> TransacaoExisteAsync(string duplicateGroupKey, CancellationToken cancellationToken = default);
    Task AdicionarTransacaoAsync(TransacaoFinanceira transacao, CancellationToken cancellationToken = default);
    void AtualizarTransacao(TransacaoFinanceira transacao);
    void RemoverTransacao(TransacaoFinanceira transacao);
    Task<AtivoFinanceiro?> ObterAtivoPorChaveOuTickerAsync(string chaveOuTicker, CancellationToken cancellationToken = default);
    Task AdicionarAtivoAsync(AtivoFinanceiro ativo, CancellationToken cancellationToken = default);

    // Eventos corporativos (split/grupamento/bonificacao) — CRUD manual.
    Task<PagedResult<EventoCorporativo>> BuscarEventosCorporativosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default);
    Task<EventoCorporativo?> ObterEventoCorporativoAsync(int id, CancellationToken cancellationToken = default);
    Task AdicionarEventoCorporativoAsync(EventoCorporativo evento, CancellationToken cancellationToken = default);
    void AtualizarEventoCorporativo(EventoCorporativo evento);
    void RemoverEventoCorporativo(EventoCorporativo evento);
}
