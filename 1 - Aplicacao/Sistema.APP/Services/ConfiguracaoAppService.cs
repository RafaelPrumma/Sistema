using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;

namespace Sistema.APP.Services;

public class ConfiguracaoAppService(IUnitOfWork uow) : IConfiguracaoAppService
{
    private readonly IUnitOfWork _uow = uow;

    public Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento, CancellationToken cancellationToken = default) =>
        _uow.Configuracoes.BuscarPorAgrupamentoAsync(agrupamento, cancellationToken);

    public Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave, CancellationToken cancellationToken = default) =>
        _uow.Configuracoes.BuscarPorChaveAsync(agrupamento, chave, cancellationToken);

    public async Task<Configuracao> AdicionarAsync(Configuracao config, CancellationToken cancellationToken = default)
    {
        var result = await _uow.Configuracoes.AdicionarAsync(config, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return result;
    }

    public async Task AtualizarAsync(Configuracao config, CancellationToken cancellationToken = default)
    {
        await _uow.Configuracoes.AtualizarAsync(config);
        await _uow.ConfirmarAsync(cancellationToken);
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        await _uow.Configuracoes.RemoverAsync(id, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
    }
}
