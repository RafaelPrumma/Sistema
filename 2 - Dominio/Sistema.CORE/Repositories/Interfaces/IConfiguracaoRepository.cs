using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IConfiguracaoRepository
{
    Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento);
    Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave);
    Task<Configuracao> AdicionarAsync(Configuracao config);
    Task AtualizarAsync(Configuracao config);
    Task RemoverAsync(int id);
}
