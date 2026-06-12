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
                Chave = "FinanceiroMeses",
                Valor = "24",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Tempo de retenção do log financeiro, em meses.",
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
            },
            new()
            {
                Agrupamento = "Sistema",
                Chave = "NomeAplicacao",
                Valor = "Sistema",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Nome exibido na aplicacao e documentacao.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "Sistema",
                Chave = "AmbientePadrao",
                Valor = "Development",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Ambiente padrao usado para operacao local.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "Aparencia",
                Chave = "PresetPadrao",
                Valor = "executivo",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Preset visual sugerido para novos usuarios.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "Mensagens",
                Chave = "PageSizeFeed",
                Valor = "20",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Quantidade padrao de itens no feed de comunicacao.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "MinhasFinancas",
                Chave = "ImportacaoAutomaticaHabilitada",
                Valor = "true",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Permite carregar a base financeira configurada no ambiente de desenvolvimento.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "MinhasFinancas",
                Chave = "MarketData:RefreshSeconds",
                Valor = "60",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Intervalo (em segundos) entre as atualizacoes automaticas de cotacoes.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "MinhasFinancas",
                Chave = "MarketData:BackgroundEnabled",
                Valor = "true",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Liga/desliga o job recorrente de atualizacao de cotacoes.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "MinhasFinancas",
                Chave = "MarketData:BrapiToken",
                Valor = "",
                Tipo = ConfiguracaoTipo.Password,
                Descricao = "Token da Brapi para cotacoes e historico completos de acoes B3.",
                UsuarioInclusao = "seed"
            },
            new()
            {
                Agrupamento = "MinhasFinancas",
                Chave = "WatchedFolderPath",
                Valor = "",
                Tipo = ConfiguracaoTipo.Texto,
                Descricao = "Pasta monitorada de onde os relatorios financeiros sao lidos na importacao.",
                UsuarioInclusao = "seed"
            }
        };
    }
}
