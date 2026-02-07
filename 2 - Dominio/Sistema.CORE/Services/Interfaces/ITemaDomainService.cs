using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Services.Interfaces;

public interface ITemaDomainService
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task SalvarAsync(Tema tema, CancellationToken cancellationToken = default);
}

