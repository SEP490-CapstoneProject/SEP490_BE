using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockTypeIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[BlockType]')
                      AND name = N'IsActive'
                )
                BEGIN
                    ALTER TABLE [BlockType]
                        ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[BlockType]')
                      AND name = N'IsActive'
                )
                BEGIN
                    ALTER TABLE [BlockType] DROP COLUMN [IsActive];
                END
            ");
        }
    }
}
