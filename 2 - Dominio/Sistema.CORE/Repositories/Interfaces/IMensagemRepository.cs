using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces
{
    public interface IMensagemRepository
    {
        Task<Mensagem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        IQueryable<Mensagem> Query();
        Task AddAsync(Mensagem mensagem, CancellationToken cancellationToken = default);
        void Update(Mensagem mensagem);
    }
}
