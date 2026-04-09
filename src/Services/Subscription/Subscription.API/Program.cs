using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using StackExchange.Redis;
using Subscription.API.Middleware;
using Subscription.Application.Interfaces;
using Subscription.Application.Metrics;
using Subscription.Application.Services;
using Subscription.Infrastructure.Azure;
using Subscription.Infrastructure.Configuration;
using Subscription.Infrastructure.Data;
using Subscription.Infrastructure.Messaging;
using Subscription.Infrastructure.Repositories;
using Subscription.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Subscription Service API", Version = "v1" });
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
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Add DbContext
builder.Services.AddDbContext<SubscriptionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(config);
});

// Add RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var host = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
    var port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
    var username = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
    var password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";
    var virtualHost = builder.Configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/";
    var useSsl = builder.Configuration.GetValue<bool>("RabbitMQ:UseSsl");

    if (!useSsl && port == 5671)
    {
        useSsl = true;
    }

    var factory = new ConnectionFactory
    {
        HostName = host,
        Port = port,
        UserName = username,
        Password = password,
        VirtualHost = virtualHost
    };

    if (useSsl)
    {
        factory.Ssl = new SslOption
        {
            Enabled = true,
            ServerName = host
        };
    }

    return factory.CreateConnection();
});

// Add Repositories
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Add Services
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAdminSubscriptionService, AdminSubscriptionService>();
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<AnalyticsService>();

// Add Metrics
builder.Services.AddSingleton<SubscriptionMetrics>();

// Add Background Services
builder.Services.AddHostedService<PaymentEventConsumer>();
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<SubscriptionPreloadService>();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "default-secret-key-12345"))
        };
    });

// Add Authorization Policies
// Role-based policies to control access to endpoints
// AdminOnly: Requires user to have "Admin" role in JWT token
// UserOnly: Requires user to have "User" role in JWT token
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("UserOnly", policy => 
        policy.RequireRole("User"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                  "https://sep-490-web-fork.vercel.app",
                  "http://localhost:3000",
                  "https://sep-490-dashboard-fork.vercel.app/",
                  "http://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseFeatureAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
    db.Database.Migrate();
}

app.Run();
