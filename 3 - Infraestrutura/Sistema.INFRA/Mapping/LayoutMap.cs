using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class LayoutMap : IEntityTypeConfiguration<Layout>
{
    public void Configure(EntityTypeBuilder<Layout> builder)
    {
        builder.ToTable("Layout");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CorPrimaria).IsRequired();
        builder.Property(l => l.ModoEscuro).HasDefaultValue(false);

        builder.HasOne(l => l.Usuario)
               .WithOne()
               .HasForeignKey<Layout>(l => l.UsuarioId);
    }
}
