using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardPointsAndSponsoredPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CacheKey",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageId",
                table: "PortfolioPreview",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagegenModel",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecruiterSummary",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedTheme",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialCaption",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisualPrompt",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RewardPointTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardPointTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SponsoredPost",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<int>(type: "int", nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PointsSpent = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClickThroughUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ClickCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsoredPost", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointTransaction_UserId",
                table: "RewardPointTransaction",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointTransaction_UserId_CreatedAt",
                table: "RewardPointTransaction",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointTransaction_UserId_Type",
                table: "RewardPointTransaction",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredPost_CreatedAt",
                table: "SponsoredPost",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredPost_CreatedBy",
                table: "SponsoredPost",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredPost_Status",
                table: "SponsoredPost",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredPost_Status_ExpiryDate",
                table: "SponsoredPost",
                columns: new[] { "Status", "ExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RewardPointTransaction");

            migrationBuilder.DropTable(
                name: "SponsoredPost");

            migrationBuilder.DropColumn(
                name: "CacheKey",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "ImagegenModel",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "RecruiterSummary",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "SelectedTheme",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "SocialCaption",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "VisualPrompt",
                table: "PortfolioPreview");
        }
    }
}
