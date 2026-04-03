using ApiGateway.Azure;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for long-lived WebSocket connections
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

// Add Azure Key Vault configuration
builder.Configuration.AddAzureKeyVault();

// Override reverse proxy cluster addresses from ServiceUrls if provided
var serviceUrls = builder.Configuration.GetSection("ServiceUrls")
    .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
void SetCluster(string clusterId, string serviceKey)
{
    if (serviceUrls.TryGetValue(serviceKey, out var url) && !string.IsNullOrWhiteSpace(url))
    {
        overrides[$"ReverseProxy:Clusters:{clusterId}:Destinations:destination1:Address"] = url;
    }
}

SetCluster("auth-cluster", "AuthService");
SetCluster("userprofile-cluster", "UserProfileService");
SetCluster("portfolio-cluster", "PortfolioService");
SetCluster("company-cluster", "CompanyService");
SetCluster("connection-cluster", "ConnectionService");
SetCluster("community-cluster", "CommunityService");
SetCluster("subscription-cluster", "SubscriptionService");
SetCluster("notification-cluster", "NotificationService");
SetCluster("realtime-cluster", "RealtimeService");
SetCluster("media-cluster", "MediaService");
SetCluster("application-cluster", "ApplicationService");
SetCluster("payment-cluster", "PaymentService");

if (overrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(overrides);
}

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
        new { name = "company", port = 5004, path = "/api/company-posts" },
        new { name = "connection", port = 5006, path = "/api/connection" },
        new { name = "community", port = 5007, path = "/api/community" },
        new { name = "subscription", port = 5008, path = "/api/subscription" },
        new { name = "notification", port = 5011, path = "/api/notifications" },
        new { name = "realtime", port = 5015, path = "/hubs/realtime" },
        new { name = "media", port = 5012, path = "/api/media" },
        new { name = "application", port = 5013, path = "/api/applications" }
    }
}));

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();
