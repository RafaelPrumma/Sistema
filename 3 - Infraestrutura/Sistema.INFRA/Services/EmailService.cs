using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Sistema.CORE.Services.Interfaces;
using System.Threading;

namespace Sistema.INFRA.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string assunto, string mensagem, CancellationToken cancellationToken = default)
    {
        var tenantId = _options.TenantId;
        var clientId = _options.ClientId;
        var clientSecret = _options.ClientSecret;
        var sender = _options.SenderEmail;

        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential, scopes);

        var message = new Message
        {
            Subject = assunto,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = mensagem
            },
            ToRecipients = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = destinatario
                    }
                }
            }
        };
        var request = new SendMailPostRequestBody
        {
            Message = message,
            SaveToSentItems = false
        };

        try
        {
            await graphClient.Users[sender].SendMail.PostAsync(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail");
            throw;
        }
    }
}
