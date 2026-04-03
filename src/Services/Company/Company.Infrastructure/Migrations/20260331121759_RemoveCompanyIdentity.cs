using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    public partial class RemoveCompanyIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('companysvc.COMPANY', 'U') IS NOT NULL
BEGIN
    IF OBJECT_ID('companysvc.COMPANY_TMP', 'U') IS NOT NULL
        DROP TABLE companysvc.COMPANY_TMP;

    CREATE TABLE companysvc.COMPANY_TMP (
        id int NOT NULL,
        name nvarchar(255) NULL,
        avatarUrl nvarchar(500) NULL,
        CONSTRAINT PK_COMPANY_TMP PRIMARY KEY (id)
    );

    INSERT INTO companysvc.COMPANY_TMP (id, name, avatarUrl)
    SELECT id, name, avatarUrl FROM companysvc.COMPANY;

    DROP TABLE companysvc.COMPANY;
    EXEC sp_rename 'companysvc.COMPANY_TMP', 'COMPANY';
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('companysvc.COMPANY', 'U') IS NOT NULL
BEGIN
    IF OBJECT_ID('companysvc.COMPANY_TMP', 'U') IS NOT NULL
        DROP TABLE companysvc.COMPANY_TMP;

    CREATE TABLE companysvc.COMPANY_TMP (
        id int IDENTITY(1,1) NOT NULL,
        name nvarchar(255) NULL,
        avatarUrl nvarchar(500) NULL,
        CONSTRAINT PK_COMPANY_TMP PRIMARY KEY (id)
    );

    SET IDENTITY_INSERT companysvc.COMPANY_TMP ON;
    INSERT INTO companysvc.COMPANY_TMP (id, name, avatarUrl)
    SELECT id, name, avatarUrl FROM companysvc.COMPANY;
    SET IDENTITY_INSERT companysvc.COMPANY_TMP OFF;

    DROP TABLE companysvc.COMPANY;
    EXEC sp_rename 'companysvc.COMPANY_TMP', 'COMPANY';
END
");
        }
    }
}
