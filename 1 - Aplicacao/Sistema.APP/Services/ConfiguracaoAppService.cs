using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class ConfiguracaoAppService(Sistema.CORE.Services.Interfaces.IConfiguracaoDomainService domainService) : IConfiguracaoAppService
{
    private readonly Sistema.CORE.Services.Interfaces.IConfiguracaoDomainService _domainService = domainService;

    public Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorAgrupamentoAsync(agrupamento, cancellationToken);

    public Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorChaveAsync(agrupamento, chave, cancellationToken);

    public Task<Configuracao> AdicionarAsync(Configuracao config, CancellationToken cancellationToken = default) =>
        _domainService.AdicionarAsync(config, cancellationToken);

    public Task AtualizarAsync(Configuracao config, CancellationToken cancellationToken = default) =>
        _domainService.AtualizarAsync(config, cancellationToken);

    public Task RemoverAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.RemoverAsync(id, cancellationToken);
}
