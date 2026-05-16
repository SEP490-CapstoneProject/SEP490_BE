using Microsoft.EntityFrameworkCore;
using Challenge.Infrastructure.Persistence;
using Challenge.Infrastructure;
using Microsoft.OpenApi.Models;
namespace Challenge.API;


public static class ChallengeApiStartup
{
    public static void AddChallengeApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        services.AddDbContext<ChallengeDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Services
        services.AddChallengeServices(configuration);

        // Controllers
        services.AddControllers();

        // Swagger
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Challenge API",
                Version = "v1",
                Description = "AI Challenge System for Skill Verification"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer {token}'"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public static void UseChallengeApi(
        this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Challenge API v1");
        });

        app.MapControllers();
    }

    public static async Task MigrateDatabaseAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ChallengeDatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ChallengeDbContext>();

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            logger.LogInformation("Challenge pending migrations: {Count}", pendingMigrations.Count());

            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Challenge EF migrations applied.");
            }

            var requiredTables = new[]
            {
                "SKILLS",
                "SKILL_ALIASES",
                "SKILL_CATEGORIES",
                "EVALUATION_CRITERIA",
                "CRITERIA_SKILL_MAPPINGS",
                "PENDING_SKILLS",
                "CHALLENGES",
                "CHALLENGE_VERSIONS",
                "CHALLENGE_CRITERIA",
                "CHALLENGE_SUBMISSIONS",
                "SUBMISSION_CRITERIA_SCORES",
                "SKILL_POINT_TRANSACTIONS",
                "USER_SKILLS",
                "SKILL_RELATIONSHIPS",
                "PROMPT_SANITIZATION_LOGS"
            };

            var missingTables = new List<string>();
            foreach (var table in requiredTables)
            {
                if (!await TableExistsAsync(dbContext, table))
                {
                    missingTables.Add(table);
                }
            }

            if (missingTables.Any())
            {
                logger.LogWarning(
                    "Challenge schema missing tables after migration: {Tables}. Applying schema bootstrap script.",
                    string.Join(", ", missingTables));
                await RebuildChallengeSchemaAsync(dbContext);
            }

            await EnsureChallengeCurrentVersionNullableAsync(dbContext);

            foreach (var table in requiredTables)
            {
                if (!await TableExistsAsync(dbContext, table))
                {
                    throw new InvalidOperationException("Challenge schema bootstrap failed.");
                }
            }

            logger.LogInformation("Challenge database schema is ready.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Challenge database migration/bootstrap failed.");
            throw;
        }
    }

    private static async Task EnsureChallengeCurrentVersionNullableAsync(ChallengeDbContext dbContext)
    {
        const string dropForeignKey = @"
            IF EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId'
            )
            BEGIN
                ALTER TABLE [CHALLENGES] DROP CONSTRAINT [FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId];
            END";

        const string dropCurrentVersionIndex = @"
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CHALLENGES_CurrentVersionId'
                  AND object_id = OBJECT_ID('CHALLENGES')
            )
            BEGIN
                DROP INDEX [IX_CHALLENGES_CurrentVersionId] ON [CHALLENGES];
            END";

        const string alterColumn = @"
            ALTER TABLE [CHALLENGES]
            ALTER COLUMN [CurrentVersionId] uniqueidentifier NULL;";

        const string recreateCurrentVersionIndex = @"
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CHALLENGES_CurrentVersionId'
                  AND object_id = OBJECT_ID('CHALLENGES')
            )
            BEGIN
                DROP INDEX [IX_CHALLENGES_CurrentVersionId] ON [CHALLENGES];
            END;

            CREATE UNIQUE INDEX [IX_CHALLENGES_CurrentVersionId]
            ON [CHALLENGES] ([CurrentVersionId])
            WHERE [CurrentVersionId] IS NOT NULL;";

        const string addForeignKey = @"
            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId'
            )
            BEGIN
                ALTER TABLE [CHALLENGES]
                ADD CONSTRAINT [FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId]
                FOREIGN KEY ([CurrentVersionId]) REFERENCES [CHALLENGE_VERSIONS]([Id]);
            END";

        await dbContext.Database.ExecuteSqlRawAsync(dropForeignKey);
        await dbContext.Database.ExecuteSqlRawAsync(dropForeignKey);
        await dbContext.Database.ExecuteSqlRawAsync(alterColumn);
        await dbContext.Database.ExecuteSqlRawAsync(recreateCurrentVersionIndex);
        await dbContext.Database.ExecuteSqlRawAsync(addForeignKey);
    }

    private static async Task RebuildChallengeSchemaAsync(ChallengeDbContext dbContext)
    {
        var dropScript = string.Join(
            Environment.NewLine,
            new[]
            {
                "DROP TABLE IF EXISTS [PROMPT_SANITIZATION_LOGS];",
                "DROP TABLE IF EXISTS [SKILL_RELATIONSHIPS];",
                "DROP TABLE IF EXISTS [USER_SKILLS];",
                "DROP TABLE IF EXISTS [SKILL_POINT_TRANSACTIONS];",
                "DROP TABLE IF EXISTS [SUBMISSION_CRITERIA_SCORES];",
                "DROP TABLE IF EXISTS [CHALLENGE_SUBMISSIONS];",
                "DROP TABLE IF EXISTS [CHALLENGE_CRITERIA];",
                "DROP TABLE IF EXISTS [CHALLENGE_VERSIONS];",
                "DROP TABLE IF EXISTS [CHALLENGES];",
                "DROP TABLE IF EXISTS [PENDING_SKILLS];",
                "DROP TABLE IF EXISTS [CRITERIA_SKILL_MAPPINGS];",
                "DROP TABLE IF EXISTS [EVALUATION_CRITERIA];",
                "DROP TABLE IF EXISTS [SKILL_ALIASES];",
                "DROP TABLE IF EXISTS [SKILL_CATEGORIES];",
                "DROP TABLE IF EXISTS [SKILLS];"
            });

        await dbContext.Database.ExecuteSqlRawAsync(dropScript);

        var createScript = dbContext.Database.GenerateCreateScript();
        createScript = string.Join(
            Environment.NewLine,
            createScript
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(line => !string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase)));

        await dbContext.Database.ExecuteSqlRawAsync(createScript);
    }

    private static async Task<bool> TableExistsAsync(ChallengeDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = @tableName
            ";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
