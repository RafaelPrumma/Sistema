using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILogService
{
    Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null);
}
