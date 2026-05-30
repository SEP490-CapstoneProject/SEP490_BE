using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPriorityScorePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PriorityScore",
                table: "SponsoredPost",
                type: "decimal(10,2)",
                nullable: true,
                defaultValue: 50m,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,2)",
                oldNullable: true,
                oldDefaultValue: 50m);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "PortfolioPreview",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesAvatar",
                table: "PortfolioPreview",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "PortfolioPreview");

            migrationBuilder.DropColumn(
                name: "IncludesAvatar",
                table: "PortfolioPreview");

            migrationBuilder.AlterColumn<decimal>(
                name: "PriorityScore",
                table: "SponsoredPost",
                type: "decimal(3,2)",
                nullable: true,
                defaultValue: 50m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true,
                oldDefaultValue: 50m);
        }
    }
}
