using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Connection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockIdAndNewStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockId",
                table: "Connection",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockId",
                table: "Connection");
        }
    }
}
