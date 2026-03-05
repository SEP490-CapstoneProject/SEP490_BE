using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsMultiple = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioBlock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioId = table.Column<int>(type: "int", nullable: false),
                    BlockTypeId = table.Column<int>(type: "int", nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioBlock_BlockType_BlockTypeId",
                        column: x => x.BlockTypeId,
                        principalTable: "BlockType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioBlock_Portfolio_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Award",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Award", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Award_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Diploma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diploma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diploma_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Time = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Education", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Education_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Experience",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StartDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EndDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experience", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Experience_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Intro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StudyField = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Intro_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtherInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherInfo_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Technology = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Mail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reference_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortfolioBlockId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skill_PortfolioBlock_PortfolioBlockId",
                        column: x => x.PortfolioBlockId,
                        principalTable: "PortfolioBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectLink_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BlockType",
                columns: new[] { "Id", "Code" },
                values: new object[] { 1, "INTRO" });

            migrationBuilder.InsertData(
                table: "BlockType",
                columns: new[] { "Id", "Code", "IsMultiple" },
                values: new object[,]
                {
                    { 2, "SKILL", true },
                    { 3, "EDUCATION", true },
                    { 4, "DIPLOMA", true },
                    { 5, "EXPERIMENT", true },
                    { 6, "PROJECT", true },
                    { 7, "AWARD", true },
                    { 8, "ACTIVITIES", true },
                    { 9, "OTHERINFO", true },
                    { 10, "REFERENCE", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_PortfolioBlockId",
                table: "Activities",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Award_PortfolioBlockId",
                table: "Award",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockType_Code",
                table: "BlockType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diploma_PortfolioBlockId",
                table: "Diploma",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Education_PortfolioBlockId",
                table: "Education",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Experience_PortfolioBlockId",
                table: "Experience",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Intro_PortfolioBlockId",
                table: "Intro",
                column: "PortfolioBlockId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtherInfo_PortfolioBlockId",
                table: "OtherInfo",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolio_UserId",
                table: "Portfolio",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioBlock_BlockTypeId",
                table: "PortfolioBlock",
                column: "BlockTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioBlock_PortfolioId",
                table: "PortfolioBlock",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_PortfolioBlockId",
                table: "Project",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLink_ProjectId",
                table: "ProjectLink",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Reference_PortfolioBlockId",
                table: "Reference",
                column: "PortfolioBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Skill_PortfolioBlockId",
                table: "Skill",
                column: "PortfolioBlockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Award");

            migrationBuilder.DropTable(
                name: "Diploma");

            migrationBuilder.DropTable(
                name: "Education");

            migrationBuilder.DropTable(
                name: "Experience");

            migrationBuilder.DropTable(
                name: "Intro");

            migrationBuilder.DropTable(
                name: "OtherInfo");

            migrationBuilder.DropTable(
                name: "ProjectLink");

            migrationBuilder.DropTable(
                name: "Reference");

            migrationBuilder.DropTable(
                name: "Skill");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "PortfolioBlock");

            migrationBuilder.DropTable(
                name: "BlockType");

            migrationBuilder.DropTable(
                name: "Portfolio");
        }
    }
}
