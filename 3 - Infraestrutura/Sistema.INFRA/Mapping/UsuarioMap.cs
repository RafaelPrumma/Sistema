using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class UsuarioMap : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuario");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Cpf).IsRequired().HasMaxLength(11);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(255);
        builder.Property(u => u.Ativo).HasDefaultValue(true);
        builder.Property(u => u.ResetToken).HasMaxLength(100);
        builder.Property(u => u.ResetTokenExpiration);

        builder.HasIndex(u => u.Cpf).IsUnique();

        builder.HasOne(u => u.Perfil)
               .WithMany()
               .HasForeignKey(u => u.PerfilId);
    }
}
