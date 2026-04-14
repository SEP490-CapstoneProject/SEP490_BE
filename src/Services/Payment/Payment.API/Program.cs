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
using Payment.Infrastructure.Providers.PayOS;
using Payment.Infrastructure.Repositories;
using Payment.Infrastructure.Services;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Structured Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "json";
});
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Add Azure Key Vault (works in Azure with Managed Identity, skips if not configured)
builder.Configuration.AddAzureKeyVault();

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
builder.Services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
builder.Services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();

// Application Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IPaymentVerificationService, PaymentVerificationService>();

// PayOS Provider (single provider, no more VNPay/MoMo)
builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOS"));
builder.Services.AddScoped<IPaymentProvider, PayOSProvider>();

// PayOS HttpClient with Polly resilience policies
builder.Services.AddHttpClient<PayOSHttpClient>(client =>
{
    var baseUrl = builder.Configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

// Plan Price Provider (HTTP client to Subscription Service)
builder.Services.AddHttpClient<IPlanPriceProvider, HttpPlanPriceProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:SubscriptionService"] ?? "http://subscription-service:5008");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// RabbitMQ
// builder.Services.AddSingleton<IConnection>(sp =>
// {
//     var factory = new ConnectionFactory
//     {
//         HostName = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq",
//         Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
//         UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
//         Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
//         AutomaticRecoveryEnabled = true,
//         NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
//     };
//     return factory.CreateConnection();
// });

// builder.Services.AddSingleton<IPaymentEventPublisher, RabbitMqPaymentEventPublisher>();
builder.Services.AddSingleton<IPaymentEventPublisher, FakePaymentEventPublisher>();


// Metrics & Alerting
builder.Services.AddSingleton<IPaymentMetricsService, PaymentMetricsService>();
builder.Services.AddSingleton<IAlertingService, AlertingService>();

// Background Services
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<PaymentExpirationService>();
builder.Services.AddHostedService<ReconciliationService>();
// builder.Services.AddHostedService<DLQMonitoringService>();

// Validate required secrets in Production
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.ValidateRequiredSecrets(
        "Jwt:Secret",
        "PayOS:ClientId",
        "PayOS:ApiKey",
        "PayOS:ChecksumKey"
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
        Version = "v2",
        Description = "Production-grade payment service with PayOS integration (fintech-grade)"
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
        policy.WithOrigins(
                  "https://sep-490-web-fork.vercel.app",
                  "http://localhost:3000",
                  "http://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.Migrate();
}

// Configure HTTP pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API V2");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Polly Policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"PayOS API retry {retryCount} after {timespan.TotalSeconds}s");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
            {
                Console.WriteLine($"PayOS API circuit breaker OPEN for {duration.TotalSeconds}s");
            },
            onReset: () =>
            {
                Console.WriteLine("PayOS API circuit breaker RESET");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}
