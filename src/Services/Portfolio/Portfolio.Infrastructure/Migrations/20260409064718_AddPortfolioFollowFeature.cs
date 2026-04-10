using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioFollowFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioFollow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PortfolioId = table.Column<int>(type: "int", nullable: false),
                    InterestLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioFollow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioFollow_Portfolio_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioFollow_CompanyId",
                table: "PortfolioFollow",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioFollow_PortfolioId",
                table: "PortfolioFollow",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "UX_PortfolioFollow_Company_Portfolio",
                table: "PortfolioFollow",
                columns: new[] { "CompanyId", "PortfolioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioFollow");
        }
    }
}
