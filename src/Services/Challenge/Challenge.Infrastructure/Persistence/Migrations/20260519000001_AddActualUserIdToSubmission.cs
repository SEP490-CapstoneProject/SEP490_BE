using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Challenge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActualUserIdToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualUserId",
                table: "CHALLENGE_SUBMISSIONS",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHALLENGE_SUBMISSIONS_ActualUserId",
                table: "CHALLENGE_SUBMISSIONS",
                column: "ActualUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CHALLENGE_SUBMISSIONS_ActualUserId",
                table: "CHALLENGE_SUBMISSIONS");

            migrationBuilder.DropColumn(
                name: "ActualUserId",
                table: "CHALLENGE_SUBMISSIONS");
        }
    }
}
