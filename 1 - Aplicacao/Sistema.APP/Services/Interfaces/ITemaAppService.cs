using Sistema.CORE.Entities;

namespace Sistema.APP.Services.Interfaces;

public interface ITemaAppService
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task SalvarAsync(Tema tema, CancellationToken cancellationToken = default);
}
