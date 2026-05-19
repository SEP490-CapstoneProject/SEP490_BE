using Microsoft.EntityFrameworkCore.Migrations;
namespace Challenge.Infrastructure.Persistence.Migrations;

/// <summary>
/// Migration to add ProposedById tracking to PendingSkill entity
/// Allows auditing which user proposed each pending skill for approval
/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class AddProposedByIdToPendingSkills : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ProposedById",
            table: "PENDING_SKILLS",
            type: "uniqueidentifier",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProposedById",
            table: "PENDING_SKILLS");
    }
}
#pragma warning restore CS1591
