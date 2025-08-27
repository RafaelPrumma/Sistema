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
				Valor = "",
				Tipo = ConfiguracaoTipo.Texto,
				UsuarioInclusao = "seed"
			},
			new()
			{
				Id = 2,
				Agrupamento = "AzureAd",
				Chave = "ClientId",
				Valor = "",
				Tipo = ConfiguracaoTipo.Texto,
				UsuarioInclusao = "seed"
			},
			new()
			{
				Id = 3,
				Agrupamento = "AzureAd",
				Chave = "ClientSecret",
				Valor = "",
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