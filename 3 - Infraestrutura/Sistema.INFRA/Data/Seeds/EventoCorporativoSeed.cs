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

        // Confirmados via salto de quantidade na Posição B3 (sem compra) + fato relevante:
        // CPTS11 — desdobramento 1:10 (base 25/09/2023, ex 26/09/2023; Capitânia). Qtd 53→530.
        ("CPTS11", TipoEventoCorporativo.Desdobramento, new DateTime(2023, 9, 26), 10m, "Seed/B3 (Capitânia)", EventoCorporativo.GerarChaveNatural("CPTS11", new DateTime(2023, 9, 26), 10m)),

        // KNSC11 — desdobramento 1:10 (ex 06/11/2023; Kinea). Qtd 19→190.
        ("KNSC11", TipoEventoCorporativo.Desdobramento, new DateTime(2023, 11, 6), 10m, "Seed/B3 (Kinea)", EventoCorporativo.GerarChaveNatural("KNSC11", new DateTime(2023, 11, 6), 10m)),

        // BBAS3 — desdobramento 1:2 (base 15/04/2024, ex 16/04/2024; AGE 02/02/2024). Qtd 70→140.
        ("BBAS3", TipoEventoCorporativo.Desdobramento, new DateTime(2024, 4, 16), 2m, "Seed/B3 (BB)", EventoCorporativo.GerarChaveNatural("BBAS3", new DateTime(2024, 4, 16), 2m)),
    ];
}
