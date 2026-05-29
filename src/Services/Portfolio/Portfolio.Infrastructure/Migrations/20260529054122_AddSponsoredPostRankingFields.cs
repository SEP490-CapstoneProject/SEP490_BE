using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsoredPostRankingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentClick",
                table: "SponsoredPost",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentImpression",
                table: "SponsoredPost",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxClick",
                table: "SponsoredPost",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxImpression",
                table: "SponsoredPost",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriorityScore",
                table: "SponsoredPost",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentClick",
                table: "SponsoredPost");

            migrationBuilder.DropColumn(
                name: "CurrentImpression",
                table: "SponsoredPost");

            migrationBuilder.DropColumn(
                name: "MaxClick",
                table: "SponsoredPost");

            migrationBuilder.DropColumn(
                name: "MaxImpression",
                table: "SponsoredPost");

            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "SponsoredPost");
        }
    }
}
