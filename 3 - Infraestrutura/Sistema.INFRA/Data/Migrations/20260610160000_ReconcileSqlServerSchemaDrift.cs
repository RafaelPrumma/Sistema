using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sistema.INFRA.Data;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610160000_ReconcileSqlServerSchemaDrift")]
    public partial class ReconcileSqlServerSchemaDrift : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            EnsureCoreSchema(migrationBuilder);
            EnsureMensagemSchema(migrationBuilder);
            EnsureFinanceSchemaSafety(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op: this migration reconciles older local SQL Server schemas with the current model.
        }

        private static void EnsureCoreSchema(MigrationBuilder migrationBuilder)
        {
            EnsureAuditColumns(migrationBuilder, "Perfil");
            EnsureAuditColumns(migrationBuilder, "Usuario");
            EnsureAuditColumns(migrationBuilder, "Funcionalidade");
            EnsureAuditColumns(migrationBuilder, "Configuracao");
            EnsureAuditColumns(migrationBuilder, "Tema");
            EnsureAuditColumns(migrationBuilder, "Mensagens");

            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Usuario]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Usuario]', N'Email') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Usuario]
        ADD [Email] nvarchar(200) NOT NULL CONSTRAINT [DF_Usuario_Email] DEFAULT (N'') WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[Usuario]', N'ResetToken') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Usuario] ADD [ResetToken] nvarchar(100) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Usuario]', N'ResetTokenExpiration') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Usuario] ADD [ResetTokenExpiration] datetime2 NULL;
    END;
END;

