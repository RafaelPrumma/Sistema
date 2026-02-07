using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services.Interfaces;

namespace Sistema.CORE.Services;

public class TemaService(IUnitOfWork uow) : ITemaDomainService
{
    private readonly IUnitOfWork _uow = uow;

	public Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        _uow.Temas.BuscarPorUsuarioIdAsync(usuarioId, cancellationToken);

    public async Task SalvarAsync(Tema tema, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.Temas.BuscarPorUsuarioIdAsync(tema.UsuarioId, cancellationToken);
        if (existing is null)
        {
            await _uow.Temas.AdicionarAsync(tema, cancellationToken);
        }
        else
        {
            existing.ModoEscuro = tema.ModoEscuro;
            existing.CorHeader = tema.CorHeader;
            existing.CorBarraEsquerda = tema.CorBarraEsquerda;
            existing.CorBarraDireita = tema.CorBarraDireita;
            existing.CorFooter = tema.CorFooter;
            existing.HeaderFixo = tema.HeaderFixo;
            existing.FooterFixo = tema.FooterFixo;
            existing.MenuLateralExpandido = tema.MenuLateralExpandido;
            existing.UsuarioAlteracao = tema.UsuarioAlteracao;
            await _uow.Temas.AtualizarAsync(existing);
        }
        await _uow.ConfirmarAsync(cancellationToken);
    }
}

