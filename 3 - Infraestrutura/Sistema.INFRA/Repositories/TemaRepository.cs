using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using System.Threading;

namespace Sistema.INFRA.Repositories;

public class TemaRepository : ITemaRepository
{
    private readonly AppDbContext _context;

    public TemaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        await _context.Temas
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.UsuarioId == usuarioId, cancellationToken);

    public async Task<Tema> AdicionarAsync(Tema tema, CancellationToken cancellationToken = default)
    {
        await _context.Temas.AddAsync(tema, cancellationToken);
        return tema;
    }

    public Task AtualizarAsync(Tema tema)
    {
        _context.Temas.Update(tema);
        return Task.CompletedTask;
    }
}

