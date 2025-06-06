namespace Sistema.CORE.Interfaces;

public interface IUnitOfWork
{
    IPerfilRepository Perfis { get; }
    IUsuarioRepository Usuarios { get; }
    ILogRepository Logs { get; }
    Task<int> CommitAsync();
}
