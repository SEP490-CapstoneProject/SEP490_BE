using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Portfolio.Infrastructure.Data;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;
using Portfolio.Infrastructure.Repositories;
using Portfolio.Infrastructure.Clients;
using Portfolio.Infrastructure.Services;
using Portfolio.Application.BlockHandlers;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthorization();

// Repositories
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IBlockTypeRepository, BlockTypeRepository>();

// Application Services
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<BlockService>(); // concrete registration for PortfolioController
builder.Services.AddScoped<IBlockTypeService, BlockTypeService>();

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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    db.Database.Migrate();
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
