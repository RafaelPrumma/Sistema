using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class ConfiguracaoService : IConfiguracaoService
{
    private readonly IUnitOfWork _uow;

    public ConfiguracaoService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento) =>
        _uow.Configuracoes.BuscarPorAgrupamentoAsync(agrupamento);

    public Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave) =>
        _uow.Configuracoes.BuscarPorChaveAsync(agrupamento, chave);

    public async Task<Configuracao> AdicionarAsync(Configuracao config)
    {
        var result = await _uow.Configuracoes.AdicionarAsync(config);
        await _uow.ConfirmarAsync();
        return result;
    }

    public async Task AtualizarAsync(Configuracao config)
    {
        await _uow.Configuracoes.AtualizarAsync(config);
        await _uow.ConfirmarAsync();
    }

    public async Task RemoverAsync(int id)
    {
        await _uow.Configuracoes.RemoverAsync(id);
        await _uow.ConfirmarAsync();
    }
}
