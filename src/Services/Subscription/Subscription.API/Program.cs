using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

<<<<<<< HEAD
// Add services to the container.
// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
=======
// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost",
        Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        UserName = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
        Password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest"
    };
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
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

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

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();
>>>>>>> 52bc06426d6e7755e66ecdaab8d748db4b31d45e

app.UseAuthentication();
app.UseAuthorization();
app.UseFeatureAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
