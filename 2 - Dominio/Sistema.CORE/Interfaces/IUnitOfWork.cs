namespace Sistema.CORE.Interfaces;

public interface IUnitOfWork
{
    IPerfilRepository Perfis { get; }
    IUsuarioRepository Usuarios { get; }
    IFuncionalidadeRepository Funcionalidades { get; }
    IPerfilFuncionalidadeRepository PerfilFuncionalidades { get; }
    ILogRepository Logs { get; }
    Task<int> CommitAsync();
}
