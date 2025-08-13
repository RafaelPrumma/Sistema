using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class TemaRepository : ITemaRepository
{
    private readonly AppDbContext _context;

    public TemaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId) =>
        await _context.Temas.FirstOrDefaultAsync(l => l.UsuarioId == usuarioId);

    public Task<Tema> AdicionarAsync(Tema tema)
    {
        _context.Temas.Add(tema);
        return Task.FromResult(tema);
    }

    public Task AtualizarAsync(Tema tema)
    {
        _context.Temas.Update(tema);
        return Task.CompletedTask;
    }
}

