using Sistema.APP.DTOs;

namespace Sistema.APP.Services.Interfaces;

/// <summary>
/// Serviço do submódulo Gastos (G1). Materializa lançamentos a partir do conteúdo bruto já
/// persistido das faturas/extratos Nubank (LAZY e idempotente) e devolve a visão geral.
/// TUDO à prova de falha: nunca estoura no load (degrada para vazio).
/// </summary>
public interface IGastosService
{
    /// <summary>
    /// Garante que os lançamentos das faturas/extratos já importados estejam materializados
    /// (idempotente pela chave natural) e devolve a visão geral do mês corrente. Disparado ao
    /// abrir o módulo Gastos — NÃO mexe no load do dashboard de Investimentos.
    /// </summary>
    Task<GastosVisaoGeralDto> ObterVisaoGeralAsync(CancellationToken cancellationToken = default);
}
