using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class FuncionalidadeMap : IEntityTypeConfiguration<Funcionalidade>
{
    public void Configure(EntityTypeBuilder<Funcionalidade> builder)
    {
        builder.ToTable("Funcionalidade");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Ativo).HasDefaultValue(true);
    }
}
