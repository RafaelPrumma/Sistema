using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface ILogRepository
{
    Task AdicionarAsync(Log log, CancellationToken cancellationToken = default);
    Task AdicionarEmLoteAsync(IEnumerable<Log> logs, CancellationToken cancellationToken = default);
    Task<int> RemoverAntesDeAsync(LogModulo modulo, DateTime dataLimiteUtc, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, LogModulo? modulo = null, CancellationToken cancellationToken = default);
}
