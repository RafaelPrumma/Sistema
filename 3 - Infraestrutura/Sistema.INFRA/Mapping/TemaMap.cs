using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class TemaMap : IEntityTypeConfiguration<Tema>
{
    public void Configure(EntityTypeBuilder<Tema> builder)
    {
        builder.ToTable("Tema");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CorHeader).IsRequired().HasMaxLength(7);
        builder.Property(t => t.CorBarraEsquerda).IsRequired().HasMaxLength(7);
        builder.Property(t => t.CorBarraDireita).IsRequired().HasMaxLength(7);
        builder.Property(t => t.CorFooter).IsRequired().HasMaxLength(7);
        builder.Property(t => t.ModoEscuro).HasDefaultValue(false);
        builder.Property(t => t.HeaderFixo).HasDefaultValue(false);
        builder.Property(t => t.FooterFixo).HasDefaultValue(false);
        builder.Property(t => t.MenuLateralExpandido).HasDefaultValue(true);

        builder.HasOne(t => t.Usuario)
               .WithOne()
               .HasForeignKey<Tema>(t => t.UsuarioId);
    }
}
