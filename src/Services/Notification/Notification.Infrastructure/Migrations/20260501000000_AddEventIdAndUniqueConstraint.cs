using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventIdAndUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add EventId column
            migrationBuilder.AddColumn<string>(
                name: "EventId",
                table: "NOTIFICATION",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Create unique index on (EventId, UserId, Type, ObjectId) where EventId IS NOT NULL
            migrationBuilder.CreateIndex(
                name: "IX_Unique_Notification_Event",
                table: "NOTIFICATION",
                columns: new[] { "EventId", "UserId", "Type", "ObjectId" },
                unique: true,
                filter: "[EventId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop unique index
            migrationBuilder.DropIndex(
                name: "IX_Unique_Notification_Event",
                table: "NOTIFICATION");

            // Remove EventId column
            migrationBuilder.DropColumn(
                name: "EventId",
                table: "NOTIFICATION");
        }
    }
}
