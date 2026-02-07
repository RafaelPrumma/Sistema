using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class LogAppService(Sistema.CORE.Services.Interfaces.ILogService domainService) : ILogService
{
    private readonly Sistema.CORE.Services.Interfaces.ILogService _domainService = domainService;

    public Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default) =>
        _domainService.BuscarFiltradosAsync(inicio, fim, tipo, cancellationToken);

    public Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default) =>
        _domainService.RegistrarAsync(entidade, operacao, sucesso, mensagem, tipo, usuario, detalhe, cancellationToken);
}
