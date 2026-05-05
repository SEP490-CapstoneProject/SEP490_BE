using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFcmTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DEVICE_TOKENS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEVICE_TOKENS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_SETTINGS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PushNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SoundEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    VibrateEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ChatNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MentionNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_SETTINGS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PUSH_NOTIFICATION_LOG",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    DeviceTokenId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUSH_NOTIFICATION_LOG", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_DEVICE_TOKENS_DeviceToken",
                table: "DEVICE_TOKENS",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DEVICE_TOKENS_UserId_IsActive",
                table: "DEVICE_TOKENS",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTINGS_UserId",
                table: "NOTIFICATION_SETTINGS",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PUSH_NOTIFICATION_LOG_NotificationId_DeviceTokenId",
                table: "PUSH_NOTIFICATION_LOG",
                columns: new[] { "NotificationId", "DeviceTokenId" });

            migrationBuilder.CreateIndex(
                name: "IX_PUSH_NOTIFICATION_LOG_Status",
                table: "PUSH_NOTIFICATION_LOG",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DEVICE_TOKENS");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_SETTINGS");

            migrationBuilder.DropTable(
                name: "PUSH_NOTIFICATION_LOG");
        }
    }
}
