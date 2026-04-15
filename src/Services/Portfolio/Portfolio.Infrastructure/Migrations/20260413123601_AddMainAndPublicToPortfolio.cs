using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMainAndPublicToPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Portfolio', 'IsMain') IS NULL
BEGIN
    ALTER TABLE [Portfolio] ADD [IsMain] bit NOT NULL DEFAULT CAST(0 AS bit);
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Portfolio', 'IsPublic') IS NULL
BEGIN
    ALTER TABLE [Portfolio] ADD [IsPublic] bit NOT NULL DEFAULT CAST(0 AS bit);
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Portfolio_EmployeeId_IsMain'
      AND object_id = OBJECT_ID('Portfolio')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Portfolio_EmployeeId_IsMain]
    ON [Portfolio]([EmployeeId], [IsMain])
    WHERE [IsMain] = 1;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Portfolio_EmployeeId_IsMain'
      AND object_id = OBJECT_ID('Portfolio')
)
BEGIN
    DROP INDEX [UX_Portfolio_EmployeeId_IsMain] ON [Portfolio];
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Portfolio', 'IsMain') IS NOT NULL
BEGIN
    ALTER TABLE [Portfolio] DROP COLUMN [IsMain];
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Portfolio', 'IsPublic') IS NOT NULL
BEGIN
    ALTER TABLE [Portfolio] DROP COLUMN [IsPublic];
END");
        }
    }
}
