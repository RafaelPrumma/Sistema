using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class MensagemDestinatarioMap : IEntityTypeConfiguration<MensagemDestinatario>
{
    public void Configure(EntityTypeBuilder<MensagemDestinatario> builder)
    {
        builder.ToTable("MensagemDestinatarios");
        builder.HasKey(x => new { x.MensagemId, x.UsuarioId });

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
