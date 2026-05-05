using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Infrastructure.Azure;
using Notification.Infrastructure.Clients;
using Notification.Infrastructure.Configuration;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Extensions;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Repositories;
using Notification.Infrastructure.Services;
using RecruitmentPlatform.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault();

// Use consistent JwtSettings configuration across all services
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings
{
    Secret = "default-secret-key-32-characters!",
    Issuer = "RecruitmentPlatform",
    Audience = "RecruitmentPlatformUsers"
};
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token (không cần gõ 'Bearer').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();

var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = redisConn);
    
    // Add IConnectionMultiplexer for AggregationFlushService
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
    {
        var configuration = sp.GetService<IConfiguration>();
        var connectionString = configuration!.GetConnectionString("Redis") ?? redisConn;
        return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                  "https://sep-490-web-fork.vercel.app",
                  "http://localhost:3000",
                  "https://sep-490-dashboard-fork.vercel.app",
                  "http://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient<IActorResolverClient, ActorResolverClient>(client =>
{
    var url = builder.Configuration["ServiceUrls:UserProfile"] ?? "http://userprofile-service:8080";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<IRecipientResolverClient, RecipientResolverClient>(client =>
{
    var url = builder.Configuration["ServiceUrls:AuthService"] ?? "http://auth-service:8080";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationEventPublisher, RabbitMqNotificationEventPublisher>();
builder.Services.AddScoped<FavoriteAggregationService>();
builder.Services.AddScoped<CommentReplyAggregationService>();
builder.Services.AddScoped<PostReportAggregationService>();

// FCM Services Registration
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();
builder.Services.AddScoped<IFcmService, FcmService>();
builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
builder.Services.AddScoped<FcmRetryService>();
builder.Services.AddScoped<FcmAnalyticsService>();
builder.Services.AddScoped<NotificationPublishingService>();

builder.Services.AddHostedService<RabbitMQConsumer>();
builder.Services.AddHostedService<AggregationFlushService>();

builder.Services.AddControllers();

var app = builder.Build();

// Apply all pending migrations with proper error handling and logging
try
{
    await app.Services.ApplyMigrationsAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"❌ FATAL: Failed to apply database migrations: {ex}");
    throw;
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API v1"));

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

