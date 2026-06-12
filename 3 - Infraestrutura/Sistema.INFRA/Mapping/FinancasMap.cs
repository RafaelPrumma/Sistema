using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Mapping;

public class CargaFinanceiraMap : IEntityTypeConfiguration<CargaFinanceira>
{
    public void Configure(EntityTypeBuilder<CargaFinanceira> builder)
    {
        builder.ToTable("FinanceiroCarga");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SchemaVersion).HasMaxLength(40);
        builder.Property(x => x.JsonSha256).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SourcePath).HasMaxLength(500);
        builder.Property(x => x.SummaryJson).IsRequired();
        builder.Property(x => x.DashboardJson);
        builder.HasIndex(x => x.JsonSha256).IsUnique();
    }
}

public class ImportacaoFinanceiraArquivoMap : IEntityTypeConfiguration<ImportacaoFinanceiraArquivo>
{
    public void Configure(EntityTypeBuilder<ImportacaoFinanceiraArquivo> builder)
    {
        builder.ToTable("FinanceiroImportacaoArquivo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceFolder).IsRequired().HasMaxLength(700);
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.HasIndex(x => new { x.SourceFolder, x.StartedAt });
    }
}

public class DocumentoFinanceiroMap : IEntityTypeConfiguration<DocumentoFinanceiro>
{
    public void Configure(EntityTypeBuilder<DocumentoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroDocumento");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Colecao).HasMaxLength(40);
        builder.Property(x => x.Path).HasMaxLength(700);
        builder.Property(x => x.StoredPath).HasMaxLength(700);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.FileType).HasMaxLength(40);
        builder.Property(x => x.Source).HasMaxLength(80);
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.ParserVersion).HasMaxLength(40);
        builder.Property(x => x.RawMetadataJson).IsRequired();
        builder.HasIndex(x => new { x.CargaFinanceiraId, x.FileName });
        builder.HasIndex(x => x.Sha256);
        builder.HasIndex(x => x.DocumentKind);
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ImportacaoFinanceiraArquivo).WithMany().HasForeignKey(x => x.ImportacaoFinanceiraArquivoId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConteudoBrutoFinanceiroMap : IEntityTypeConfiguration<ConteudoBrutoFinanceiro>
{
    public void Configure(EntityTypeBuilder<ConteudoBrutoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroConteudoBruto");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SheetName).HasMaxLength(160);
        builder.Property(x => x.RawText);
        builder.Property(x => x.RawJson);
        builder.HasIndex(x => new { x.DocumentoFinanceiroId, x.ContentType, x.PageNumber });
        builder.HasOne(x => x.DocumentoFinanceiro).WithMany(x => x.ConteudosBrutos).HasForeignKey(x => x.DocumentoFinanceiroId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AtivoFinanceiroMap : IEntityTypeConfiguration<AtivoFinanceiro>
{
    public void Configure(EntityTypeBuilder<AtivoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroAtivo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AssetKey).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Ticker).HasMaxLength(40);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(240);
        builder.Property(x => x.Market).HasMaxLength(60);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.ConceptRole).HasMaxLength(80);
        builder.HasIndex(x => x.AssetKey).IsUnique();
    }
}

public class CarteiraFinanceiraMap : IEntityTypeConfiguration<CarteiraFinanceira>
{
    public void Configure(EntityTypeBuilder<CarteiraFinanceira> builder)
    {
        builder.ToTable("FinanceiroCarteira");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(140);
        builder.Property(x => x.Descricao).HasMaxLength(500);
        builder.Property(x => x.Tipo).IsRequired().HasMaxLength(40);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public class CarteiraAtivoFinanceiroMap : IEntityTypeConfiguration<CarteiraAtivoFinanceiro>
{
    public void Configure(EntityTypeBuilder<CarteiraAtivoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroCarteiraAtivo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PesoAlvo).HasPrecision(9, 4);
        builder.Property(x => x.Observacao).HasMaxLength(500);
        builder.HasIndex(x => new { x.CarteiraFinanceiraId, x.AtivoFinanceiroId }).IsUnique();
        builder.HasOne(x => x.CarteiraFinanceira).WithMany(x => x.Ativos).HasForeignKey(x => x.CarteiraFinanceiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AtivoFinanceiro).WithMany().HasForeignKey(x => x.AtivoFinanceiroId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CotacaoAtivoFinanceiroMap : IEntityTypeConfiguration<CotacaoAtivoFinanceiro>
{
    public void Configure(EntityTypeBuilder<CotacaoAtivoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroCotacaoAtivo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Price).HasPrecision(28, 12);
        builder.Property(x => x.PriceBRL).HasPrecision(28, 12);
        builder.Property(x => x.Change).HasPrecision(28, 12);
        builder.Property(x => x.ChangePercent).HasPrecision(18, 8);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.RawJson).IsRequired();
        builder.HasIndex(x => new { x.AtivoFinanceiroId, x.Provedor }).IsUnique();
        builder.HasIndex(x => x.RetrievedAt);
        builder.HasOne(x => x.AtivoFinanceiro).WithMany().HasForeignKey(x => x.AtivoFinanceiroId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PrecoHistoricoAtivoFinanceiroMap : IEntityTypeConfiguration<PrecoHistoricoAtivoFinanceiro>
{
    public void Configure(EntityTypeBuilder<PrecoHistoricoAtivoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroPrecoHistoricoAtivo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Interval).IsRequired().HasMaxLength(12);
        builder.Property(x => x.Open).HasPrecision(28, 12);
        builder.Property(x => x.High).HasPrecision(28, 12);
        builder.Property(x => x.Low).HasPrecision(28, 12);
        builder.Property(x => x.Close).HasPrecision(28, 12);
        builder.Property(x => x.CloseBRL).HasPrecision(28, 12);
        builder.Property(x => x.Volume).HasPrecision(28, 8);
        builder.Property(x => x.RawJson).IsRequired();
        builder.HasIndex(x => new { x.AtivoFinanceiroId, x.Provedor, x.Interval, x.Date }).IsUnique();
        builder.HasOne(x => x.AtivoFinanceiro).WithMany().HasForeignKey(x => x.AtivoFinanceiroId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OperacaoB3Map : IEntityTypeConfiguration<OperacaoB3>
{
    public void Configure(EntityTypeBuilder<OperacaoB3> builder)
    {
        builder.ToTable("FinanceiroOperacaoB3");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NoteNumber).HasMaxLength(60);
        builder.Property(x => x.Broker).HasMaxLength(120);
        builder.Property(x => x.Market).HasMaxLength(80);
        builder.Property(x => x.OriginalAssetName).HasMaxLength(240);
        builder.Property(x => x.DebitCredit).HasMaxLength(4);
        builder.Property(x => x.DuplicateGroupKey).HasMaxLength(120);
        builder.Property(x => x.SourceFile).HasMaxLength(300);
        builder.Property(x => x.RawJson).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(24, 8);
        builder.Property(x => x.UnitPrice).HasPrecision(24, 8);
        builder.Property(x => x.GrossAmount).HasPrecision(24, 8);
        builder.Property(x => x.Fees).HasPrecision(24, 8);
        builder.Property(x => x.NetAmount).HasPrecision(24, 8);
        builder.HasIndex(x => new { x.CargaFinanceiraId, x.TradeDate });
        builder.HasIndex(x => new { x.AssetId, x.IsCanonical });
        builder.HasIndex(x => x.DuplicateGroupKey);
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class TransacaoCriptoMap : IEntityTypeConfiguration<TransacaoCripto>
{
    public void Configure(EntityTypeBuilder<TransacaoCripto> builder)
    {
        builder.ToTable("FinanceiroTransacaoCripto");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Exchange).HasMaxLength(80);
        builder.Property(x => x.AssetSymbol).HasMaxLength(40);
        builder.Property(x => x.Pair).HasMaxLength(40);
        builder.Property(x => x.FeeAsset).HasMaxLength(40);
        builder.Property(x => x.RawType).HasMaxLength(120);
        builder.Property(x => x.SourceFile).HasMaxLength(300);
        builder.Property(x => x.RawJson).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(28, 12);
        builder.Property(x => x.Price).HasPrecision(28, 12);
        builder.Property(x => x.Total).HasPrecision(28, 12);
        builder.Property(x => x.FeeAmount).HasPrecision(28, 12);
        builder.HasIndex(x => new { x.CargaFinanceiraId, x.TransactionDate });
        builder.HasIndex(x => x.AssetSymbol);
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class TransacaoFinanceiraMap : IEntityTypeConfiguration<TransacaoFinanceira>
{
    public void Configure(EntityTypeBuilder<TransacaoFinanceira> builder)
    {
        builder.ToTable("FinanceiroTransacao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Broker).HasMaxLength(120);
        builder.Property(x => x.Fonte).HasMaxLength(80);
        builder.Property(x => x.Observacao).HasMaxLength(500);
        builder.HasIndex(x => x.Fonte);
        builder.Property(x => x.StagingTipo).HasMaxLength(40);
        builder.Property(x => x.DuplicateGroupKey).HasMaxLength(160);
        builder.Property(x => x.RawJson).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(28, 12);
        builder.Property(x => x.UnitPrice).HasPrecision(28, 12);
        builder.Property(x => x.GrossAmount).HasPrecision(28, 12);
        builder.Property(x => x.Fees).HasPrecision(28, 12);
        builder.HasIndex(x => new { x.AssetId, x.Date });
        builder.HasIndex(x => x.Origem);
        builder.HasIndex(x => x.DuplicateGroupKey);
        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<CargaFinanceira>().WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class EstimativaPosicaoCarteiraMap : IEntityTypeConfiguration<EstimativaPosicaoCarteira>
{
    public void Configure(EntityTypeBuilder<EstimativaPosicaoCarteira> builder)
    {
        builder.ToTable("FinanceiroPosicaoEstimativa");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(24, 8);
        builder.Property(x => x.AveragePrice).HasPrecision(24, 8);
        builder.Property(x => x.TotalInvested).HasPrecision(24, 8);
        builder.Property(x => x.TotalSold).HasPrecision(24, 8);
        builder.Property(x => x.RealizedResult).HasPrecision(24, 8);
        builder.Property(x => x.EstimatedCurrentPosition).HasPrecision(24, 8);
        builder.Property(x => x.RawJson).IsRequired();
        builder.HasIndex(x => new { x.CargaFinanceiraId, x.Status });
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RendimentoInvestimentoMap : IEntityTypeConfiguration<RendimentoInvestimento>
{
    public void Configure(EntityTypeBuilder<RendimentoInvestimento> builder)
    {
        builder.ToTable("FinanceiroRendimento");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IncomeType).HasMaxLength(160);
        builder.Property(x => x.Source).HasMaxLength(160);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.Taxation).HasMaxLength(80);
        builder.Property(x => x.Amount).HasPrecision(24, 8);
        builder.Property(x => x.TaxWithheld).HasPrecision(24, 8);
        builder.Property(x => x.RawJson).IsRequired();
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AgregadoFinanceiroMap : IEntityTypeConfiguration<AgregadoFinanceiro>
{
    public void Configure(EntityTypeBuilder<AgregadoFinanceiro> builder)
    {
        builder.ToTable("FinanceiroAgregado");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Dimensao).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Chave).IsRequired().HasMaxLength(180);
        builder.Property(x => x.Mes).HasMaxLength(7);
        builder.Property(x => x.ValorCompra).HasPrecision(24, 8);
        builder.Property(x => x.ValorVenda).HasPrecision(24, 8);
        builder.Property(x => x.Saldo).HasPrecision(24, 8);
        builder.Property(x => x.Quantidade).HasPrecision(28, 12);
        builder.Property(x => x.RawJson).IsRequired();
        builder.HasIndex(x => new { x.CargaFinanceiraId, x.Dimensao });
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AlertaConfiabilidadeMap : IEntityTypeConfiguration<AlertaConfiabilidade>
{
    public void Configure(EntityTypeBuilder<AlertaConfiabilidade> builder)
    {
        builder.ToTable("FinanceiroAlertaConfiabilidade");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(120);
        builder.Property(x => x.Code).HasMaxLength(120);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Details);
        builder.HasIndex(x => new { x.CargaFinanceiraId, x.Severity });
        builder.HasOne(x => x.CargaFinanceira).WithMany().HasForeignKey(x => x.CargaFinanceiraId).OnDelete(DeleteBehavior.Cascade);
    }
}
