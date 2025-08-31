using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using System.Threading;

namespace Sistema.INFRA.Repositories;

public class FuncionalidadeRepository : IFuncionalidadeRepository
{
    private readonly AppDbContext _context;

    public FuncionalidadeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Funcionalidade> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default)
    {
        await _context.Funcionalidades.AddAsync(func, cancellationToken);
        return func;
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var obj = await _context.Funcionalidades.FindAsync(new object?[] { id }, cancellationToken);
        if (obj is null) return;
        _context.Funcionalidades.Remove(obj);
    }

    public async Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Funcionalidades.FindAsync(new object?[] { id }, cancellationToken);
    }

    public Task AtualizarAsync(Funcionalidade func)
    {
        _context.Funcionalidades.Update(func);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Funcionalidades.AsNoTracking().OrderBy(f => f.Id);
        return await query.ToPagedResultAsync(page, pageSize, cancellationToken);
    }
}
