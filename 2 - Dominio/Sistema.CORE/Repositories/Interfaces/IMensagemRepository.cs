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
        Task AddRangeAsync(IEnumerable<Mensagem> mensagens, CancellationToken cancellationToken = default);
        void Update(Mensagem mensagem);

        IQueryable<MensagemReacao> QueryReacoes();
        Task AddReacaoAsync(MensagemReacao reacao, CancellationToken cancellationToken = default);
        void UpdateReacao(MensagemReacao reacao);

        IQueryable<MensagemLeitura> QueryLeituras();
        Task AddLeituraAsync(MensagemLeitura leitura, CancellationToken cancellationToken = default);
    }
}
