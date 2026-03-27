using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Domain.Interfaces;
using Payment.Infrastructure.Azure;
using Payment.Infrastructure.Configuration;
using Payment.Infrastructure.Data;
using Payment.Infrastructure.Providers.MoMo;
using Payment.Infrastructure.Providers.VNPay;
using Payment.Infrastructure.Repositories;
using Payment.Infrastructure.Services;
using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault (works in Azure with Managed Identity, skips if not configured)
builder.Configuration.AddAzureKeyVault();

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentDb")));

// Repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
builder.Services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
builder.Services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();

// Application Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWebhookHandler, WebhookHandler>();

// Payment Providers
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VNPay"));
builder.Services.Configure<MoMoSettings>(builder.Configuration.GetSection("MoMo"));
builder.Services.AddScoped<IPaymentProvider, VnPayProvider>();
builder.Services.AddHttpClient<IPaymentProvider, MoMoProvider>();

// Plan Price Provider (HTTP client to Subscription Service)
builder.Services.AddHttpClient<IPlanPriceProvider, HttpPlanPriceProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:SubscriptionService"] ?? "http://subscription-service:5008");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq",
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };
    return factory.CreateConnection();
});

builder.Services.AddSingleton<IPaymentEventPublisher, RabbitMqPaymentEventPublisher>();

// Background Services
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<PaymentExpirationService>();

// Validate required secrets in Production
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.ValidateRequiredSecrets(
        "Jwt:Secret",
        "VNPay:TmnCode",
        "VNPay:HashSecret",
        "MoMo:PartnerCode",
        "MoMo:AccessKey",
        "MoMo:SecretKey"
    );
}

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Service API",
        Version = "v1",
        Description = "Production-grade payment service with VNPay and MoMo integration"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.Migrate();
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API V1");
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
