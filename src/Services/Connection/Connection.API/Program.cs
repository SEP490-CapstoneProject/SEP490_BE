using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// HttpClient to call UserProfile service
builder.Services.AddHttpClient("UserProfile", client =>
{
    client.BaseAddress = new Uri("http://userprofile-service:8080");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register DbContext and DI
builder.Services.AddDbContext<Connection.Infrastructure.Data.ConnectionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<Connection.Application.Interfaces.IConnectionRepository, Connection.Infrastructure.Repositories.ConnectionRepository>();
builder.Services.AddScoped<Connection.Application.Interfaces.IConnectionService, Connection.Application.Services.ConnectionService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Connection.Infrastructure.Data.ConnectionDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHub<Connection.API.Hubs.ChatHub>("/hubs/chat");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
