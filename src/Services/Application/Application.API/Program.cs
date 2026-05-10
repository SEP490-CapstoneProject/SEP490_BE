using Application.Application.Interfaces;
using Application.Application.Services;
using Application.Infrastructure.Azure;
using Application.Infrastructure.Clients;
using Application.Infrastructure.Configuration;
using Application.Infrastructure.Data;
using Application.Infrastructure.Messaging;
using Application.Infrastructure.Repositories;
using Application.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

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
});

builder.Services.AddAuthorization();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IEntitlementChecker, EntitlementChecker>();
builder.Services.AddScoped<IApplicationNotificationEventPublisher, RabbitMqApplicationNotificationEventPublisher>();

// HTTP Client with Polly
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(10);

// UserProfileClient - uses a custom factory to route to multiple services
builder.Services.AddHttpClient("UserProfileService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:UserProfileService"] 
        ?? "https://userprofile-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io";
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("CompanyService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:CompanyService"]
        ?? "https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io";
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("PortfolioService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:PortfolioService"]
        ?? "https://portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io";
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddScoped<IUserProfileClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<UserProfileClient>>();
    var userProfileClient = httpClientFactory.CreateClient("UserProfileService");
    var companyClient = httpClientFactory.CreateClient("CompanyService");
    var portfolioClient = httpClientFactory.CreateClient("PortfolioService");
    
    return new UserProfileClient(userProfileClient, companyClient, portfolioClient, logger);
});

builder.Services.AddHttpClient<ISubscriptionClient, SubscriptionClient>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:SubscriptionService"] ?? "http://localhost:5008";
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(timeoutPolicy);

// CORS
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

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Application API",
        Version = "v1",
        Description = "Job Application Management Service"
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

var app = builder.Build();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Application API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
