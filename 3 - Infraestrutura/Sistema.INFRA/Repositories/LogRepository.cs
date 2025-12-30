using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class LogRepository(AppDbContext context) : ILogRepository
{
    private readonly AppDbContext _context = context;

	public async Task AdicionarAsync(Log log, CancellationToken cancellationToken = default)
    {
        await _context.Logs.AddAsync(log, cancellationToken);
    }

    public async Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default)
    {
        var query = _context.Logs.AsQueryable();
        if (inicio.HasValue)
            query = query.Where(l => l.DataOperacao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(l => l.DataOperacao <= fim.Value);
        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);
        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }
}
