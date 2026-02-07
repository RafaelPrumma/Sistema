using System.Threading;
using System.Threading.Tasks;

namespace Sistema.CORE.Services.Interfaces;

public interface IEmailDomainService
{
    Task EnviarAsync(string destinatario, string assunto, string mensagem, CancellationToken cancellationToken = default);
}
