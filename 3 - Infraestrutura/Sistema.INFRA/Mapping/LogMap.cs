using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class LogMap : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> builder)
    {
        builder.ToTable("Log");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Entidade).IsRequired();
        builder.Property(l => l.Operacao).IsRequired();
        builder.Property(l => l.Mensagem).IsRequired();
        builder.Property(l => l.Usuario).IsRequired();
    }
}
