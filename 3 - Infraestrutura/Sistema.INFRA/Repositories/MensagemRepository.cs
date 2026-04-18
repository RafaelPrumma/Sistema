using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories
{
    public class MensagemRepository(AppDbContext context) : IMensagemRepository
    {
        private readonly AppDbContext _context = context;

        public async Task AddAsync(Mensagem mensagem, CancellationToken cancellationToken = default)
        {
            await _context.Mensagens.AddAsync(mensagem, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<Mensagem> mensagens, CancellationToken cancellationToken = default)
        {
            await _context.Mensagens.AddRangeAsync(mensagens, cancellationToken);
        }

        public async Task<Mensagem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Mensagens
                .Include(m => m.Autor)
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .Include(m => m.Perfil)
                .Include(m => m.Reacoes)
                .Include(m => m.MensagemPai)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public IQueryable<Mensagem> Query()
        {
            return _context.Mensagens
                .Include(m => m.Autor)
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .Include(m => m.Perfil)
                .Include(m => m.Reacoes)
                .AsNoTracking();
        }

        public IQueryable<MensagemReacao> QueryReacoes() => _context.MensagemReacoes.AsQueryable();

        public async Task AddReacaoAsync(MensagemReacao reacao, CancellationToken cancellationToken = default)
        {
            await _context.MensagemReacoes.AddAsync(reacao, cancellationToken);
        }

        public void UpdateReacao(MensagemReacao reacao)
        {
            _context.MensagemReacoes.Update(reacao);
        }

        public IQueryable<MensagemLeitura> QueryLeituras() => _context.MensagemLeituras.AsQueryable();

        public async Task AddLeituraAsync(MensagemLeitura leitura, CancellationToken cancellationToken = default)
        {
            await _context.MensagemLeituras.AddAsync(leitura, cancellationToken);
        }

        public void Update(Mensagem mensagem)
        {
            _context.Mensagens.Update(mensagem);
        }
    }
}
