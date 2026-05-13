using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Realtime.API.Hubs;
using Realtime.API.Services;
using Realtime.Application.Interfaces;
using Realtime.Infrastructure.Azure;
using Realtime.Infrastructure.Messaging;
using Realtime.Infrastructure.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault();

var jwtKey = builder.Configuration["JwtSettings:Secret"] ?? "default-secret-key-32-characters!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "RecruitmentPlatform";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "RecruitmentPlatformUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/realtime"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Realtime Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

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

var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConn))
{
    try
    {
        var redisOptions = ConfigurationOptions.Parse(redisConn);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectRetry = 5;
        redisOptions.ConnectTimeout = 5000;

        var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);
        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        builder.Services.AddSingleton<IRealtimeIdempotencyStore, RedisRealtimeIdempotencyStore>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Realtime] Redis unavailable, fallback to in-memory idempotency store. Error: {ex.Message}");
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton<IRealtimeIdempotencyStore, MemoryRealtimeIdempotencyStore>();
    }
}
else
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IRealtimeIdempotencyStore, MemoryRealtimeIdempotencyStore>();
}

builder.Services.AddSingleton<IRealtimePushService, SignalRPushService>();

builder.Services.AddHostedService<CommentEventConsumer>();
builder.Services.AddHostedService<ReplyEventConsumer>();
builder.Services.AddHostedService<NotificationEventConsumer>();
builder.Services.AddHostedService<PostFavoriteEventConsumer>();
builder.Services.AddHostedService<ConnectionRequestedEventConsumer>();
builder.Services.AddHostedService<ConnectionAcceptedEventConsumer>();
builder.Services.AddHostedService<SkillPointsAwardedEventConsumer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Realtime API v1"));

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RealtimeHub>("/hubs/realtime");

app.Run();


