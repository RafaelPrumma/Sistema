using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class ConfiguracaoMap : IEntityTypeConfiguration<Configuracao>
{
    public void Configure(EntityTypeBuilder<Configuracao> builder)
    {
        builder.ToTable("Configuracao");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Agrupamento).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Chave).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Valor).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Tipo).IsRequired();
        builder.Property(c => c.Descricao).HasMaxLength(500);
        builder.Property(c => c.Ativo).HasDefaultValue(true);
    }
}
