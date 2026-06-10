using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sistema.INFRA.Data;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610153000_RepairAuditAndPermissionSchema")]
    public partial class RepairAuditAndPermissionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            EnsureAuditColumns(migrationBuilder, "Perfil");
            EnsureAuditColumns(migrationBuilder, "Usuario");
            EnsureAuditColumns(migrationBuilder, "Funcionalidade");
            EnsureAuditColumns(migrationBuilder, "Configuracao");
            EnsureAuditColumns(migrationBuilder, "Tema");
            EnsureAuditColumns(migrationBuilder, "Mensagens");

            RepairPerfilFuncionalidade(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op: this migration repairs existing local schemas and preserves data.
        }

        private static void EnsureAuditColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($"""
IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataInclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [DataInclusao] datetime2 NOT NULL CONSTRAINT [DF_{tableName}_DataInclusao] DEFAULT (SYSUTCDATETIME()) WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataAlteracao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [DataAlteracao] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioInclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [UsuarioInclusao] nvarchar(max) NOT NULL CONSTRAINT [DF_{tableName}_UsuarioInclusao] DEFAULT (N'migration') WITH VALUES;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioAlteracao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [UsuarioAlteracao] nvarchar(max) NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'DataExclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [DataExclusao] datetime2 NULL;
    END;

    IF COL_LENGTH(N'[dbo].[{tableName}]', N'UsuarioExclusao') IS NULL
    BEGIN
        ALTER TABLE [dbo].[{tableName}]
        ADD [UsuarioExclusao] nvarchar(max) NULL;
    END;
END;
""");
        }

        private static void RepairPerfilFuncionalidade(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[PerfilFuncionalidade]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'Permissoes') IS NULL
    BEGIN
        ALTER TABLE [dbo].[PerfilFuncionalidade]
        ADD [Permissoes] int NOT NULL CONSTRAINT [DF_PerfilFuncionalidade_Permissoes] DEFAULT (0) WITH VALUES;
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
END;
""");
        }
    }
}
