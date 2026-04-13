using Connection.Infrastructure.Azure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent circular reference errors when serializing entities with navigation properties
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Connection.API",
        Version = "v1",
        Description = "Connection Service — Matching & messaging"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter your token (without 'Bearer' prefix).",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR();

// HttpClient to call UserProfile service
builder.Services.AddHttpClient("UserProfile", client =>
{
    var userProfileUrl = builder.Configuration["ServiceUrls:UserProfileService"] ?? "http://userprofile-service:8080";
    client.BaseAddress = new Uri(userProfileUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register DbContext and DI
builder.Services.AddDbContext<Connection.Infrastructure.Data.ConnectionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))  // ✅ Changed from /hubs/chat to /hubs/realtime
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<Connection.Application.Interfaces.IConnectionRepository, Connection.Infrastructure.Repositories.ConnectionRepository>();
builder.Services.AddScoped<Connection.Application.Interfaces.IConnectionService, Connection.Application.Services.ConnectionService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
                  "https://sep-490-web-fork.vercel.app",
                  "http://localhost:3000",
                  "https://sep-490-dashboard-fork.vercel.app/",
                  "http://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());

});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Connection.Infrastructure.Data.ConnectionDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Starting database migration...");
        db.Database.Migrate();
        logger.LogInformation("✅ Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database migration failed at startup.");
        logger.LogError("Connection String: {ConnectionString}", 
            builder.Configuration.GetConnectionString("DefaultConnection"));
        logger.LogError("This may indicate:");
        logger.LogError("  1. Database server is not reachable");
        logger.LogError("  2. Connection string is incorrect");
        logger.LogError("  3. Insufficient permissions to create database");
        throw;
    }
}

// AutoMapper not used; mapping done manually

// Configure the HTTP request pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connection API V1");
    c.RoutePrefix = "swagger";
});

// Global exception handler - return JSON error instead of empty 500
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = ex?.Error?.Message ?? "An unexpected error occurred";
        var inner = ex?.Error?.InnerException?.Message;
        await ctx.Response.WriteAsJsonAsync(new { error = message, inner });
    });
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Connection.API.Hubs.ChatHub>("/hubs/chat");  // ✅ Changed from /hubs/chat to /hubs/realtime

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
