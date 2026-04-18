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

    public async Task AdicionarEmLoteAsync(IEnumerable<Log> logs, CancellationToken cancellationToken = default)
    {
        await _context.Logs.AddRangeAsync(logs, cancellationToken);
    }

    public async Task<int> RemoverAntesDeAsync(LogModulo modulo, DateTime dataLimiteUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Logs
            .Where(l => l.Modulo == modulo && l.DataOperacao < dataLimiteUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, LogModulo? modulo = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Logs.AsQueryable();
        if (inicio.HasValue)
            query = query.Where(l => l.DataOperacao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(l => l.DataOperacao <= fim.Value);
        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);
        if (modulo.HasValue)
            query = query.Where(l => l.Modulo == modulo.Value);

        return await query.AsNoTracking().OrderByDescending(l => l.DataOperacao).ToListAsync(cancellationToken);
    }
}
