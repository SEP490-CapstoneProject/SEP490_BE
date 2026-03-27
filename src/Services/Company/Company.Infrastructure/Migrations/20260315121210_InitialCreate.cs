using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "COMPANY",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    avatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "COMPANY_POST",
                columns: table => new
                {
                    postId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyId = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    salary = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    employmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    experienceYear = table.Column<int>(type: "int", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: true),
                    jobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requirementsMandatory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requirementsPreferred = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    benefits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    coverImageVideo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANY_POST", x => x.postId);
                    table.ForeignKey(
                        name: "FK_CompanyPost_Company",
                        column: x => x.companyId,
                        principalTable: "COMPANY",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "COMPANY_POST_MEDIA",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyPostId = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANY_POST_MEDIA", x => x.id);
                    table.ForeignKey(
                        name: "FK_PostMedia_Post",
                        column: x => x.companyPostId,
                        principalTable: "COMPANY_POST",
                        principalColumn: "postId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "COMPANY_POST_SAVE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    companyPostId = table.Column<int>(type: "int", nullable: false),
                    userId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANY_POST_SAVE", x => x.id);
                    table.ForeignKey(
                        name: "FK_PostSave_Post",
                        column: x => x.companyPostId,
                        principalTable: "COMPANY_POST",
                        principalColumn: "postId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_CompanyId",
                table: "COMPANY_POST",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_Status_CreateAt_PostId",
                table: "COMPANY_POST",
                columns: new[] { "status", "createAt", "postId" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_PostId",
                table: "COMPANY_POST_MEDIA",
                column: "companyPostId");

            migrationBuilder.CreateIndex(
                name: "IX_COMPANY_POST_SAVE_companyPostId",
                table: "COMPANY_POST_SAVE",
                column: "companyPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSave_User_Post",
                table: "COMPANY_POST_SAVE",
                columns: new[] { "userId", "companyPostId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "COMPANY_POST_MEDIA");

            migrationBuilder.DropTable(
                name: "COMPANY_POST_SAVE");

            migrationBuilder.DropTable(
                name: "COMPANY_POST");

            migrationBuilder.DropTable(
                name: "COMPANY");
        }
    }
}
