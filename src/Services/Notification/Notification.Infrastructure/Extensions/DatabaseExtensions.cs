using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies all pending migrations to the database.
    /// This is idempotent - safe to call multiple times.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        
        try
        {
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"📊 Applying {pendingMigrations.Count} pending migration(s)...");
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine($"  • Applying: {migration}");
                }
                
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("✅ All migrations applied successfully");
            }
            else
            {
                Console.WriteLine("✅ Database is up to date - no pending migrations");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error applying migrations: {ex.Message}");
            throw;
        }
    }
}
