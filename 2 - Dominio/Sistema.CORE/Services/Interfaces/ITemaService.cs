using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ITemaService
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task SalvarAsync(Tema tema, CancellationToken cancellationToken = default);
}

