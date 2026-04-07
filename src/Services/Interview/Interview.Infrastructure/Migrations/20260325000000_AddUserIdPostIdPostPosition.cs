using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Interview.Infrastructure.Migrations
{
    public partial class AddUserIdPostIdPostPosition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Interviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Interviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PostPosition",
                table: "Interviews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UserId", table: "Interviews");
            migrationBuilder.DropColumn(name: "PostId", table: "Interviews");
            migrationBuilder.DropColumn(name: "PostPosition", table: "Interviews");
        }
    }
}
