using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface ILogRepository
{
    Task AdicionarAsync(Log log, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default);
}
