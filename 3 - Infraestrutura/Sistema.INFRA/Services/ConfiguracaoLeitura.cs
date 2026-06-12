using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Sistema.APP.Services.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Services;

// Lê configurações da tabela Configuracao com cache curto (TTL 30s); se a chave estiver vazia ou
// indisponível, cai no appsettings (chave "Agrupamento:Chave"). Tratar valor vazio como "não definido".
public class ConfiguracaoLeitura(AppDbContext context, IConfiguration configuration, IMemoryCache cache) : IConfiguracaoLeitura
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    private readonly AppDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMemoryCache _cache = cache;

    public async Task<string?> ObterTextoAsync(string agrupamento, string chave, string? padrao = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = ChaveCache(agrupamento, chave);
        if (!_cache.TryGetValue(cacheKey, out string? valor))
        {
            valor = null;
            try
            {
                valor = await _context.Configuracoes
                    .AsNoTracking()
                    .Where(x => x.Agrupamento == agrupamento && x.Chave == chave && x.Ativo)
                    .Select(x => x.Valor)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch
            {
                // Banco indisponível: usa o appsettings como fonte.
            }

            if (string.IsNullOrWhiteSpace(valor))
                valor = _configuration[$"{agrupamento}:{chave}"];

            _cache.Set(cacheKey, valor ?? string.Empty, Ttl);
        }

        return string.IsNullOrWhiteSpace(valor) ? padrao : valor;
    }

    public async Task<int> ObterIntAsync(string agrupamento, string chave, int padrao, CancellationToken cancellationToken = default)
    {
        var texto = await ObterTextoAsync(agrupamento, chave, null, cancellationToken);
        return int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) ? valor : padrao;
    }

    public async Task<bool> ObterBoolAsync(string agrupamento, string chave, bool padrao, CancellationToken cancellationToken = default)
    {
        var texto = (await ObterTextoAsync(agrupamento, chave, null, cancellationToken))?.Trim();
        if (string.IsNullOrEmpty(texto))
            return padrao;
        if (texto.Equals("true", StringComparison.OrdinalIgnoreCase) || texto == "1" || texto.Equals("sim", StringComparison.OrdinalIgnoreCase))
            return true;
        if (texto.Equals("false", StringComparison.OrdinalIgnoreCase) || texto == "0" || texto.Equals("nao", StringComparison.OrdinalIgnoreCase) || texto.Equals("não", StringComparison.OrdinalIgnoreCase))
            return false;
        return bool.TryParse(texto, out var valor) ? valor : padrao;
    }

    public void InvalidarCache(string agrupamento, string chave)
        => _cache.Remove(ChaveCache(agrupamento, chave));

    private static string ChaveCache(string agrupamento, string chave) => $"cfg::{agrupamento}::{chave}";
}
