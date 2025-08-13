using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class TemaService : ITemaService
{
    private readonly IUnitOfWork _uow;

    public TemaService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId) =>
        _uow.Temas.BuscarPorUsuarioIdAsync(usuarioId);

    public async Task SalvarAsync(Tema tema)
    {
        var existing = await _uow.Temas.BuscarPorUsuarioIdAsync(tema.UsuarioId);
        if (existing is null)
        {
            await _uow.Temas.AdicionarAsync(tema);
        }
        else
        {
            existing.ModoEscuro = tema.ModoEscuro;
            existing.CorPrimaria = tema.CorPrimaria;
            existing.UsuarioAlteracao = tema.UsuarioAlteracao;
            await _uow.Temas.AtualizarAsync(existing);
        }
        await _uow.ConfirmarAsync();
    }
}

