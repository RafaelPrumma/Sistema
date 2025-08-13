using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class LayoutService : ILayoutService
{
    private readonly IUnitOfWork _uow;

    public LayoutService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<Layout?> BuscarPorUsuarioIdAsync(int usuarioId) =>
        _uow.Layouts.BuscarPorUsuarioIdAsync(usuarioId);

    public async Task SalvarAsync(Layout layout)
    {
        var existing = await _uow.Layouts.BuscarPorUsuarioIdAsync(layout.UsuarioId);
        if (existing is null)
        {
            await _uow.Layouts.AdicionarAsync(layout);
        }
        else
        {
            existing.ModoEscuro = layout.ModoEscuro;
            existing.CorPrimaria = layout.CorPrimaria;
            existing.UsuarioAlteracao = layout.UsuarioAlteracao;
            await _uow.Layouts.AtualizarAsync(existing);
        }
        await _uow.ConfirmarAsync();
    }
}

