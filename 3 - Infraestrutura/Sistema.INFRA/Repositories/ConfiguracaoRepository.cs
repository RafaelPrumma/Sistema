using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using System.Threading;

namespace Sistema.INFRA.Repositories;

public class ConfiguracaoRepository(AppDbContext context) : IConfiguracaoRepository
{
    private readonly AppDbContext _context = context;

	public async Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento, CancellationToken cancellationToken = default) =>
        await _context.Configuracoes
            .AsNoTracking()
            .Where(c => c.Agrupamento == agrupamento && c.Ativo)
            .ToListAsync(cancellationToken);

    public async Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave, CancellationToken cancellationToken = default) =>
        await _context.Configuracoes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Agrupamento == agrupamento && c.Chave == chave, cancellationToken);

    public async Task<Configuracao> AdicionarAsync(Configuracao config, CancellationToken cancellationToken = default)
    {
        await _context.Configuracoes.AddAsync(config, cancellationToken);
        return config;
    }

    public Task AtualizarAsync(Configuracao config)
    {
        _context.Configuracoes.Update(config);
        return Task.CompletedTask;
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Configuracoes.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Configuracoes.Remove(entity);
        }
    }
}
