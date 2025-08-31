using Sistema.CORE.Entities;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface ILogRepository
{
    Task AdicionarAsync(Log log, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default);
}
