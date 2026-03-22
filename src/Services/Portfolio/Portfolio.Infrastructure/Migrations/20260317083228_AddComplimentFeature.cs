using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComplimentFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedComplimentCount",
                table: "Portfolio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageScore",
                table: "Portfolio",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComplimentCount",
                table: "Portfolio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Compliment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compliment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compliment_Portfolio_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compliment_Portfolio_Company_State",
                table: "Compliment",
                columns: new[] { "PortfolioId", "CompanyId", "State" });

            migrationBuilder.CreateIndex(
                name: "UX_Compliment_Portfolio_Company_Active",
                table: "Compliment",
                columns: new[] { "PortfolioId", "CompanyId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Compliment");

            migrationBuilder.DropColumn(
                name: "ApprovedComplimentCount",
                table: "Portfolio");

            migrationBuilder.DropColumn(
                name: "AverageScore",
                table: "Portfolio");

            migrationBuilder.DropColumn(
                name: "ComplimentCount",
                table: "Portfolio");
        }
    }
}
