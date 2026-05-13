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
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ChallengeDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}

