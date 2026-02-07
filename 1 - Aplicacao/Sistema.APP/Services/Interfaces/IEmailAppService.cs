namespace Sistema.APP.Services.Interfaces;

public interface IEmailAppService
{
    Task EnviarAsync(string destinatario, string assunto, string mensagem, CancellationToken cancellationToken = default);
}
