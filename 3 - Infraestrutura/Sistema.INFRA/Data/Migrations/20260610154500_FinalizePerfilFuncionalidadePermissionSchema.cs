using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sistema.INFRA.Data;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610154500_FinalizePerfilFuncionalidadePermissionSchema")]
    public partial class FinalizePerfilFuncionalidadePermissionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

    IF COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'PodeLer') IS NOT NULL
    BEGIN
        DECLARE @PodeLerDefaultConstraint sysname;

        SELECT @PodeLerDefaultConstraint = dc.[name]
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.[default_object_id] = dc.[object_id]
        WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[PerfilFuncionalidade]')
          AND c.[name] = N'PodeLer';

        IF @PodeLerDefaultConstraint IS NOT NULL
        BEGIN
            DECLARE @DropPodeLerDefaultSql nvarchar(max) =
                N'ALTER TABLE [dbo].[PerfilFuncionalidade] DROP CONSTRAINT ' + QUOTENAME(@PodeLerDefaultConstraint);
            EXEC sp_executesql @DropPodeLerDefaultSql;
        END;

        EXEC(N'ALTER TABLE [dbo].[PerfilFuncionalidade] DROP COLUMN [PodeLer]');
    END;

    IF COL_LENGTH(N'[dbo].[PerfilFuncionalidade]', N'PodeEscrever') IS NOT NULL
    BEGIN
        DECLARE @PodeEscreverDefaultConstraint sysname;

        SELECT @PodeEscreverDefaultConstraint = dc.[name]
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.[default_object_id] = dc.[object_id]
        WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[PerfilFuncionalidade]')
          AND c.[name] = N'PodeEscrever';

        IF @PodeEscreverDefaultConstraint IS NOT NULL
        BEGIN
            DECLARE @DropPodeEscreverDefaultSql nvarchar(max) =
                N'ALTER TABLE [dbo].[PerfilFuncionalidade] DROP CONSTRAINT ' + QUOTENAME(@PodeEscreverDefaultConstraint);
            EXEC sp_executesql @DropPodeEscreverDefaultSql;
        END;

        EXEC(N'ALTER TABLE [dbo].[PerfilFuncionalidade] DROP COLUMN [PodeEscrever]');
    END;
END;
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op: legacy PodeLer/PodeEscrever columns are not part of the current model.
        }
    }
}
