using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Portfolio.Infrastructure.Data;
using Portfolio.Infrastructure.Azure;
using Portfolio.Infrastructure.Configuration;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;
using Portfolio.Infrastructure.Repositories;
using Portfolio.Infrastructure.Clients;
using Portfolio.Infrastructure.Services;
using Portfolio.Infrastructure.Messaging;
using Portfolio.Application.BlockHandlers;
using RecruitmentPlatform.AI.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Add MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Portfolio API",
        Version = "v1",
        Description = "Portfolio Management Service — Supports dynamic blocks with image uploads"
    });

    // JWT support in Swagger UI
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

// DbContext
builder.Services.AddDbContext<PortfolioDbContext>(options =>
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
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CompanyOnly", policy => policy.RequireRole("company"));
});

// Repositories
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPortfolioFollowRepository, PortfolioFollowRepository>();
builder.Services.AddScoped<IPortfolioFollowCategoryRepository, PortfolioFollowCategoryRepository>();
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IBlockTypeRepository, BlockTypeRepository>();
builder.Services.AddScoped<IComplimentRepository, ComplimentRepository>();
builder.Services.AddScoped<ICriterionRepository, CriterionRepository>();

// Application Services
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<BlockService>(); // concrete registration for PortfolioController
builder.Services.AddScoped<IBlockTypeService, BlockTypeService>();
builder.Services.AddScoped<IComplimentService, ComplimentService>();
builder.Services.AddScoped<IPortfolioFollowService, PortfolioFollowService>();
builder.Services.AddScoped<IPortfolioFollowCategoryService, PortfolioFollowCategoryService>();
builder.Services.AddScoped<IPortfolioEmbeddingEventPublisher, PortfolioEmbeddingEventPublisher>();
builder.Services.AddScoped<IPortfolioNotificationEventPublisher, PortfolioNotificationEventPublisher>();
builder.Services.AddRecruitmentPlatformAi(builder.Configuration);
builder.Services.AddHostedService<PortfolioEmbeddingConsumer>();
builder.Services.AddHostedService<PortfolioEmbeddingBackfillWorker>();
builder.Services.AddScoped<ICriterionService, CriterionService>();

// Block Handlers
builder.Services.AddScoped<IBlockHandler, IntroBlockHandler>();
builder.Services.AddScoped<IBlockHandler, SkillBlockHandler>();
builder.Services.AddScoped<IBlockHandler, EducationBlockHandler>();
builder.Services.AddScoped<IBlockHandler, DiplomaBlockHandler>();
builder.Services.AddScoped<IBlockHandler, ExperienceBlockHandler>();
builder.Services.AddScoped<IBlockHandler, ProjectBlockHandler>();
builder.Services.AddScoped<IBlockHandler, AwardBlockHandler>();
builder.Services.AddScoped<IBlockHandler, ActivitiesBlockHandler>();
builder.Services.AddScoped<IBlockHandler, OtherInfoBlockHandler>();
builder.Services.AddScoped<IBlockHandler, ReferenceBlockHandler>();
// External HTTP Clients
var serviceUrls = builder.Configuration.GetSection("ServiceUrls");

builder.Services.AddHttpClient<IMediaServiceClient, MediaServiceClient>(client =>
{
    client.BaseAddress = new Uri(serviceUrls["MediaService"] ?? "http://media-service:8080");
});

builder.Services.AddHttpClient<IMediaService, Portfolio.Infrastructure.Services.MediaService>(client =>
{
    client.BaseAddress = new Uri(serviceUrls["MediaService"] ?? "http://media-service:8080");
});

builder.Services.AddHttpClient<IEmployeeServiceClient, EmployeeServiceClient>(client =>
{
    client.BaseAddress = new Uri(serviceUrls["UserProfileService"] ?? "http://userprofile-service:8080");
});

builder.Services.AddHttpClient<IReviewerProfileClient, ReviewerProfileClient>(client =>
{
    client.BaseAddress = new Uri(serviceUrls["UserProfileService"] ?? "http://userprofile-service:8080");
});

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(serviceUrls["AuthService"] ?? "http://auth-service:8080");
});

builder.Services.AddHttpClient<ICompanyMatchingClient, CompanyMatchingClient>(client =>
{
    client.BaseAddress = new Uri(serviceUrls["CompanyService"] ?? "http://company-service:8080");
    client.Timeout = TimeSpan.FromSeconds(2);
});

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

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Portfolio', 'Embedding') IS NULL
    ALTER TABLE [Portfolio] ADD [Embedding] NVARCHAR(MAX) NULL;
IF COL_LENGTH('Portfolio', 'EmbeddingVersion') IS NULL
    ALTER TABLE [Portfolio] ADD [EmbeddingVersion] INT NOT NULL CONSTRAINT DF_Portfolio_EmbeddingVersion DEFAULT 0;
IF COL_LENGTH('Portfolio', 'EmbeddingUpdatedAt') IS NULL
    ALTER TABLE [Portfolio] ADD [EmbeddingUpdatedAt] DATETIME2 NULL;
IF COL_LENGTH('Portfolio', 'EmbeddingStatus') IS NULL
    ALTER TABLE [Portfolio] ADD [EmbeddingStatus] NVARCHAR(20) NOT NULL CONSTRAINT DF_Portfolio_EmbeddingStatus DEFAULT 'Pending';
IF COL_LENGTH('Portfolio', 'ModerationStatus') IS NULL
    ALTER TABLE [Portfolio] ADD [ModerationStatus] NVARCHAR(20) NOT NULL CONSTRAINT DF_Portfolio_ModerationStatus DEFAULT 'PendingReview';
IF COL_LENGTH('Portfolio', 'ModerationReason') IS NULL
    ALTER TABLE [Portfolio] ADD [ModerationReason] NVARCHAR(500) NULL;
IF COL_LENGTH('Portfolio', 'ModeratedAt') IS NULL
    ALTER TABLE [Portfolio] ADD [ModeratedAt] DATETIME2 NULL;");
}

// Pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Portfolio API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
