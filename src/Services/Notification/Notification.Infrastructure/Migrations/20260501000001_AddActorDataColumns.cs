using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActorDataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ActorName and ActorAvatar columns
            // (EventId already added by 20260501000000_AddEventIdAndUniqueConstraint)
            
            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "NOTIFICATION",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorAvatar",
                table: "NOTIFICATION",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "NOTIFICATION");

            migrationBuilder.DropColumn(
                name: "ActorAvatar",
                table: "NOTIFICATION");
        }
    }
}
