using Sistema.CORE.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema.CORE.Interfaces
{
    public interface IMensagemRepository
    {
        Task<Mensagem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        IQueryable<Mensagem> Query();
        Task AddAsync(Mensagem mensagem, CancellationToken cancellationToken = default);
        void Update(Mensagem mensagem);
    }
}
