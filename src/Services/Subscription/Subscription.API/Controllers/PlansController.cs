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

    /// <summary>Lấy tất cả plans đang active.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPlans()
    {
        var plans = await _subscriptionService.GetAllPlansAsync();
        return Ok(plans);
    }

    /// <summary>
    /// Lấy danh sách plans theo role.
    /// Trả về plans có AllowedRole = role HOẶC AllowedRole = null (tất cả role đều dùng được).
    /// </summary>
    [HttpGet("by-role/{role}")]
    public async Task<IActionResult> GetPlansByRole(string role)
    {
        var plans = await _subscriptionService.GetPlansByRoleAsync(role);
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
