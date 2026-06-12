namespace Sistema.APP.Services.Interfaces;

// Leitura tipada e cacheada de configurações (tabela Configuracao), com fallback no appsettings.
// Evita bater no banco a cada leitura (ex.: tick de cotações). Reutilizável por qualquer módulo.
public interface IConfiguracaoLeitura
{
    Task<string?> ObterTextoAsync(string agrupamento, string chave, string? padrao = null, CancellationToken cancellationToken = default);
    Task<int> ObterIntAsync(string agrupamento, string chave, int padrao, CancellationToken cancellationToken = default);
    Task<bool> ObterBoolAsync(string agrupamento, string chave, bool padrao, CancellationToken cancellationToken = default);
    void InvalidarCache(string agrupamento, string chave);
}
