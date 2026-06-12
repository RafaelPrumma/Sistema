using Sistema.APP.DTOs;

namespace Sistema.APP.Services.Interfaces;

public interface IMinhasFinancasMarketDataService
{
    Task AtualizarCotacoesAsync(bool force = false, CancellationToken cancellationToken = default);

    // Valida um ticker contra as APIs (Brapi para ação/FII/ETF/BDR, Binance para cripto) e
    // devolve nome + classe inferida. Usado pelo lançamento manual estilo Google Finance.
    Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default);

    // Cota e importa o histórico de um ativo específico (recém-criado no lançamento manual).
    Task GarantirCotacaoAtivoAsync(int ativoId, CancellationToken cancellationToken = default);
}
