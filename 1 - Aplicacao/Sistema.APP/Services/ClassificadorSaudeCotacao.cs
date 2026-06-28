using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

// F-S — lógica pura da "saúde" da cotação de um ativo detido (sem DbContext, testável isolada).
// Espelha exatamente como a valoração do dashboard escolhe o preço (CriarAtivosCotadosDaTabela):
// prefere a cotação com PrecoBRL>0 e, na ausência dela, valora pelo CUSTO (fallback). A partir dessa
// escolha derivamos um status amigável + uma severidade (ok/atencao/critico) que dá cor ao badge.
//
// Não recalcula nada de transações nem chama API: recebe os sinais já lidos dos read models
// (FinanceiroCotacaoAtivo: status/provedor/preço/validade) e devolve a classificação.
public static class ClassificadorSaudeCotacao
{
    public enum Severidade
    {
        Ok,       // preço de mercado vivo e válido
        Atencao,  // valorando, mas com ressalva (fechamento B3, vencida, sem token)
        Critico   // sem preço utilizável: caiu no custo, ou a cotação falhou
    }

    public readonly record struct Resultado(string Status, Severidade Nivel);

    // Sinais de entrada — todos vêm de FinanceiroCotacaoAtivo (cache/status da última cotação por
    // ativo/provedor), já tendo escolhido a "melhor" cotação como a valoração faz.
    //  temPrecoUtilizavel: existe alguma cotação com PrecoBRL>0 para o ativo (a que valora a posição).
    //  provedor/status: provedor e StatusCotacao da cotação escolhida (a utilizável, se houver; senão a
    //    última tentada — a mais recente — para explicar POR QUE não há preço).
    //  vencida: a cotação utilizável existe mas já passou do ExpiraEm (preço velho ainda em uso).
    public static Resultado Classificar(bool temPrecoUtilizavel, ProvedorCotacao provedor, StatusCotacao status, bool vencida)
    {
        // Sem nenhuma cotação utilizável → a posição é valorada pelo CUSTO. O motivo (por que não há
        // preço) vem do status da última tentativa: sem token Brapi, falha, par não suportado, etc.
        if (!temPrecoUtilizavel)
        {
            return status switch
            {
                StatusCotacao.SemToken => new Resultado("Sem token", Severidade.Atencao),
                StatusCotacao.Falhou => new Resultado("Falhou", Severidade.Critico),
                StatusCotacao.NaoSuportada => new Resultado("Não suportada", Severidade.Critico),
                _ => new Resultado("Fallback custo", Severidade.Critico)
            };
        }

        // Há preço utilizável. O fechamento da custódia B3 (B3Custodia) é um preço bom, mas não é cotação
        // ao vivo — sinaliza atenção para distinguir de Brapi/Binance em tempo real.
        if (provedor == ProvedorCotacao.B3Custodia)
            return new Resultado("B3 Custódia", Severidade.Atencao);

        // Preço vivo, mas já vencido (passou do TTL): ainda valora, porém está velho.
        if (vencida)
            return new Resultado("Vencida", Severidade.Atencao);

        // Cotação marcada como desatualizada pelo provedor também conta como vencida.
        if (status == StatusCotacao.Desatualizada)
            return new Resultado("Vencida", Severidade.Atencao);

        // Preço vivo e válido.
        return new Resultado("Atual", Severidade.Ok);
    }

    // Rótulo amigável da severidade (usado como classe/tom do badge na view e nos contadores).
    public static string RotuloSeveridade(Severidade nivel) => nivel switch
    {
        Severidade.Ok => "ok",
        Severidade.Atencao => "atencao",
        _ => "critico"
    };

    // Grupo visual a que o ativo pertence (B3 / Cripto / B3 Custódia). Cripto pelo flag do ativo;
    // os ativos B3 cotados pelo fechamento da custódia ganham um grupo próprio (sem token Brapi).
    public static string Grupo(bool ehCripto, bool temPrecoUtilizavel, ProvedorCotacao provedorPreco)
    {
        if (ehCripto)
            return "Cripto";
        if (temPrecoUtilizavel && provedorPreco == ProvedorCotacao.B3Custodia)
            return "B3 Custódia";
        return "B3";
    }
}
