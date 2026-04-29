using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostModerationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reviewStatus",
                schema: "companysvc",
                table: "COMPANY_POST",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewReason",
                schema: "companysvc",
                table: "COMPANY_POST",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewedAt",
                schema: "companysvc",
                table: "COMPANY_POST",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reviewStatus",
                schema: "companysvc",
                table: "COMPANY_POST");

            migrationBuilder.DropColumn(
                name: "reviewReason",
                schema: "companysvc",
                table: "COMPANY_POST");

            migrationBuilder.DropColumn(
                name: "reviewedAt",
                schema: "companysvc",
                table: "COMPANY_POST");
        }
    }
}
