using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserProfile.Application.Interfaces;
using UserProfile.Application.Services;
using UserProfile.Infrastructure.Data;
using UserProfile.Infrastructure.Repositories;
using UserProfile.Infrastructure.Azure;
using UserProfile.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault (works in Azure with Managed Identity, skips if not configured)
builder.Configuration.AddAzureKeyVault();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "UserProfile API", 
        Version = "v1",
        Description = "Employee and Company Profile Management API"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Just enter your token below (without 'Bearer' prefix).",
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
builder.Services.AddDbContext<UserProfileDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

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

// Add HttpClient for Media Service
builder.Services.AddHttpClient("MediaService", client =>
{
    var mediaServiceUrl = builder.Configuration["ServiceUrls:MediaService"] ?? "http://media-service:8080";
    client.BaseAddress = new Uri(mediaServiceUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Add HttpClient for Auth Service (service-to-service)
builder.Services.AddHttpClient("AuthService", client =>
{
    var authServiceUrl = builder.Configuration["ServiceUrls:AuthService"] ?? "http://auth-service:8080";
    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add DI
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IAuthUserClient, AuthUserClient>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
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

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
    db.Database.Migrate();
}

// Configure pipeline - Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserProfile API V1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
