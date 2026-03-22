using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Subscription.Application.Interfaces;

namespace Subscription.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IRedisService _redisService;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IRedisService redisService,
        IRabbitMQPublisher rabbitMQPublisher,
        ILogger<HealthController> logger)
    {
        _redisService = redisService;
        _rabbitMQPublisher = rabbitMQPublisher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var redisHealthy = await _redisService.PingAsync();
        var rabbitHealthy = await _rabbitMQPublisher.IsHealthyAsync();

        var status = redisHealthy && rabbitHealthy ? "healthy" : "degraded";

        return Ok(new
        {
            status,
            timestamp = DateTime.UtcNow,
            services = new
            {
                redis = redisHealthy ? "healthy" : "unhealthy",
                rabbitmq = rabbitHealthy ? "healthy" : "unhealthy"
            }
        });
    }

    [HttpGet("redis")]
    public async Task<IActionResult> GetRedisHealth()
    {
        try
        {
            var healthy = await _redisService.PingAsync();
            return healthy 
                ? Ok(new { status = "healthy", timestamp = DateTime.UtcNow })
                : StatusCode(503, new { status = "unhealthy", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpGet("rabbitmq")]
    public async Task<IActionResult> GetRabbitMQHealth()
    {
        try
        {
            var healthy = await _rabbitMQPublisher.IsHealthyAsync();
            return healthy 
                ? Ok(new { status = "healthy", timestamp = DateTime.UtcNow })
                : StatusCode(503, new { status = "unhealthy", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }
}
