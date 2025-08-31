using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;
using System.Threading;

namespace Sistema.INFRA;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public IPerfilRepository Perfis { get; }
    public IUsuarioRepository Usuarios { get; }
    public ILogRepository Logs { get; }
    public IFuncionalidadeRepository Funcionalidades { get; }
    public IPerfilFuncionalidadeRepository PerfilFuncionalidades { get; }
    public ITemaRepository Temas { get; }
    public IConfiguracaoRepository Configuracoes { get; }
    public IMensagemRepository Mensagens { get; }

    public UnitOfWork(AppDbContext context,
                      IPerfilRepository perfis,
                      IUsuarioRepository usuarios,
                      ILogRepository logs,
                      IFuncionalidadeRepository funcionalidades,
                      IPerfilFuncionalidadeRepository perfilFuncs,
                      ITemaRepository temas,
                      IConfiguracaoRepository configuracoes,
                      IMensagemRepository mensagens)
    {
        _context = context;
        Perfis = perfis;
        Usuarios = usuarios;
        Logs = logs;
        Funcionalidades = funcionalidades;
        PerfilFuncionalidades = perfilFuncs;
        Temas = temas;
        Configuracoes = configuracoes;
        Mensagens = mensagens;
    }

    public Task<int> ConfirmarAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
}