IF OBJECT_ID(N'[dbo].[Log]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Log]', N'CorrelationId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Log] ADD [CorrelationId] nvarchar(100) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Log]', N'Modulo') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Log]
        ADD [Modulo] int NOT NULL CONSTRAINT [DF_Log_Modulo] DEFAULT (0) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[Log]', N'TraceId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Log] ADD [TraceId] nvarchar(64) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Log]', N'SpanId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Log] ADD [SpanId] nvarchar(32) NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Log_CorrelationId' AND [object_id] = OBJECT_ID(N'[dbo].[Log]'))
    BEGIN
        CREATE INDEX [IX_Log_CorrelationId] ON [dbo].[Log] ([CorrelationId]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Log_Modulo_DataOperacao' AND [object_id] = OBJECT_ID(N'[dbo].[Log]'))
       AND COL_LENGTH(N'[dbo].[Log]', N'Modulo') IS NOT NULL
       AND COL_LENGTH(N'[dbo].[Log]', N'DataOperacao') IS NOT NULL
    BEGIN
        CREATE INDEX [IX_Log_Modulo_DataOperacao] ON [dbo].[Log] ([Modulo], [DataOperacao]);
    END;
END;

IF OBJECT_ID(N'[dbo].[PerfilFuncionalidade]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'Permissoes') IS NULL
    BEGIN
        ALTER TABLE [dbo].[PerfilFuncionalidade]
        ADD [Permissoes] int NOT NULL CONSTRAINT [DF_PerfilFuncionalidade_Permissoes_Reconcile] DEFAULT (0) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'PodeLer') IS NOT NULL
       AND COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'PodeEscrever') IS NOT NULL
       AND COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'Permissoes') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE [dbo].[PerfilFuncionalidade]
            SET [Permissoes] =
                CASE
                    WHEN [PerfilId] = 1 THEN -1
                    WHEN [PodeEscrever] = CAST(1 AS bit) THEN 7
                    WHEN [PodeLer] = CAST(1 AS bit) THEN 1
                    ELSE [Permissoes]
                END
            WHERE [Permissoes] = 0;
        ');
    END;

    IF OBJECT_ID(N'[dbo].[Perfil]', N'U') IS NOT NULL
       AND COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'Permissoes') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE pf
            SET [Permissoes] = -1
            FROM [dbo].[PerfilFuncionalidade] pf
            INNER JOIN [dbo].[Perfil] p ON p.[Id] = pf.[PerfilId]
            WHERE p.[Nome] = N''Admin''
              AND pf.[Permissoes] <> -1;
        ');
    END;

    EXEC(N'
        DECLARE @ColumnName sysname;
        DECLARE @ConstraintName sysname;
        DECLARE @Sql nvarchar(max);

        DECLARE legacy_columns CURSOR LOCAL FAST_FORWARD FOR
            SELECT [name]
            FROM sys.columns
            WHERE [object_id] = OBJECT_ID(N''[dbo].[PerfilFuncionalidade]'')
              AND [name] IN (N''PodeLer'', N''PodeEscrever'');

        OPEN legacy_columns;
        FETCH NEXT FROM legacy_columns INTO @ColumnName;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @ConstraintName = dc.[name]
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.[default_object_id] = dc.[object_id]
            WHERE dc.[parent_object_id] = OBJECT_ID(N''[dbo].[PerfilFuncionalidade]'')
              AND c.[name] = @ColumnName;

            IF @ConstraintName IS NOT NULL
            BEGIN
                SET @Sql = N''ALTER TABLE [dbo].[PerfilFuncionalidade] DROP CONSTRAINT '' + QUOTENAME(@ConstraintName);
                EXEC sp_executesql @Sql;
            END;

            SET @Sql = N''ALTER TABLE [dbo].[PerfilFuncionalidade] DROP COLUMN '' + QUOTENAME(@ColumnName);
            EXEC sp_executesql @Sql;

            SET @ConstraintName = NULL;
            FETCH NEXT FROM legacy_columns INTO @ColumnName;
        END;

        CLOSE legacy_columns;
        DEALLOCATE legacy_columns;
    ');
END;
""");
        }

        private static void EnsureMensagemSchema(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[Mensagens]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Mensagens]', N'AutorId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens] ADD [AutorId] int NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'AvisoAudiencia') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens] ADD [AvisoAudiencia] int NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'AvisoGrupo') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens] ADD [AvisoGrupo] nvarchar(100) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'AvisoPrioridade') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens] ADD [AvisoPrioridade] int NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'AvisoValidoAte') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens] ADD [AvisoValidoAte] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'Fixada') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens]
        ADD [Fixada] bit NOT NULL CONSTRAINT [DF_Mensagens_Fixada] DEFAULT (0) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'PerfilId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens] ADD [PerfilId] int NULL;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'Status') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens]
        ADD [Status] int NOT NULL CONSTRAINT [DF_Mensagens_Status] DEFAULT (1) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[Mensagens]', N'Tipo') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Mensagens]
        ADD [Tipo] int NOT NULL CONSTRAINT [DF_Mensagens_Tipo] DEFAULT (1) WITH VALUES;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Mensagens_AutorId' AND [object_id] = OBJECT_ID(N'[dbo].[Mensagens]'))
       AND COL_LENGTH(N'[dbo].[Mensagens]', N'AutorId') IS NOT NULL
    BEGIN
        CREATE INDEX [IX_Mensagens_AutorId] ON [dbo].[Mensagens] ([AutorId]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Mensagens_PerfilId' AND [object_id] = OBJECT_ID(N'[dbo].[Mensagens]'))
       AND COL_LENGTH(N'[dbo].[Mensagens]', N'PerfilId') IS NOT NULL
    BEGIN
        CREATE INDEX [IX_Mensagens_PerfilId] ON [dbo].[Mensagens] ([PerfilId]);
    END;
END;

IF OBJECT_ID(N'[dbo].[MensagemDestinatarios]', N'U') IS NULL
   AND OBJECT_ID(N'[dbo].[Mensagens]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[Usuario]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[MensagemDestinatarios]
    (
        [MensagemId] int NOT NULL,
        [UsuarioId] int NOT NULL,
        CONSTRAINT [PK_MensagemDestinatarios] PRIMARY KEY ([MensagemId], [UsuarioId]),
        CONSTRAINT [FK_MensagemDestinatarios_Mensagens_MensagemId] FOREIGN KEY ([MensagemId]) REFERENCES [dbo].[Mensagens] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MensagemDestinatarios_Usuario_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([Id])
    );

    CREATE INDEX [IX_MensagemDestinatarios_UsuarioId] ON [dbo].[MensagemDestinatarios] ([UsuarioId]);
END;

IF OBJECT_ID(N'[dbo].[MensagemLeituras]', N'U') IS NULL
   AND OBJECT_ID(N'[dbo].[Mensagens]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[Usuario]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[MensagemLeituras]
    (
        [Id] int NOT NULL IDENTITY,
        [PublicacaoId] int NOT NULL,
        [UsuarioId] int NOT NULL,
        [DataLeitura] datetime2 NOT NULL,
        [DataEntrega] datetime2 NULL,
        CONSTRAINT [PK_MensagemLeituras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MensagemLeituras_Mensagens_PublicacaoId] FOREIGN KEY ([PublicacaoId]) REFERENCES [dbo].[Mensagens] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MensagemLeituras_Usuario_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([Id])
    );

    CREATE UNIQUE INDEX [IX_MensagemLeituras_PublicacaoId_UsuarioId] ON [dbo].[MensagemLeituras] ([PublicacaoId], [UsuarioId]);
    CREATE INDEX [IX_MensagemLeituras_UsuarioId] ON [dbo].[MensagemLeituras] ([UsuarioId]);
END;

IF OBJECT_ID(N'[dbo].[MensagemReacoes]', N'U') IS NULL
   AND OBJECT_ID(N'[dbo].[Mensagens]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[Usuario]', N'U') IS NOT NULL
BEGIN
    CREATE TABLE [dbo].[MensagemReacoes]
    (
        [Id] int NOT NULL IDENTITY,
        [PublicacaoId] int NOT NULL,
        [UsuarioId] int NOT NULL,
        [TipoReacao] int NOT NULL,
        [Data] datetime2 NOT NULL,
        CONSTRAINT [PK_MensagemReacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MensagemReacoes_Mensagens_PublicacaoId] FOREIGN KEY ([PublicacaoId]) REFERENCES [dbo].[Mensagens] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MensagemReacoes_Usuario_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([Id])
    );

    CREATE UNIQUE INDEX [IX_MensagemReacoes_PublicacaoId_UsuarioId] ON [dbo].[MensagemReacoes] ([PublicacaoId], [UsuarioId]);
    CREATE INDEX [IX_MensagemReacoes_UsuarioId] ON [dbo].[MensagemReacoes] ([UsuarioId]);
END;
""");
        }

        private static void EnsureFinanceSchemaSafety(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[FinanceiroCarga]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[FinanceiroCarga]', N'DataExclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroCarga] ADD [DataExclusao] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[FinanceiroCarga]', N'UsuarioExclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[FinanceiroCarga] ADD [UsuarioExclusao] nvarchar(max) NULL;
    END;
END;
""");

            foreach (var tableName in new[]
            {
                "FinanceiroAtivo",
                "FinanceiroDocumento",
                "FinanceiroConteudoBruto",
                "FinanceiroOperacaoB3",
                "FinanceiroTransacaoCripto",
                "FinanceiroPosicaoEstimativa",
                "FinanceiroRendimento",
                "FinanceiroAgregado",
                "FinanceiroAlertaConfiabilidade"
            })
            {
                EnsureAuditColumns(migrationBuilder, tableName);
            }
        }

        private static void EnsureAuditColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($"""
IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataInclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_{tableName}_DataInclusao_Reconcile] DEFAULT (SYSUTCDATETIME()) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataAlteracao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}] ADD [DataAlteracao] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioInclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_{tableName}_UsuarioInclusao_Reconcile] DEFAULT (N'migration') WITH VALUES;
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
