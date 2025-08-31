using Sistema.CORE.Entities;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface ITemaRepository
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<Tema> AdicionarAsync(Tema tema, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Tema tema);
}

