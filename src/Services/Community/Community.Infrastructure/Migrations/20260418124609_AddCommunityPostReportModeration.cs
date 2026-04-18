using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Community.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityPostReportModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communityPostReport",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    communityPostId = table.Column<int>(type: "int", nullable: false),
                    reporterUserId = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    reviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    reviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updateAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communityPostReport", x => x.id);
                    table.ForeignKey(
                        name: "FK_PostReport_Post",
                        column: x => x.communityPostId,
                        principalTable: "communityPost",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_PostId",
                table: "communityPostReport",
                column: "communityPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_Status",
                table: "communityPostReport",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "UQ_PostReport_Post_Reporter",
                table: "communityPostReport",
                columns: new[] { "communityPostId", "reporterUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communityPostReport");
        }
    }
}
