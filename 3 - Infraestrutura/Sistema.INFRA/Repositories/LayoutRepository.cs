using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class LayoutRepository : ILayoutRepository
{
    private readonly AppDbContext _context;

    public LayoutRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Layout?> BuscarPorUsuarioIdAsync(int usuarioId) =>
        await _context.Layouts.FirstOrDefaultAsync(l => l.UsuarioId == usuarioId);

    public Task<Layout> AdicionarAsync(Layout layout)
    {
        _context.Layouts.Add(layout);
        return Task.FromResult(layout);
    }

    public Task AtualizarAsync(Layout layout)
    {
        _context.Layouts.Update(layout);
        return Task.CompletedTask;
    }
}

