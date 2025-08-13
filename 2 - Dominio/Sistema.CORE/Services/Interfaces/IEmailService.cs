using System.Threading.Tasks;

namespace Sistema.CORE.Interfaces;

public interface IEmailService
{
    Task EnviarAsync(string destinatario, string assunto, string mensagem);
}
