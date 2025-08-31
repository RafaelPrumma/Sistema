using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema.INFRA.Repositories
{
    public class MensagemRepository : IMensagemRepository
    {
        private readonly AppDbContext _context;

        public MensagemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Mensagem mensagem, CancellationToken cancellationToken = default)
        {
            await _context.Mensagens.AddAsync(mensagem, cancellationToken);
        }

        public async Task<Mensagem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Mensagens
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .Include(m => m.MensagemPai)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public IQueryable<Mensagem> Query()
        {
            return _context.Mensagens.AsNoTracking();
        }

        public void Update(Mensagem mensagem)
        {
            _context.Mensagens.Update(mensagem);
        }
    }
}
