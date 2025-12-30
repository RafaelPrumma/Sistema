using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA;

public class UnitOfWork(AppDbContext context, IPerfilRepository perfis, IUsuarioRepository usuarios, ILogRepository logs, IFuncionalidadeRepository funcionalidades,
				  IPerfilFuncionalidadeRepository perfilFuncs, ITemaRepository temas, IConfiguracaoRepository configuracoes, IMensagemRepository mensagens) : IUnitOfWork
{
    private readonly AppDbContext _context = context;
	public IPerfilRepository Perfis { get; } = perfis;
	public IUsuarioRepository Usuarios { get; } = usuarios;
	public ILogRepository Logs { get; } = logs;
	public IFuncionalidadeRepository Funcionalidades { get; } = funcionalidades;
	public IPerfilFuncionalidadeRepository PerfilFuncionalidades { get; } = perfilFuncs;
	public ITemaRepository Temas { get; } = temas;
	public IConfiguracaoRepository Configuracoes { get; } = configuracoes;
	public IMensagemRepository Mensagens { get; } = mensagens;

	public Task<int> ConfirmarAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
}
