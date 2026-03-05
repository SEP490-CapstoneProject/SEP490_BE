using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserIdWithEmployeeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Portfolio",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Portfolio_UserId",
                table: "Portfolio",
                newName: "IX_Portfolio_EmployeeId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Portfolio",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "active",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Portfolio",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Portfolio",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Portfolio");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "Portfolio",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Portfolio_EmployeeId",
                table: "Portfolio",
                newName: "IX_Portfolio_UserId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Portfolio",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "active");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Portfolio",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);
        }
    }
}
