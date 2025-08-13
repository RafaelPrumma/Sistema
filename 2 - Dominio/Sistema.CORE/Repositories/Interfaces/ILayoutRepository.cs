using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILayoutRepository
{
    Task<Layout?> BuscarPorUsuarioIdAsync(int usuarioId);
    Task<Layout> AdicionarAsync(Layout layout);
    Task AtualizarAsync(Layout layout);
}

