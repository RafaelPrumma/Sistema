using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class ConfiguracaoSeed
{
	public static IEnumerable<Configuracao> Get()
	{
		return new List<Configuracao>
		{
			new()
			{
				Id = 1,
				Agrupamento = "AzureAd",
				Chave = "TenantId",
				Valor = "ab4ade51-fe57-4d7b-8944-8013eea0ac73",
				Tipo = ConfiguracaoTipo.Texto,
				UsuarioInclusao = "seed"
			},
			new()
			{
				Id = 2,
				Agrupamento = "AzureAd",
				Chave = "ClientId",
				Valor = "2cdea501-4ef5-4789-8c72-a3b4bf953327",
				Tipo = ConfiguracaoTipo.Texto,
				UsuarioInclusao = "seed"
			},
			new()
			{
				Id = 3,
				Agrupamento = "AzureAd",
				Chave = "ClientSecret",
				Valor = "6PW8Q~ErW~9a7Inilr5yyatDkQLc4Lk3VLvx5a8h",
				Tipo = ConfiguracaoTipo.Password,
				UsuarioInclusao = "seed"
			},
			new()
			{
				Id = 4,
				Agrupamento = "AzureAd",
				Chave = "SenderEmail",
				Valor = "desenvolvimento@prumma.com.br",
				Tipo = ConfiguracaoTipo.Email,
				UsuarioInclusao = "seed"
			}
		};
	}
}