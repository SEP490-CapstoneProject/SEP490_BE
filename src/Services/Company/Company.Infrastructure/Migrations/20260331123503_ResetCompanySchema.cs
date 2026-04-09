using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    public partial class ResetCompanySchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'companysvc')
    EXEC('CREATE SCHEMA companysvc');

IF OBJECT_ID('companysvc.COMPANY_POST_MEDIA', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY_POST_MEDIA;
IF OBJECT_ID('companysvc.COMPANY_POST_SAVE', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY_POST_SAVE;
IF OBJECT_ID('companysvc.COMPANY_POST', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY_POST;
IF OBJECT_ID('companysvc.COMPANY', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY;
");

            migrationBuilder.CreateTable(
                name: "COMPANY",
                schema: "companysvc",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    avatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "COMPANY_POST",
                schema: "companysvc",
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
                });

            migrationBuilder.CreateTable(
                name: "COMPANY_POST_MEDIA",
                schema: "companysvc",
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
                        principalSchema: "companysvc",
                        principalTable: "COMPANY_POST",
                        principalColumn: "postId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "COMPANY_POST_SAVE",
                schema: "companysvc",
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
                        principalSchema: "companysvc",
                        principalTable: "COMPANY_POST",
                        principalColumn: "postId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_CompanyId",
                schema: "companysvc",
                table: "COMPANY_POST",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_Status_CreateAt_PostId",
                schema: "companysvc",
                table: "COMPANY_POST",
                columns: new[] { "status", "createAt", "postId" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_PostId",
                schema: "companysvc",
                table: "COMPANY_POST_MEDIA",
                column: "companyPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSave_User_Post",
                schema: "companysvc",
                table: "COMPANY_POST_SAVE",
                columns: new[] { "userId", "companyPostId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('companysvc.COMPANY_POST_MEDIA', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY_POST_MEDIA;
IF OBJECT_ID('companysvc.COMPANY_POST_SAVE', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY_POST_SAVE;
IF OBJECT_ID('companysvc.COMPANY_POST', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY_POST;
IF OBJECT_ID('companysvc.COMPANY', 'U') IS NOT NULL DROP TABLE companysvc.COMPANY;
");
        }
    }
}
