using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Community.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostModerationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reviewStatus",
                table: "communityPost",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewReason",
                table: "communityPost",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewedAt",
                table: "communityPost",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reviewStatus",
                table: "communityPost");

            migrationBuilder.DropColumn(
                name: "reviewReason",
                table: "communityPost");

            migrationBuilder.DropColumn(
                name: "reviewedAt",
                table: "communityPost");
        }
    }
}
