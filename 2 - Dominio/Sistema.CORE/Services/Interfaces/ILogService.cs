using Sistema.CORE.Entities;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface ILogService
{
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default);

    Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default);
}

