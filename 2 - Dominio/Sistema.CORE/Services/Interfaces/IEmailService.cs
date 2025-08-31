using System.Threading;
using System.Threading.Tasks;

namespace Sistema.CORE.Interfaces;

public interface IEmailService
{
    Task EnviarAsync(string destinatario, string assunto, string mensagem, CancellationToken cancellationToken = default);
}
