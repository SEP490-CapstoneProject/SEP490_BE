using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPostReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "COMPANY_POST_REPORT",
                schema: "companysvc",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyPostId = table.Column<int>(type: "int", nullable: false),
                    reporterUserId = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    reviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    reviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANY_POST_REPORT", x => x.id);
                    table.ForeignKey(
                        name: "FK_PostReport_Post",
                        column: x => x.companyPostId,
                        principalSchema: "companysvc",
                        principalTable: "COMPANY_POST",
                        principalColumn: "postId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_Post_Reporter",
                schema: "companysvc",
                table: "COMPANY_POST_REPORT",
                columns: new[] { "companyPostId", "reporterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_PostId",
                schema: "companysvc",
                table: "COMPANY_POST_REPORT",
                column: "companyPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "COMPANY_POST_REPORT",
                schema: "companysvc");
        }
    }
}
