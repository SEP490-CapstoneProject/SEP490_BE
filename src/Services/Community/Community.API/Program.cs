using Microsoft.EntityFrameworkCore;
using Community.Application.Clients;
using Community.Application.Interfaces;
using Community.Application.Services;
using Community.Infrastructure.Clients;
using Community.Infrastructure.Data;
using Community.Infrastructure.Repositories;
using Community.Infrastructure.Services;
using Community.Infrastructure.Azure;
using Community.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RecruitmentPlatform.AI.Services;
using RecruitmentPlatform.Common;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Community API",
        Version = "v1",
        Description = "Community Service — Posts, Comments, Replies, Feed"
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

// Add DbContext
builder.Services.AddDbContext<CommunityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add DI
builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<ICommunityEventPublisher, RabbitMqCommunityEventPublisher>();
builder.Services.AddScoped<INotificationEventPublisher, RabbitMqNotificationEventPublisher>();
builder.Services.AddScoped<ModerationService>();

// Add typed HttpClient for Media Service
var mediaServiceUrl = builder.Configuration["ServiceUrls:MediaService"] ?? "http://media-service:8080";
builder.Services.AddHttpClient<IMediaUploadClient, MediaUploadClient>(client =>
{
    client.BaseAddress = new Uri(mediaServiceUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Add UserInfo client (UserProfile service - employee + company)
var userProfileUrl = builder.Configuration["ServiceUrls:UserProfileService"] ?? "http://userprofile-service:8080";
builder.Services.AddHttpClient<IUserInfoClient, UserInfoClient>(c =>
    c.BaseAddress = new Uri(userProfileUrl));

// Add Portfolio preview client
var portfolioUrl = builder.Configuration["ServiceUrls:PortfolioService"] ?? "http://portfolio-service:8080";
builder.Services.AddHttpClient<IPortfolioPreviewClient, PortfolioPreviewClient>(c =>
    c.BaseAddress = new Uri(portfolioUrl));

// Add HTTP client for Portfolio feed service (sponsored posts)
builder.Services.AddHttpClient("PortfolioFeedClient", client =>
{
    client.BaseAddress = new Uri(portfolioUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// JWT Authentication - Use consistent JwtSettings configuration
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

// Add CORS
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

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
    db.Database.Migrate();
}

// Configure pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Community API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
