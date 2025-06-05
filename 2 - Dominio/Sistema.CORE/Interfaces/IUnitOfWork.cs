namespace Sistema.CORE.Interfaces;

public interface IUnitOfWork
{
    IPerfilRepository Perfis { get; }
    IUsuarioRepository Usuarios { get; }
    ILogRepository Logs { get; }
    IFuncionalidadeRepository Funcionalidades { get; }
    IPerfilFuncionalidadeRepository PerfilFuncionalidades { get; }
    Task<int> CommitAsync();
}
