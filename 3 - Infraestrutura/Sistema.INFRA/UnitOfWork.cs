using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public IPerfilRepository Perfis { get; }
    public IUsuarioRepository Usuarios { get; }
    public ILogRepository Logs { get; }

    public UnitOfWork(AppDbContext context,
                      IPerfilRepository perfis,
                      IUsuarioRepository usuarios,
                      ILogRepository logs)
    {
        _context = context;
        Perfis = perfis;
        Usuarios = usuarios;
        Logs = logs;
    }

    public Task<int> CommitAsync() => _context.SaveChangesAsync();
}
