using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services.Interfaces;

namespace Sistema.CORE.Services;

/// <summary>
/// Serviço responsável por gerenciar configurações do sistema e delegar persistência ao repositório.
/// </summary>
public class ConfiguracaoService(IUnitOfWork uow) : IConfiguracaoService
{
    private readonly IUnitOfWork _uow = uow;

    /// <summary>
    /// Busca configurações pertencentes a um agrupamento específico.
    /// </summary>
    /// <param name="agrupamento">Agrupamento utilizado como chave de filtro.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Enumerable com as configurações encontradas.</returns>
    public Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento, CancellationToken cancellationToken = default) =>
        _uow.Configuracoes.BuscarPorAgrupamentoAsync(agrupamento, cancellationToken);

    /// <summary>
    /// Obtém uma configuração específica a partir do agrupamento e da chave informados.
    /// </summary>
    /// <param name="agrupamento">Agrupamento ao qual a configuração pertence.</param>
    /// <param name="chave">Chave exclusiva dentro do agrupamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Configuração localizada ou nula quando inexistente.</returns>
    public Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave, CancellationToken cancellationToken = default) =>
        _uow.Configuracoes.BuscarPorChaveAsync(agrupamento, chave, cancellationToken);

    /// <summary>
    /// Adiciona uma nova configuração ao repositório e persiste a alteração.
    /// </summary>
    /// <param name="config">Entidade de configuração a ser incluída.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Entidade persistida com seu identificador.</returns>
    public async Task<Configuracao> AdicionarAsync(Configuracao config, CancellationToken cancellationToken = default)
    {
        var result = await _uow.Configuracoes.AdicionarAsync(config, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Atualiza uma configuração existente e confirma a transação.
    /// </summary>
    /// <param name="config">Configuração já existente com dados atualizados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task AtualizarAsync(Configuracao config, CancellationToken cancellationToken = default)
    {
        await _uow.Configuracoes.AtualizarAsync(config);
        await _uow.ConfirmarAsync(cancellationToken);
    }

    /// <summary>
    /// Remove uma configuração pelo identificador e persiste a exclusão.
    /// </summary>
    /// <param name="id">Identificador da configuração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        await _uow.Configuracoes.RemoverAsync(id, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
    }
}
