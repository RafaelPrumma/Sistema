using Microsoft.EntityFrameworkCore;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema.INFRA.Data;

public class AppDbContext : DbContext
{
    private readonly IExecutionContext _executionContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, IExecutionContext executionContext) : base(options)
    {
        _executionContext = executionContext;
    }

    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();
    public DbSet<PerfilFuncionalidade> PerfilFuncionalidades => Set<PerfilFuncionalidade>();
    public DbSet<Tema> Temas => Set<Tema>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<MensagemReacao> MensagemReacoes => Set<MensagemReacao>();
    public DbSet<MensagemLeitura> MensagemLeituras => Set<MensagemLeitura>();
    public DbSet<MensagemDestinatario> MensagemDestinatarios => Set<MensagemDestinatario>();
    public DbSet<CargaFinanceira> CargasFinanceiras => Set<CargaFinanceira>();
    public DbSet<ImportacaoFinanceiraArquivo> ImportacoesFinanceirasArquivo => Set<ImportacaoFinanceiraArquivo>();
    public DbSet<DocumentoFinanceiro> DocumentosFinanceiros => Set<DocumentoFinanceiro>();
    public DbSet<ConteudoBrutoFinanceiro> ConteudosBrutosFinanceiros => Set<ConteudoBrutoFinanceiro>();
    public DbSet<AtivoFinanceiro> AtivosFinanceiros => Set<AtivoFinanceiro>();
    public DbSet<CarteiraFinanceira> CarteirasFinanceiras => Set<CarteiraFinanceira>();
    public DbSet<CarteiraAtivoFinanceiro> CarteirasAtivosFinanceiros => Set<CarteiraAtivoFinanceiro>();
    public DbSet<CotacaoAtivoFinanceiro> CotacoesAtivosFinanceiros => Set<CotacaoAtivoFinanceiro>();
    public DbSet<PrecoHistoricoAtivoFinanceiro> PrecosHistoricosAtivosFinanceiros => Set<PrecoHistoricoAtivoFinanceiro>();
    public DbSet<OperacaoB3> OperacoesB3 => Set<OperacaoB3>();
    public DbSet<TransacaoCripto> TransacoesCripto => Set<TransacaoCripto>();
    public DbSet<TransacaoFinanceira> TransacoesFinanceiras => Set<TransacaoFinanceira>();
    public DbSet<NegociacaoMensalB3> NegociacoesMensaisB3 => Set<NegociacaoMensalB3>();
    public DbSet<ProventoAnualB3> ProventosAnuaisB3 => Set<ProventoAnualB3>();
    public DbSet<EstimativaPosicaoCarteira> EstimativasPosicaoCarteira => Set<EstimativaPosicaoCarteira>();
    public DbSet<PosicaoAtivo> PosicoesAtivos => Set<PosicaoAtivo>();
    public DbSet<RendimentoInvestimento> RendimentosInvestimento => Set<RendimentoInvestimento>();
    public DbSet<AgregadoFinanceiro> AgregadosFinanceiros => Set<AgregadoFinanceiro>();
    public DbSet<AlertaConfiabilidade> AlertasConfiabilidade => Set<AlertaConfiabilidade>();
    public DbSet<EventoCorporativo> EventosCorporativos => Set<EventoCorporativo>();
    public DbSet<AlertaPreco> AlertasPreco => Set<AlertaPreco>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext).GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
                var generic = method.MakeGenericMethod(entityType.ClrType);
                generic.Invoke(null, new object[] { modelBuilder });
            }
        }
        base.OnModelCreating(modelBuilder);
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : AuditableEntity
        => builder.Entity<TEntity>().HasQueryFilter(x => x.DataExclusao == null);

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();
        var usuario = _executionContext.Usuario ?? "system";
        var agora = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DataInclusao = agora;
                entry.Entity.UsuarioInclusao = string.IsNullOrWhiteSpace(entry.Entity.UsuarioInclusao) ? usuario : entry.Entity.UsuarioInclusao;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.DataAlteracao = agora;
                entry.Entity.UsuarioAlteracao = string.IsNullOrWhiteSpace(entry.Entity.UsuarioAlteracao) ? usuario : entry.Entity.UsuarioAlteracao;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DataExclusao = agora;
                entry.Entity.UsuarioExclusao = usuario;
                entry.Entity.DataAlteracao = agora;
                entry.Entity.UsuarioAlteracao = usuario;
            }
        }
    }
}
