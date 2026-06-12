using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Common;
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

    public Task<DataTablesResponse<Log>> BuscarDataTableAsync(DataTablesRequest request, DateTime? inicio, DateTime? fim, LogTipo? tipo, LogModulo? modulo, CancellationToken cancellationToken = default)
    {
        var query = _context.Logs.AsNoTracking().AsQueryable();
        if (inicio.HasValue)
            query = query.Where(l => l.DataOperacao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(l => l.DataOperacao <= fim.Value);
        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);
        if (modulo.HasValue)
            query = query.Where(l => l.Modulo == modulo.Value);

        // Chaves batem com os campos camelCase do LogDto (orderColumn enviado pelo DataTables).
        var ordenacoes = new Dictionary<string, Func<IQueryable<Log>, bool, IOrderedQueryable<Log>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["dataOperacao"] = (q, desc) => desc ? q.OrderByDescending(l => l.DataOperacao) : q.OrderBy(l => l.DataOperacao),
            ["modulo"] = (q, desc) => desc ? q.OrderByDescending(l => l.Modulo) : q.OrderBy(l => l.Modulo),
            ["tipo"] = (q, desc) => desc ? q.OrderByDescending(l => l.Tipo) : q.OrderBy(l => l.Tipo),
            ["usuario"] = (q, desc) => desc ? q.OrderByDescending(l => l.Usuario) : q.OrderBy(l => l.Usuario),
            ["entidade"] = (q, desc) => desc ? q.OrderByDescending(l => l.Entidade) : q.OrderBy(l => l.Entidade),
            ["operacao"] = (q, desc) => desc ? q.OrderByDescending(l => l.Operacao) : q.OrderBy(l => l.Operacao)
        };

        return query.ToDataTablesAsync(
            request,
            (q, termo) => q.Where(l => l.Entidade.Contains(termo) || l.Operacao.Contains(termo) || l.Mensagem.Contains(termo) || l.Usuario.Contains(termo)),
            ordenacoes,
            "dataOperacao",
            cancellationToken);
    }
}
