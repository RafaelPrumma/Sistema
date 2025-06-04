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
    public DbSet<Log> Logs => Set<Log>();

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
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.DataAlteracao = DateTime.UtcNow;
            }
        }
    }
}
