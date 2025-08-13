using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class PerfilFuncionalidadeMap : IEntityTypeConfiguration<PerfilFuncionalidade>
{
    public void Configure(EntityTypeBuilder<PerfilFuncionalidade> builder)
    {
        builder.ToTable("PerfilFuncionalidade");
        builder.HasKey(pf => new { pf.PerfilId, pf.FuncionalidadeId });

        builder.HasOne(pf => pf.Perfil)
               .WithMany()
               .HasForeignKey(pf => pf.PerfilId);

        builder.HasOne(pf => pf.Funcionalidade)
               .WithMany(f => f.Perfis)
               .HasForeignKey(pf => pf.FuncionalidadeId);
    }
}
