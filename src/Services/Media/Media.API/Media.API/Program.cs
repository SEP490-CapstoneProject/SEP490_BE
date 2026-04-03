using Media.API.Models;
using Media.API.Services;
using Media.API.Azure;
using Media.API.Configuration;
using DotNetEnv;

// Load environment variables from .env.local
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Configure Kestrel to allow large file uploads (150MB)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 150 * 1024 * 1024; // 150MB
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Cloudinary settings (Key Vault/config first, env fallback)
builder.Services.Configure<CloudinarySettings>(options =>
{
    options.CloudName = builder.Configuration["Cloudinary:CloudName"]
                        ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
                        ?? "";
    options.ApiKey = builder.Configuration["Cloudinary:ApiKey"]
                     ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
                     ?? "";
    options.ApiSecret = builder.Configuration["Cloudinary:ApiSecret"]
                        ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
                        ?? "";
});

// Add DI
builder.Services.AddScoped<IMediaUploadService, CloudinaryUploadService>();

// Add CORS - Allow all microservices to call this service
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Media API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
