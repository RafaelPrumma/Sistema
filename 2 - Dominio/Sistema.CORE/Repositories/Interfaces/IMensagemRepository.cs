using Sistema.CORE.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace Sistema.CORE.Interfaces
{
    public interface IMensagemRepository
    {
        Task<Mensagem?> GetByIdAsync(int id);
        IQueryable<Mensagem> Query();
        Task AddAsync(Mensagem mensagem);
        void Update(Mensagem mensagem);
    }
}
