using Company.Application.Clients;
using Company.Application.Interfaces;
using Company.Application.Services;
using Company.Infrastructure.Azure;
using Company.Infrastructure.Clients;
using Company.Infrastructure.Configuration;
using Company.Infrastructure.Data;
using Company.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
});

builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICompanyPostRepository, CompanyPostRepository>();
builder.Services.AddScoped<ICompanyPostService, CompanyPostService>();
builder.Services.AddScoped<ICompanyCacheRepository, CompanyCacheRepository>();

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

var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });
}
else
{
    builder.Services.AddAuthentication();
}
builder.Services.AddAuthorization();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
    db.Database.Migrate();
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
