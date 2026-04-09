using Connection.Infrastructure.Azure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// HttpClient to call UserProfile service
builder.Services.AddHttpClient("UserProfile", client =>
{
    var userProfileUrl = builder.Configuration["ServiceUrls:UserProfileService"] ?? "http://userprofile-service:8080";
    client.BaseAddress = new Uri(userProfileUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register DbContext and DI
builder.Services.AddDbContext<Connection.Infrastructure.Data.ConnectionDbContext>(options =>
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

builder.Services.AddScoped<Connection.Application.Interfaces.IConnectionRepository, Connection.Infrastructure.Repositories.ConnectionRepository>();
builder.Services.AddScoped<Connection.Application.Interfaces.IConnectionService, Connection.Application.Services.ConnectionService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
                  "https://sep-490-web-fork.vercel.app",
                  "http://localhost:3000",
                  "https://sep-490-dashboard-fork.vercel.app/",
                  "http://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Connection.Infrastructure.Data.ConnectionDbContext>();
    db.Database.Migrate();
}

// AutoMapper not used; mapping done manually

// Configure the HTTP request pipeline - Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connection API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Connection.API.Hubs.ChatHub>("/hubs/chat");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
