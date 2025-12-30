using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping
{
    public class MensagemMap : IEntityTypeConfiguration<Mensagem>
    {
        public void Configure(EntityTypeBuilder<Mensagem> builder)
        {
            builder.ToTable("Mensagens");

            builder.HasKey(m => m.Id);

            builder.HasOne(m => m.Remetente)
                .WithMany()
                .HasForeignKey(m => m.RemetenteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Destinatario)
                .WithMany()
                .HasForeignKey(m => m.DestinatarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.MensagemPai)
                .WithMany(m => m.Respostas)
                .HasForeignKey(m => m.MensagemPaiId)
                .IsRequired(false);

            builder.Property(m => m.Assunto).HasMaxLength(200);
            builder.Property(m => m.Corpo).IsRequired().HasMaxLength(5000);
        }
    }
}
