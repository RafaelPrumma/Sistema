namespace Sistema.APP.Services.Interfaces;

public interface IEmailService
{
    Task EnviarAsync(string destinatario, string assunto, string mensagem, CancellationToken cancellationToken = default);
}
