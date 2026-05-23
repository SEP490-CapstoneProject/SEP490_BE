using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviewImageAndAdvancedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "visualPrompt",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Structured visual prompt for image generation");

            migrationBuilder.AddColumn<string>(
                name: "imageUrl",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Generated preview image URL from Media service");

            migrationBuilder.AddColumn<string>(
                name: "recruiterSummary",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Recruiter-specific summary variant");

            migrationBuilder.AddColumn<string>(
                name: "selectedTheme",
                table: "PortfolioPreview",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "professional",
                comment: "Selected theme: professional, creative, minimal, startup, corporate, cyberpunk");

            migrationBuilder.AddColumn<string>(
                name: "socialCaption",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true,
                comment: "LinkedIn/social media variant of preview");

            migrationBuilder.AddColumn<string>(
                name: "imagegen_model",
                table: "PortfolioPreview",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "Which Imagen model was used for generation");

            migrationBuilder.AddColumn<string>(
                name: "cacheKey",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Hash of portfolio content + prompt for cache invalidation");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPreview_selectedTheme",
                table: "PortfolioPreview",
                column: "selectedTheme");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PortfolioPreview_selectedTheme",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "visualPrompt",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "imageUrl",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "recruiterSummary",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "selectedTheme",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "socialCaption",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "imagegen_model",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "cacheKey",
                table: "PortfolioPreview");
        }
    }
}
