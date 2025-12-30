using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema.INFRA.Repositories
{
    /// <summary>
    /// Repositório responsável pelo acesso a dados de mensagens com carregamento das relações necessárias.
    /// </summary>
    public class MensagemRepository : IMensagemRepository
    {
        private readonly AppDbContext _context;

        public MensagemRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Inclui uma nova mensagem no contexto de persistência.
        /// </summary>
        /// <param name="mensagem">Entidade de mensagem a ser adicionada.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task AddAsync(Mensagem mensagem, CancellationToken cancellationToken = default)
        {
            await _context.Mensagens.AddAsync(mensagem, cancellationToken);
        }

        /// <summary>
        /// Recupera uma mensagem por identificador incluindo remetente, destinatário e mensagem pai.
        /// </summary>
        /// <param name="id">Identificador da mensagem.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Mensagem encontrada ou nula.</returns>
        public async Task<Mensagem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Mensagens
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .Include(m => m.MensagemPai)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        /// <summary>
        /// Disponibiliza a consulta base de mensagens com relacionamentos necessários já incluídos.
        /// </summary>
        /// <returns>Consulta configurada para leitura sem rastreamento.</returns>
        public IQueryable<Mensagem> Query()
        {
            return _context.Mensagens
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .AsNoTracking();
        }

        /// <summary>
        /// Atualiza uma mensagem existente no contexto de dados.
        /// </summary>
        /// <param name="mensagem">Entidade com modificações a serem persistidas.</param>
        public void Update(Mensagem mensagem)
        {
            _context.Mensagens.Update(mensagem);
        }
    }
}
