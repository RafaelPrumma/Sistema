using Sistema.APP.DTOs;

namespace Sistema.APP.Services.Interfaces;

public interface IFinancasMarketDataService
{
    Task AtualizarCotacoesAsync(bool force = false, CancellationToken cancellationToken = default);

    // Busca na Brapi o histórico de proventos (dividendo/JCP/rendimento) dos ativos B3 em carteira e
    // cruza com a posição na data-com para estimar o valor recebido. Idempotente (chave natural).
    Task AtualizarProventosAsync(bool force = false, CancellationToken cancellationToken = default);

    // Busca na Brapi (dividendsData.stockDividends) desdobramentos/grupamentos dos ativos B3 em carteira
    // e faz upsert idempotente de EventoCorporativo (dedup por janela contra os já semeados/manuais).
    Task AtualizarEventosCorporativosAsync(bool force = false, CancellationToken cancellationToken = default);

    // Valida um ticker contra as APIs (Brapi para ação/FII/ETF/BDR, Binance para cripto) e
    // devolve nome + classe inferida. Usado pelo lançamento manual estilo Google Finance.
    Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default);

    // Cota e importa o histórico de um ativo específico (recém-criado no lançamento manual).
    Task GarantirCotacaoAtivoAsync(int ativoId, CancellationToken cancellationToken = default);
}
