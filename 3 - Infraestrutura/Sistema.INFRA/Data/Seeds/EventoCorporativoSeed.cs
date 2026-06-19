using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

/// <summary>
/// Seed dos eventos corporativos confirmados.
/// Somente entram aqui eventos com fator e data validados em fonte oficial (B3 / informe do fundo).
/// Eventos com ratio PM_banco/PM_real ainda não confirmados ficam pendentes de validação manual.
/// </summary>
public static class EventoCorporativoSeed
{
    /// <summary>
    /// Retorna os eventos confirmados, usando o ticker para localizar o AtivoFinanceiroId em runtime.
    /// O seed é idempotente via ChaveNatural (ticker|data|fator).
    /// </summary>
    public static IEnumerable<(string Ticker, TipoEventoCorporativo Tipo, DateTime Data, decimal Fator, string Fonte, string ChaveNatural)> GetDefinicoes() =>
    [
        // BCFF11 — desdobramento 1:8 em 28/11/2023 (confirmado B3/informe do fundo).
        ("BCFF11", TipoEventoCorporativo.Desdobramento, new DateTime(2023, 11, 28), 8m, "Seed/B3", EventoCorporativo.GerarChaveNatural("BCFF11", new DateTime(2023, 11, 28), 8m)),

        // GGRC11 — desdobramento 1:10 em 06/03/2024 (confirmado B3/informe do fundo).
        ("GGRC11", TipoEventoCorporativo.Desdobramento, new DateTime(2024, 3, 6), 10m, "Seed/B3", EventoCorporativo.GerarChaveNatural("GGRC11", new DateTime(2024, 3, 6), 10m)),
    ];
}
