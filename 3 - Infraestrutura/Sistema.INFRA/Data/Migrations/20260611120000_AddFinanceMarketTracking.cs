using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sistema.INFRA.Data;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260611120000_AddFinanceMarketTracking")]
    public partial class AddFinanceMarketTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateImportacaoArquivo(migrationBuilder);
            ExtendDocumentoFinanceiro(migrationBuilder);
            CreateCarteiras(migrationBuilder);
            CreateMarketData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op: these tables store imported financial files, wallet grouping, and market history.
        }

        private static void CreateImportacaoArquivo(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[FinanceiroImportacaoArquivo]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FinanceiroImportacaoArquivo]
    (
        [Id] int NOT NULL IDENTITY,
        [SourceFolder] nvarchar(700) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [FinishedAt] datetime2 NULL,
        [Status] int NOT NULL,
        [FilesDiscovered] int NOT NULL,
        [FilesImported] int NOT NULL,
        [FilesSkipped] int NOT NULL,
        [StructuredRowsImported] int NOT NULL,
        [Message] nvarchar(2000) NULL,
        [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_FinanceiroImportacaoArquivo_DataInclusao] DEFAULT (SYSUTCDATETIME()),
        [DataAlteracao] datetime2 NULL,
        [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_FinanceiroImportacaoArquivo_UsuarioInclusao] DEFAULT (N'migration'),
        [UsuarioAlteracao] nvarchar(max) NULL,
        [DataExclusao] datetime2 NULL,
        [UsuarioExclusao] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceiroImportacaoArquivo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroImportacaoArquivo_SourceFolder_StartedAt' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroImportacaoArquivo]'))
BEGIN
    CREATE INDEX [IX_FinanceiroImportacaoArquivo_SourceFolder_StartedAt] ON [dbo].[FinanceiroImportacaoArquivo] ([SourceFolder], [StartedAt]);
END;
""");

            EnsureAuditColumns(migrationBuilder, "FinanceiroImportacaoArquivo");
        }

        private static void ExtendDocumentoFinanceiro(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[FinanceiroDocumento]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'ImportacaoFinanceiraArquivoId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroDocumento] ADD [ImportacaoFinanceiraArquivoId] int NULL;
    END;

    IF COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'StoredPath') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroDocumento] ADD [StoredPath] nvarchar(700) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'DocumentKind') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroDocumento]
        ADD [DocumentKind] int NOT NULL CONSTRAINT [DF_FinanceiroDocumento_DocumentKind] DEFAULT (0) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'ParseStatus') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroDocumento]
        ADD [ParseStatus] int NOT NULL CONSTRAINT [DF_FinanceiroDocumento_ParseStatus] DEFAULT (0) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'ParserVersion') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroDocumento]
        ADD [ParserVersion] nvarchar(40) NOT NULL CONSTRAINT [DF_FinanceiroDocumento_ParserVersion] DEFAULT (N'') WITH VALUES;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroDocumento_DocumentKind' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroDocumento]'))
    BEGIN
        CREATE INDEX [IX_FinanceiroDocumento_DocumentKind] ON [dbo].[FinanceiroDocumento] ([DocumentKind]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroDocumento_Sha256' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroDocumento]'))
       AND COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'Sha256') IS NOT NULL
    BEGIN
        CREATE INDEX [IX_FinanceiroDocumento_Sha256] ON [dbo].[FinanceiroDocumento] ([Sha256]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroDocumento_ImportacaoFinanceiraArquivoId' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroDocumento]'))
       AND COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'ImportacaoFinanceiraArquivoId') IS NOT NULL
    BEGIN
        CREATE INDEX [IX_FinanceiroDocumento_ImportacaoFinanceiraArquivoId] ON [dbo].[FinanceiroDocumento] ([ImportacaoFinanceiraArquivoId]);
    END;

    IF OBJECT_ID(N'[dbo].[FinanceiroImportacaoArquivo]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_FinanceiroDocumento_FinanceiroImportacaoArquivo_ImportacaoFinanceiraArquivoId')
       AND COL_LENGTH(N'[dbo].[FinanceiroDocumento]', N'ImportacaoFinanceiraArquivoId') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroDocumento]
        ADD CONSTRAINT [FK_FinanceiroDocumento_FinanceiroImportacaoArquivo_ImportacaoFinanceiraArquivoId]
        FOREIGN KEY ([ImportacaoFinanceiraArquivoId]) REFERENCES [dbo].[FinanceiroImportacaoArquivo] ([Id]) ON DELETE SET NULL;
    END;
END;
""");
        }

        private static void CreateCarteiras(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[FinanceiroCarteira]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FinanceiroCarteira]
    (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(120) NOT NULL,
        [Slug] nvarchar(140) NOT NULL,
        [Descricao] nvarchar(500) NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [IsSistema] bit NOT NULL,
        [Ativo] bit NOT NULL,
        [Ordem] int NOT NULL,
        [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_FinanceiroCarteira_DataInclusao] DEFAULT (SYSUTCDATETIME()),
        [DataAlteracao] datetime2 NULL,
        [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_FinanceiroCarteira_UsuarioInclusao] DEFAULT (N'migration'),
        [UsuarioAlteracao] nvarchar(max) NULL,
        [DataExclusao] datetime2 NULL,
        [UsuarioExclusao] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceiroCarteira] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroCarteira_Slug' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroCarteira]'))
