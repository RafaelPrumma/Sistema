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
        builder.Property(u => u.Nome).IsRequired();
        builder.Property(u => u.Cpf).IsRequired();
        builder.Property(u => u.SenhaHash).IsRequired();
        builder.Property(u => u.Ativo).HasDefaultValue(true);

        builder.HasOne(u => u.Perfil)
               .WithMany()
               .HasForeignKey(u => u.PerfilId);
    }
}
