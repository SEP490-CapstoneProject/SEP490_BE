using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies all pending migrations to the database.
    /// This is idempotent - safe to call multiple times.
    /// If columns already exist, will skip creation gracefully.
    /// Falls back to raw SQL if migrations don't apply columns.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var config = scope.ServiceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
        var disableAuto = false;
        if (config != null)
        {
            var raw = config["DisableAutoMigrations"];
            if (!string.IsNullOrEmpty(raw) && bool.TryParse(raw, out var parsed))
            {
                disableAuto = parsed;
            }
        }

        if (disableAuto)
        {
            Console.WriteLine("Auto migrations disabled via DisableAutoMigrations=true");
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            
            // Check current column state
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'NOTIFICATION' AND COLUMN_NAME IN ('EventId', 'ActorName', 'ActorAvatar')
            ";
            var existingColumns = (int?)await command.ExecuteScalarAsync() ?? 0;
            
            Console.WriteLine($"📊 Checking for pending migrations (found {existingColumns}/3 legacy columns)...");
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"📊 Applying {pendingMigrations.Count} pending migration(s)...");
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine($"  • {migration}");
                }
                
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("✅ Migrations applied successfully");
            }
            else
            {
                Console.WriteLine("⚠️  No pending migrations found in EF history - checking database directly...");
            }

            // Verify columns were created, if not use raw SQL
            command.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'NOTIFICATION' AND COLUMN_NAME IN ('EventId', 'ActorName', 'ActorAvatar')
            ";
            var finalColumnCount = (int?)await command.ExecuteScalarAsync() ?? 0;
            
            if (finalColumnCount < 3)
            {
                Console.WriteLine($"⚠️  Missing {3 - finalColumnCount} column(s) - applying raw SQL fallback...");
                
                // Fallback: Use raw SQL to ensure columns exist
                command.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NOTIFICATION' AND COLUMN_NAME = 'EventId')
                        ALTER TABLE [NOTIFICATION] ADD [EventId] nvarchar(100) NULL;
                    
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NOTIFICATION' AND COLUMN_NAME = 'ActorName')
                        ALTER TABLE [NOTIFICATION] ADD [ActorName] nvarchar(255) NULL;
                    
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NOTIFICATION' AND COLUMN_NAME = 'ActorAvatar')
                        ALTER TABLE [NOTIFICATION] ADD [ActorAvatar] nvarchar(500) NULL;
                ";
                
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Fallback SQL applied - columns verified/created");
            }
            else
            {
                Console.WriteLine("✅ All required columns verified to exist");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error applying migrations: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
