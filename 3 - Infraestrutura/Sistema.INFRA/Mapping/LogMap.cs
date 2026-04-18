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
        builder.Property(l => l.Entidade).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Operacao).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Mensagem).IsRequired().HasMaxLength(2000);
        builder.Property(l => l.Usuario).IsRequired().HasMaxLength(100);
        builder.Property(l => l.CorrelationId).HasMaxLength(100);
        builder.Property(l => l.TraceId).HasMaxLength(64);
        builder.Property(l => l.SpanId).HasMaxLength(32);
        builder.Property(l => l.Modulo).IsRequired();
        builder.HasIndex(l => new { l.Modulo, l.DataOperacao });
        builder.HasIndex(l => l.CorrelationId);
    }
}
