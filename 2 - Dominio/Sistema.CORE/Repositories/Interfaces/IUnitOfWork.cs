using System.Threading;
using System.Threading.Tasks;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IUnitOfWork
{
    IPerfilRepository Perfis { get; }
    IUsuarioRepository Usuarios { get; }
    IFuncionalidadeRepository Funcionalidades { get; }
    IPerfilFuncionalidadeRepository PerfilFuncionalidades { get; }
    ILogRepository Logs { get; }
    ITemaRepository Temas { get; }
    IConfiguracaoRepository Configuracoes { get; }
    IMensagemRepository Mensagens { get; }
    Task<int> ConfirmarAsync(CancellationToken cancellationToken = default);
}
