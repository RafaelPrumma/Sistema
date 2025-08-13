using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public IPerfilRepository Perfis { get; }
    public IUsuarioRepository Usuarios { get; }
    public ILogRepository Logs { get; }
    public IFuncionalidadeRepository Funcionalidades { get; }
    public IPerfilFuncionalidadeRepository PerfilFuncionalidades { get; }
    public ILayoutRepository Layouts { get; }

    public UnitOfWork(AppDbContext context,
                      IPerfilRepository perfis,
                      IUsuarioRepository usuarios,
                      ILogRepository logs,
                      IFuncionalidadeRepository funcionalidades,
                      IPerfilFuncionalidadeRepository perfilFuncs,
                      ILayoutRepository layouts)
    {
        _context = context;
        Perfis = perfis;
        Usuarios = usuarios;
        Logs = logs;
        Funcionalidades = funcionalidades;
        PerfilFuncionalidades = perfilFuncs;
        Layouts = layouts;
    }

    public Task<int> ConfirmarAsync() => _context.SaveChangesAsync();
}
