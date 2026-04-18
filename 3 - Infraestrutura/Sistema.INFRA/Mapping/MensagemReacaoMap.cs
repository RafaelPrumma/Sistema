using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class MensagemReacaoMap : IEntityTypeConfiguration<MensagemReacao>
{
    public void Configure(EntityTypeBuilder<MensagemReacao> builder)
    {
        builder.ToTable("MensagemReacoes");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.PublicacaoId, x.UsuarioId }).IsUnique();

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
