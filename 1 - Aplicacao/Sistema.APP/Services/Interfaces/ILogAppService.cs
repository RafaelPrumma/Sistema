using Sistema.CORE.Entities;

namespace Sistema.APP.Services.Interfaces;

public interface ILogAppService
{
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, LogModulo? modulo = null, CancellationToken cancellationToken = default);
    Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default);
    Task RegistrarPorModuloAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, LogModulo modulo, string? detalhe = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Log>> BuscarAcessoAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> BuscarComunicacaoAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> BuscarAdministracaoAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default);

    Task RegistrarAcessoAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default);
    Task RegistrarComunicacaoAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default);
    Task RegistrarAdministracaoAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default);
}
