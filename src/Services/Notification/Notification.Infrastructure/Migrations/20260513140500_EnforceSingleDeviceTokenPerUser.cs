using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Notification.Infrastructure.Data;

#nullable disable

namespace Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NotificationDbContext))]
    [Migration("20260513140500_EnforceSingleDeviceTokenPerUser")]
    public partial class EnforceSingleDeviceTokenPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DEVICE_TOKENS_UserId_IsActive'
      AND object_id = OBJECT_ID('[DEVICE_TOKENS]')
)
BEGIN
    DROP INDEX [IX_DEVICE_TOKENS_UserId_IsActive] ON [DEVICE_TOKENS];
END
");

            migrationBuilder.Sql(@"
;WITH RankedTokens AS (
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY UserId
            ORDER BY
                CASE WHEN LastUsedAt IS NULL THEN RegisteredAt ELSE LastUsedAt END DESC,
                RegisteredAt DESC,
                Id DESC
        ) AS rn
    FROM DEVICE_TOKENS
)
DELETE dt
FROM DEVICE_TOKENS dt
INNER JOIN RankedTokens rt ON dt.Id = rt.Id
WHERE rt.rn > 1;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DEVICE_TOKENS_UserId'
      AND object_id = OBJECT_ID('[DEVICE_TOKENS]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_DEVICE_TOKENS_UserId] ON [DEVICE_TOKENS]([UserId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DEVICE_TOKENS_UserId'
      AND object_id = OBJECT_ID('[DEVICE_TOKENS]')
)
BEGIN
    DROP INDEX [IX_DEVICE_TOKENS_UserId] ON [DEVICE_TOKENS];
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DEVICE_TOKENS_UserId_IsActive'
      AND object_id = OBJECT_ID('[DEVICE_TOKENS]')
)
BEGIN
    CREATE INDEX [IX_DEVICE_TOKENS_UserId_IsActive] ON [DEVICE_TOKENS]([UserId], [IsActive]);
END
");
        }
    }
}
