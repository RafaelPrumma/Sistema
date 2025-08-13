using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ITemaService
{
    Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId);
    Task SalvarAsync(Tema tema);
}

