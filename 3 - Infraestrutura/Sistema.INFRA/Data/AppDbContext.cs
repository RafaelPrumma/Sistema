using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema.INFRA.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();
    public DbSet<PerfilFuncionalidade> PerfilFuncionalidades => Set<PerfilFuncionalidade>();
    public DbSet<Tema> Temas => Set<Tema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

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
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DataInclusao = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(entry.Entity.UsuarioInclusao))
                    entry.Entity.UsuarioInclusao = "system";
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.DataAlteracao = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(entry.Entity.UsuarioAlteracao))
                    entry.Entity.UsuarioAlteracao = "system";
            }
        }
    }
}
