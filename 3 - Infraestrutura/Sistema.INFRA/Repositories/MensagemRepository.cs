using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;
using System.Linq;
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

        public async Task AddAsync(Mensagem mensagem)
        {
            await _context.Mensagens.AddAsync(mensagem);
        }

        public async Task<Mensagem?> GetByIdAsync(int id)
        {
            return await _context.Mensagens
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .Include(m => m.MensagemPai)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public IQueryable<Mensagem> Query()
        {
            return _context.Mensagens.AsQueryable();
        }

        public void Update(Mensagem mensagem)
        {
            _context.Mensagens.Update(mensagem);
        }
    }
}
