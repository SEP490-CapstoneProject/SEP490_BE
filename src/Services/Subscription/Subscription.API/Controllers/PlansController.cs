using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Interfaces;

namespace Subscription.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<PlansController> _logger;

    public PlansController(ISubscriptionService subscriptionService, ILogger<PlansController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPlans()
    {
        var plans = await _subscriptionService.GetAllPlansAsync();
        return Ok(plans);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlanById(int id)
    {
        var plan = await _subscriptionService.GetPlanByIdAsync(id);
        if (plan == null)
            return NotFound(new { error = "Plan not found" });
        
        return Ok(plan);
    }
}
