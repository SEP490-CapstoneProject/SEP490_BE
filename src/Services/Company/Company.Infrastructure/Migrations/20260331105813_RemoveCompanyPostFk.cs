using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyPostFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyPost_Company",
                schema: "companysvc",
                table: "COMPANY_POST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_CompanyPost_Company",
                schema: "companysvc",
                table: "COMPANY_POST",
                column: "companyId",
                principalSchema: "companysvc",
                principalTable: "COMPANY",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
