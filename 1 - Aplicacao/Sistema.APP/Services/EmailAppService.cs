using Sistema.APP.Services.Interfaces;

namespace Sistema.APP.Services;

public class EmailAppService(Sistema.CORE.Services.Interfaces.IEmailService domainService) : IEmailService
{
    private readonly Sistema.CORE.Services.Interfaces.IEmailService _domainService = domainService;

    public Task EnviarAsync(string destinatario, string assunto, string mensagem, CancellationToken cancellationToken = default) =>
        _domainService.EnviarAsync(destinatario, assunto, mensagem, cancellationToken);
}