BEGIN
    CREATE UNIQUE INDEX [IX_FinanceiroCarteira_Slug] ON [dbo].[FinanceiroCarteira] ([Slug]);
END;

IF OBJECT_ID(N'[dbo].[FinanceiroCarteiraAtivo]', N'U') IS NULL
   AND OBJECT_ID(N'[dbo].[FinanceiroCarteira]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[FinanceiroAtivo]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[FinanceiroCarteiraAtivo]
    (
        [Id] int NOT NULL IDENTITY,
        [CarteiraFinanceiraId] int NOT NULL,
        [AtivoFinanceiroId] int NOT NULL,
        [PesoAlvo] decimal(9,4) NULL,
        [Observacao] nvarchar(500) NULL,
        [Ativo] bit NOT NULL,
        [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_FinanceiroCarteiraAtivo_DataInclusao] DEFAULT (SYSUTCDATETIME()),
        [DataAlteracao] datetime2 NULL,
        [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_FinanceiroCarteiraAtivo_UsuarioInclusao] DEFAULT (N'migration'),
        [UsuarioAlteracao] nvarchar(max) NULL,
        [DataExclusao] datetime2 NULL,
        [UsuarioExclusao] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceiroCarteiraAtivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinanceiroCarteiraAtivo_FinanceiroCarteira_CarteiraFinanceiraId] FOREIGN KEY ([CarteiraFinanceiraId]) REFERENCES [dbo].[FinanceiroCarteira] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FinanceiroCarteiraAtivo_FinanceiroAtivo_AtivoFinanceiroId] FOREIGN KEY ([AtivoFinanceiroId]) REFERENCES [dbo].[FinanceiroAtivo] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[FinanceiroCarteiraAtivo]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroCarteiraAtivo_AtivoFinanceiroId' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroCarteiraAtivo]'))
    BEGIN
        CREATE INDEX [IX_FinanceiroCarteiraAtivo_AtivoFinanceiroId] ON [dbo].[FinanceiroCarteiraAtivo] ([AtivoFinanceiroId]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroCarteiraAtivo_CarteiraFinanceiraId_AtivoFinanceiroId' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroCarteiraAtivo]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_FinanceiroCarteiraAtivo_CarteiraFinanceiraId_AtivoFinanceiroId] ON [dbo].[FinanceiroCarteiraAtivo] ([CarteiraFinanceiraId], [AtivoFinanceiroId]);
    END;
END;
""");

            EnsureAuditColumns(migrationBuilder, "FinanceiroCarteira");
            EnsureAuditColumns(migrationBuilder, "FinanceiroCarteiraAtivo");
        }

        private static void CreateMarketData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[FinanceiroCotacaoAtivo]', N'U') IS NULL
   AND OBJECT_ID(N'[dbo].[FinanceiroAtivo]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[FinanceiroCotacaoAtivo]
    (
        [Id] int NOT NULL IDENTITY,
        [AtivoFinanceiroId] int NOT NULL,
        [Provedor] int NOT NULL,
        [Symbol] nvarchar(40) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [Price] decimal(28,12) NOT NULL,
        [PriceBRL] decimal(28,12) NOT NULL,
        [Change] decimal(28,12) NULL,
        [ChangePercent] decimal(18,8) NULL,
        [MarketTime] datetime2 NULL,
        [RetrievedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [Status] int NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [RawJson] nvarchar(max) NOT NULL,
        [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_FinanceiroCotacaoAtivo_DataInclusao] DEFAULT (SYSUTCDATETIME()),
        [DataAlteracao] datetime2 NULL,
        [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_FinanceiroCotacaoAtivo_UsuarioInclusao] DEFAULT (N'migration'),
        [UsuarioAlteracao] nvarchar(max) NULL,
        [DataExclusao] datetime2 NULL,
        [UsuarioExclusao] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceiroCotacaoAtivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinanceiroCotacaoAtivo_FinanceiroAtivo_AtivoFinanceiroId] FOREIGN KEY ([AtivoFinanceiroId]) REFERENCES [dbo].[FinanceiroAtivo] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[FinanceiroCotacaoAtivo]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroCotacaoAtivo_AtivoFinanceiroId_Provedor' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroCotacaoAtivo]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_FinanceiroCotacaoAtivo_AtivoFinanceiroId_Provedor] ON [dbo].[FinanceiroCotacaoAtivo] ([AtivoFinanceiroId], [Provedor]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroCotacaoAtivo_RetrievedAt' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroCotacaoAtivo]'))
    BEGIN
        CREATE INDEX [IX_FinanceiroCotacaoAtivo_RetrievedAt] ON [dbo].[FinanceiroCotacaoAtivo] ([RetrievedAt]);
    END;
END;

IF OBJECT_ID(N'[dbo].[FinanceiroPrecoHistoricoAtivo]', N'U') IS NULL
   AND OBJECT_ID(N'[dbo].[FinanceiroAtivo]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[FinanceiroPrecoHistoricoAtivo]
    (
        [Id] int NOT NULL IDENTITY,
        [AtivoFinanceiroId] int NOT NULL,
        [Provedor] int NOT NULL,
        [Symbol] nvarchar(40) NOT NULL,
        [Date] datetime2 NOT NULL,
        [Interval] nvarchar(12) NOT NULL,
        [Open] decimal(28,12) NOT NULL,
        [High] decimal(28,12) NOT NULL,
        [Low] decimal(28,12) NOT NULL,
        [Close] decimal(28,12) NOT NULL,
        [CloseBRL] decimal(28,12) NOT NULL,
        [Volume] decimal(28,8) NULL,
        [RawJson] nvarchar(max) NOT NULL,
        [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_FinanceiroPrecoHistoricoAtivo_DataInclusao] DEFAULT (SYSUTCDATETIME()),
        [DataAlteracao] datetime2 NULL,
        [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_FinanceiroPrecoHistoricoAtivo_UsuarioInclusao] DEFAULT (N'migration'),
        [UsuarioAlteracao] nvarchar(max) NULL,
        [DataExclusao] datetime2 NULL,
        [UsuarioExclusao] nvarchar(max) NULL,
        CONSTRAINT [PK_FinanceiroPrecoHistoricoAtivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinanceiroPrecoHistoricoAtivo_FinanceiroAtivo_AtivoFinanceiroId] FOREIGN KEY ([AtivoFinanceiroId]) REFERENCES [dbo].[FinanceiroAtivo] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[FinanceiroPrecoHistoricoAtivo]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FinanceiroPrecoHistoricoAtivo_AtivoFinanceiroId_Provedor_Interval_Date' AND [object_id] = OBJECT_ID(N'[dbo].[FinanceiroPrecoHistoricoAtivo]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_FinanceiroPrecoHistoricoAtivo_AtivoFinanceiroId_Provedor_Interval_Date] ON [dbo].[FinanceiroPrecoHistoricoAtivo] ([AtivoFinanceiroId], [Provedor], [Interval], [Date]);
    END;
END;
""");

            EnsureAuditColumns(migrationBuilder, "FinanceiroCotacaoAtivo");
            EnsureAuditColumns(migrationBuilder, "FinanceiroPrecoHistoricoAtivo");
        }

        private static void EnsureAuditColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($"""
IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataInclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_{tableName}_DataInclusao_Market] DEFAULT (SYSUTCDATETIME()) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataAlteracao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}] ADD [DataAlteracao] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioInclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_{tableName}_UsuarioInclusao_Market] DEFAULT (N'migration') WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioAlteracao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}] ADD [UsuarioAlteracao] nvarchar(max) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataExclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}] ADD [DataExclusao] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioExclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}] ADD [UsuarioExclusao] nvarchar(max) NULL;
    END;
END;
""");
        }
    }
}
