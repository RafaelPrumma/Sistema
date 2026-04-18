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

            builder.HasOne(m => m.Autor)
                .WithMany()
                .HasForeignKey(m => m.AutorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Remetente)
                .WithMany()
                .HasForeignKey(m => m.RemetenteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Destinatario)
                .WithMany()
                .HasForeignKey(m => m.DestinatarioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Perfil)
                .WithMany()
                .HasForeignKey(m => m.PerfilId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.MensagemPai)
                .WithMany(m => m.Respostas)
                .HasForeignKey(m => m.MensagemPaiId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(m => m.Assunto).HasMaxLength(200);
            builder.Property(m => m.Corpo).IsRequired().HasMaxLength(5000);
            builder.Property(m => m.AvisoGrupo).HasMaxLength(100);

            builder.HasMany(m => m.DestinatariosExplicitos)
                .WithOne(d => d.Mensagem)
                .HasForeignKey(d => d.MensagemId);

            builder.HasMany(m => m.Reacoes)
                .WithOne(r => r.Publicacao)
                .HasForeignKey(r => r.PublicacaoId);

            builder.HasMany(m => m.Leituras)
                .WithOne(l => l.Publicacao)
                .HasForeignKey(l => l.PublicacaoId);
        }
    }
}
