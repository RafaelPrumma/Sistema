using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class ConfiguracaoRepository : IConfiguracaoRepository
{
    private readonly AppDbContext _context;

    public ConfiguracaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento) =>
        await _context.Configuracoes
            .Where(c => c.Agrupamento == agrupamento && c.Ativo)
            .ToListAsync();

    public async Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave) =>
        await _context.Configuracoes
            .FirstOrDefaultAsync(c => c.Agrupamento == agrupamento && c.Chave == chave);

    public Task<Configuracao> AdicionarAsync(Configuracao config)
    {
        _context.Configuracoes.Add(config);
        return Task.FromResult(config);
    }

    public Task AtualizarAsync(Configuracao config)
    {
        _context.Configuracoes.Update(config);
        return Task.CompletedTask;
    }

    public async Task RemoverAsync(int id)
    {
        var entity = await _context.Configuracoes.FindAsync(id);
        if (entity != null)
        {
            _context.Configuracoes.Remove(entity);
        }
    }
}
