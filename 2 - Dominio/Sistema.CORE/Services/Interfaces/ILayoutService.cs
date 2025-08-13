using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILayoutService
{
    Task<Layout?> BuscarPorUsuarioIdAsync(int usuarioId);
    Task SalvarAsync(Layout layout);
}

