using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorComplimentToUserScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Compliment]
SET [State] = 3,
    [UpdatedAt] = GETUTCDATE()
WHERE [State] <> 3;");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Compliment_Portfolio_Company_Active' AND object_id = OBJECT_ID('Compliment'))
    DROP INDEX [UX_Compliment_Portfolio_Company_Active] ON [Compliment];");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Compliment_Portfolio_Company_State' AND object_id = OBJECT_ID('Compliment'))
    DROP INDEX [IX_Compliment_Portfolio_Company_State] ON [Compliment];");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Compliment', 'UserId') IS NULL AND COL_LENGTH('Compliment', 'CompanyId') IS NOT NULL
    EXEC sp_rename 'Compliment.CompanyId', 'UserId', 'COLUMN';");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Compliment', 'IsDeleted') IS NOT NULL
BEGIN
    DECLARE @dfName sysname;
    SELECT @dfName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t
        ON t.object_id = c.object_id
    WHERE t.name = 'Compliment' AND c.name = 'IsDeleted';

    IF @dfName IS NOT NULL
        EXEC('ALTER TABLE [Compliment] DROP CONSTRAINT [' + @dfName + ']');

    ALTER TABLE [Compliment] DROP COLUMN [IsDeleted];
END");

            migrationBuilder.Sql(@"
ALTER TABLE [Compliment] ALTER COLUMN [Content] nvarchar(max) NULL;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Compliment_Portfolio_User_State' AND object_id = OBJECT_ID('Compliment'))
    CREATE INDEX [IX_Compliment_Portfolio_User_State] ON [Compliment]([PortfolioId], [UserId], [State]);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Compliment_Portfolio_User_Active' AND object_id = OBJECT_ID('Compliment'))
    CREATE UNIQUE INDEX [UX_Compliment_Portfolio_User_Active]
    ON [Compliment]([PortfolioId], [UserId])
    WHERE [State] <> 3;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Compliment_Portfolio_User_Active' AND object_id = OBJECT_ID('Compliment'))
    DROP INDEX [UX_Compliment_Portfolio_User_Active] ON [Compliment];");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Compliment_Portfolio_User_State' AND object_id = OBJECT_ID('Compliment'))
    DROP INDEX [IX_Compliment_Portfolio_User_State] ON [Compliment];");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Compliment', 'CompanyId') IS NULL AND COL_LENGTH('Compliment', 'UserId') IS NOT NULL
    EXEC sp_rename 'Compliment.UserId', 'CompanyId', 'COLUMN';");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Compliment', 'IsDeleted') IS NULL
    ALTER TABLE [Compliment] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);");

            migrationBuilder.Sql(@"
UPDATE [Compliment] SET [IsDeleted] = CASE WHEN [State] = 3 THEN 1 ELSE 0 END;");

            migrationBuilder.Sql(@"
ALTER TABLE [Compliment] ALTER COLUMN [Content] nvarchar(max) NOT NULL;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Compliment_Portfolio_Company_State' AND object_id = OBJECT_ID('Compliment'))
    CREATE INDEX [IX_Compliment_Portfolio_Company_State] ON [Compliment]([PortfolioId], [CompanyId], [State]);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Compliment_Portfolio_Company_Active' AND object_id = OBJECT_ID('Compliment'))
    CREATE UNIQUE INDEX [UX_Compliment_Portfolio_Company_Active]
    ON [Compliment]([PortfolioId], [CompanyId])
    WHERE [IsDeleted] = 0;");
        }
    }
}
