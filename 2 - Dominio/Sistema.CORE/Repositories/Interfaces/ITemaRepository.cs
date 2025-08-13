using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ITemaRepository
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId);
    Task<Tema> AdicionarAsync(Tema tema);
    Task AtualizarAsync(Tema tema);
}

