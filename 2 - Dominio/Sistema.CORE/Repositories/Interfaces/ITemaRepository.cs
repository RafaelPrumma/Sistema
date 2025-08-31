using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface ITemaRepository
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<Tema> AdicionarAsync(Tema tema, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Tema tema);
}

