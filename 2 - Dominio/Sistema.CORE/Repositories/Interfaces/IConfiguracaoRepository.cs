using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IConfiguracaoRepository
{
    Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento, CancellationToken cancellationToken = default);
    Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave, CancellationToken cancellationToken = default);
    Task<Configuracao> AdicionarAsync(Configuracao config, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Configuracao config);
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
