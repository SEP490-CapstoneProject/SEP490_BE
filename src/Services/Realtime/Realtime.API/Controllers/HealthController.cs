using Microsoft.AspNetCore.Mvc;

namespace Realtime.API.Controllers;

[ApiController]
[Route("api/realtime/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "realtime-service" });
    }
}
