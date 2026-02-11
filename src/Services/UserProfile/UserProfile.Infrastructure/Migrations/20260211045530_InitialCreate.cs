using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserProfile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    companyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    companyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    activityField = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    coverImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    taxIdentification = table.Column<int>(type: "int", nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.companyId);
                });

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    coverImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_userId",
                table: "company",
                column: "userId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_userId",
                table: "employee",
                column: "userId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company");

            migrationBuilder.DropTable(
                name: "employee");
        }
    }
}
