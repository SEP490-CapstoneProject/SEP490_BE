var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SkillSnap API Gateway", Version = "v1" });
});

// Add CORS
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

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "api-gateway" }));

// Service discovery endpoint
app.MapGet("/services", () => Results.Ok(new
{
    services = new[]
    {
        new { name = "auth", port = 5001, path = "/api/auth" },
        new { name = "userprofile", port = 5002, path = "/api/userprofile" },
        new { name = "portfolio", port = 5003, path = "/api/portfolio" },
        new { name = "company", port = 5004, path = "/api/company" },
        new { name = "connection", port = 5006, path = "/api/connection" },
        new { name = "community", port = 5007, path = "/api/community" },
        new { name = "subscription", port = 5008, path = "/api/subscription" },
        new { name = "notification", port = 5011, path = "/api/notifications" },
        new { name = "media", port = 5012, path = "/api/media" },
        new { name = "application", port = 5013, path = "/api/applications" }
    }
}));

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();
