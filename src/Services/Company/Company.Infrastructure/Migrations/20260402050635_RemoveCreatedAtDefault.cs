using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreatedAtDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only remove the default constraint from CreatedAt
            migrationBuilder.Sql(@"
                DECLARE @constraint_name NVARCHAR(200);
                SELECT @constraint_name = d.name
                FROM sys.default_constraints d
                INNER JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                WHERE d.parent_object_id = OBJECT_ID(N'[companysvc].[COMPANY_POST]') 
                  AND c.name = N'createAt';
                
                IF @constraint_name IS NOT NULL
                    EXEC('ALTER TABLE [companysvc].[COMPANY_POST] DROP CONSTRAINT [' + @constraint_name + ']');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "createAt",
                schema: "companysvc",
                table: "COMPANY_POST",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
