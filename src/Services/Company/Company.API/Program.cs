using Company.Application.Clients;
using Company.Application.Interfaces;
using Company.Application.Services;
using Company.Infrastructure.Azure;
using Company.Infrastructure.Clients;
using Company.Infrastructure.Configuration;
using Company.Infrastructure.Data;
using Company.Infrastructure.Repositories;
using Company.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecruitmentPlatform.AI.DependencyInjection;
using RecruitmentPlatform.AI.Services;
using System.Text;
using RecruitmentPlatform.Common;
using Company.API.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Company API",
        Version = "v1",
        Description = "Company Service — Job Posts Feed, Detail, CRUD, Save"
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

    c.OperationFilter<SwaggerIgnoreParameterOperationFilter>();
});

builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICompanyPostRepository, CompanyPostRepository>();
builder.Services.AddScoped<ICompanyPostService, CompanyPostService>();
builder.Services.AddScoped<ICompanyCacheRepository, CompanyCacheRepository>();
builder.Services.AddScoped<ICompanyEmbeddingEventPublisher, CompanyEmbeddingEventPublisher>();
builder.Services.AddScoped<ICompanyNotificationEventPublisher, RabbitMqCompanyNotificationEventPublisher>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddRecruitmentPlatformAi(builder.Configuration);
var disableBackgroundWorkers = builder.Configuration["DisableBackgroundWorkers"];
if (!(bool.TryParse(disableBackgroundWorkers, out var _disable) && _disable))
{
    builder.Services.AddHostedService<CompanyEmbeddingConsumer>();
    builder.Services.AddHostedService<CompanyEmbeddingBackfillWorker>();
}

var mediaServiceUrl = builder.Configuration["ServiceUrls:MediaService"] ?? "http://media-service:8080";
builder.Services.AddHttpClient<IMediaUploadClient, MediaUploadClient>(client =>
{
    client.BaseAddress = new Uri(mediaServiceUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

var userProfileServiceUrl = builder.Configuration["ServiceUrls:UserProfileService"] ?? "http://userprofile-service:8080";
builder.Services.AddHttpClient<ICompanyProfileClient, UserProfileCompanyClient>(client =>
{
    client.BaseAddress = new Uri(userProfileServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var portfolioServiceUrl = builder.Configuration["ServiceUrls:PortfolioService"] ?? "http://portfolio-service:8080";
builder.Services.AddHttpClient<IPortfolioMatchingClient, PortfolioMatchingClient>(client =>
{
    client.BaseAddress = new Uri(portfolioServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
});

// Add HTTP client for Portfolio feed service (sponsored posts)
builder.Services.AddHttpClient("PortfolioFeedClient", client =>
{
    client.BaseAddress = new Uri(portfolioServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

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

var app = builder.Build();

var disableAutoMigrations = builder.Configuration.GetValue<bool?>("DisableAutoMigrations") ?? false;
if (!disableAutoMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
        db.Database.Migrate();
        db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('companysvc.COMPANY_POST', 'embedding') IS NULL
    ALTER TABLE [companysvc].[COMPANY_POST] ADD [embedding] NVARCHAR(MAX) NULL;
IF COL_LENGTH('companysvc.COMPANY_POST', 'embeddingVersion') IS NULL
    ALTER TABLE [companysvc].[COMPANY_POST] ADD [embeddingVersion] INT NOT NULL CONSTRAINT DF_CompanyPost_EmbeddingVersion DEFAULT 0;
IF COL_LENGTH('companysvc.COMPANY_POST', 'embeddingUpdatedAt') IS NULL
    ALTER TABLE [companysvc].[COMPANY_POST] ADD [embeddingUpdatedAt] DATETIME2 NULL;
IF COL_LENGTH('companysvc.COMPANY_POST', 'embeddingStatus') IS NULL
    ALTER TABLE [companysvc].[COMPANY_POST] ADD [embeddingStatus] NVARCHAR(20) NOT NULL CONSTRAINT DF_CompanyPost_EmbeddingStatus DEFAULT 'Pending';");
    }
}
else
{
    Console.WriteLine("Auto migrations disabled via DisableAutoMigrations=true");
}

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Company API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
