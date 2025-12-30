using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using System.Threading;

namespace Sistema.INFRA.Repositories;

public class PerfilFuncionalidadeRepository(AppDbContext context) : IPerfilFuncionalidadeRepository
{
    private readonly AppDbContext _context = context;

	public async Task<IEnumerable<PerfilFuncionalidade>> BuscarPorPerfilIdAsync(int perfilId, CancellationToken cancellationToken = default)
    {
        return await _context.PerfilFuncionalidades
            .Include(pf => pf.Funcionalidade)
            .Where(pf => pf.PerfilId == perfilId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task DefinirParaPerfilAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcs, CancellationToken cancellationToken = default)
    {
        var existing = _context.PerfilFuncionalidades.Where(pf => pf.PerfilId == perfilId);
        _context.PerfilFuncionalidades.RemoveRange(existing);
        await _context.PerfilFuncionalidades.AddRangeAsync(funcs, cancellationToken);
    }
}
