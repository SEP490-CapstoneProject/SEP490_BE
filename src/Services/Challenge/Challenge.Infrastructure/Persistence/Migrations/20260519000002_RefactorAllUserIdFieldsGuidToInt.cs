using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Challenge.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RefactorAllUserIdFieldsGuidToInt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop ActualUserId column and index if they exist
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_CHALLENGE_SUBMISSIONS_ActualUserId' AND object_id = OBJECT_ID('CHALLENGE_SUBMISSIONS'))
            BEGIN
                DROP INDEX IX_CHALLENGE_SUBMISSIONS_ActualUserId ON CHALLENGE_SUBMISSIONS
            END
        ");

        migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CHALLENGE_SUBMISSIONS' AND COLUMN_NAME='ActualUserId')
            BEGIN
                ALTER TABLE CHALLENGE_SUBMISSIONS DROP COLUMN ActualUserId
            END
        ");

        // Alter CHALLENGE_SUBMISSIONS: UserId from Guid to int
        migrationBuilder.AlterColumn<int>(
            name: "UserId",
            table: "CHALLENGE_SUBMISSIONS",
            type: "int",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        // Alter USER_SKILLS: UserId from Guid to int
        migrationBuilder.AlterColumn<int>(
            name: "UserId",
            table: "USER_SKILLS",
            type: "int",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        // Alter SKILL_POINT_TRANSACTIONS: UserId from Guid to int
        migrationBuilder.AlterColumn<int>(
            name: "UserId",
            table: "SKILL_POINT_TRANSACTIONS",
            type: "int",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        // Alter CHALLENGES: CreatedById from Guid to int
        migrationBuilder.AlterColumn<int>(
            name: "CreatedById",
            table: "CHALLENGES",
            type: "int",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        // Alter CHALLENGES: ReviewedById from Guid? to int?
        migrationBuilder.AlterColumn<int>(
            name: "ReviewedById",
            table: "CHALLENGES",
            type: "int",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        // Alter PENDING_SKILLS: ProposedById from Guid? to int?
        migrationBuilder.AlterColumn<int>(
            name: "ProposedById",
            table: "PENDING_SKILLS",
            type: "int",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        // Alter PENDING_SKILLS: ReviewedById from Guid? to int?
        migrationBuilder.AlterColumn<int>(
            name: "ReviewedById",
            table: "PENDING_SKILLS",
            type: "int",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert CHALLENGE_SUBMISSIONS: UserId from int to Guid
        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "CHALLENGE_SUBMISSIONS",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");

        // Add back ActualUserId column
        migrationBuilder.AddColumn<int>(
            name: "ActualUserId",
            table: "CHALLENGE_SUBMISSIONS",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CHALLENGE_SUBMISSIONS_ActualUserId",
            table: "CHALLENGE_SUBMISSIONS",
            column: "ActualUserId");

        // Revert USER_SKILLS: UserId from int to Guid
        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "USER_SKILLS",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");

        // Revert SKILL_POINT_TRANSACTIONS: UserId from int to Guid
        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "SKILL_POINT_TRANSACTIONS",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");

        // Revert CHALLENGES: CreatedById from int to Guid
        migrationBuilder.AlterColumn<Guid>(
            name: "CreatedById",
            table: "CHALLENGES",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");

        // Revert CHALLENGES: ReviewedById from int? to Guid?
        migrationBuilder.AlterColumn<Guid>(
            name: "ReviewedById",
            table: "CHALLENGES",
            type: "uniqueidentifier",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        // Revert PENDING_SKILLS: ProposedById from int? to Guid?
        migrationBuilder.AlterColumn<Guid>(
            name: "ProposedById",
            table: "PENDING_SKILLS",
            type: "uniqueidentifier",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        // Revert PENDING_SKILLS: ReviewedById from int? to Guid?
        migrationBuilder.AlterColumn<Guid>(
            name: "ReviewedById",
            table: "PENDING_SKILLS",
            type: "uniqueidentifier",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);
    }
}
