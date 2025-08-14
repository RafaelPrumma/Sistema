using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
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
		var tenantId = _config["AzureAd:TenantId"];
		var clientId = _config["AzureAd:ClientId"];
		var clientSecret = _config["AzureAd:ClientSecret"];
		var sender = _config["AzureAd:SenderEmail"];

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

		await graphClient.Users[sender].SendMail.PostAsync(request);
	}
}
