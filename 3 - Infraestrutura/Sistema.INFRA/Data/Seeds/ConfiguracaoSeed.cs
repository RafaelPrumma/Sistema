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
                Agrupamento = "AzureAd",
                Chave = "TenantId",
                Valor = "",
                Tipo = ConfiguracaoTipo.Texto,
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "AzureAd",
                Chave = "ClientId",
                Valor = "",
                Tipo = ConfiguracaoTipo.Texto,
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "AzureAd",
                Chave = "ClientSecret",
                Valor = "",
                Tipo = ConfiguracaoTipo.Password,
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "AzureAd",
                Chave = "SenderEmail",
                Valor = "desenvolvimento@prumma.com.br",
                Tipo = ConfiguracaoTipo.Email,
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "LogsRetencao",
                Chave = "AcessoMeses",
                Valor = "3",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Tempo de retenção do log de acesso, em meses.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "LogsRetencao",
                Chave = "ComunicacaoMeses",
                Valor = "6",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Tempo de retenção do log de comunicação, em meses.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "LogsRetencao",
                Chave = "AdministracaoMeses",
                Valor = "12",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Tempo de retenção do log de administração, em meses.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "LogsRetencao",
                Chave = "GeralMeses",
                Valor = "12",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Tempo de retenção do log geral, em meses.",
                UsuarioInclusao = "seed"
            }
        };
    }
}
