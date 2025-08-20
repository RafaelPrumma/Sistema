using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Sistema.CORE.Interfaces;

namespace Sistema.INFRA.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task EnviarAsync(string destinatario, string assunto, string mensagem)
    {
        var host = _config["Smtp:Host"];
        var portStr = _config["Smtp:Port"];
        int port = 25;
        if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out var parsed))
        {
            port = parsed;
        }
        var user = _config["Smtp:Username"];
        var pass = _config["Smtp:Password"];
        var from = _config["Smtp:From"];

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };
        using var mail = new MailMessage(from!, destinatario, assunto, mensagem);
        await client.SendMailAsync(mail);
    }
}
