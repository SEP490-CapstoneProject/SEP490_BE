using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioFollowCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "PortfolioFollow",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PortfolioFollowCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(HOUR, 7, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioFollowCategory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioFollow_CategoryId",
                table: "PortfolioFollow",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioFollowCategory_CompanyId",
                table: "PortfolioFollowCategory",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "UX_PortfolioFollowCategory_Company_Code",
                table: "PortfolioFollowCategory",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioFollow_PortfolioFollowCategory_CategoryId",
                table: "PortfolioFollow",
                column: "CategoryId",
                principalTable: "PortfolioFollowCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioFollow_PortfolioFollowCategory_CategoryId",
                table: "PortfolioFollow");

            migrationBuilder.DropTable(
                name: "PortfolioFollowCategory");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioFollow_CategoryId",
                table: "PortfolioFollow");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "PortfolioFollow");
        }
    }
}
