var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7000;
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        
        // Add Swagger endpoints for each microservice
        c.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "Auth Service");
        // Uncomment as services are implemented:
        // c.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "UserProfile Service");
        // c.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Portfolio Service");
        // c.SwaggerEndpoint("http://localhost:5004/swagger/v1/swagger.json", "Company Service");
        // c.SwaggerEndpoint("http://localhost:5005/swagger/v1/swagger.json", "JobHiring Service");
        // c.SwaggerEndpoint("http://localhost:5006/swagger/v1/swagger.json", "Connection Service");
        // c.SwaggerEndpoint("http://localhost:5007/swagger/v1/swagger.json", "Community Service");
        // c.SwaggerEndpoint("http://localhost:5008/swagger/v1/swagger.json", "Subscription Service");
        // c.SwaggerEndpoint("http://localhost:5009/swagger/v1/swagger.json", "Advertisement Service");
        // c.SwaggerEndpoint("http://localhost:5010/swagger/v1/swagger.json", "Moderation Service");
        // c.SwaggerEndpoint("http://localhost:5011/swagger/v1/swagger.json", "Notification Service");
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();
