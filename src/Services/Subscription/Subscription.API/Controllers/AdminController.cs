using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.DTOs.Admin;
using Subscription.Application.Interfaces;
using Subscription.Infrastructure.Services;

namespace Subscription.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly IAdminSubscriptionService _adminService;
    private readonly AnalyticsService _analyticsService;

    public AdminController(
        ILogger<AdminController> logger,
        IAdminSubscriptionService adminService,
        AnalyticsService analyticsService)
    {
        _logger = logger;
        _adminService = adminService;
        _analyticsService = analyticsService;
    }

    // Plan Management endpoints will be added here
    // Subscription Management endpoints will be added here
    
    #region Plan Management

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        try
        {
            var result = await _adminService.CreatePlanAsync(request);
            return CreatedAtAction(nameof(GetPlanById), new { planId = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("plans/{planId}")]
    public async Task<IActionResult> GetPlanById(int planId)
    {
        try
        {
            // Use subscription service to get plan details
            return Ok(new { planId, message = "Use /api/subscriptions/plans endpoint for plan details" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("plans/{planId}")]
    public async Task<IActionResult> UpdatePlan(int planId, [FromBody] UpdatePlanRequest request)
    {
        try
        {
            var result = await _adminService.UpdatePlanAsync(planId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("plans/{planId}")]
    public async Task<IActionResult> DeletePlan(int planId)
    {
        try
        {
            await _adminService.DeletePlanAsync(planId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("plans/{planId}/toggle-active")]
    public async Task<IActionResult> TogglePlanActive(int planId)
    {
        try
        {
            var result = await _adminService.TogglePlanActiveAsync(planId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling plan {PlanId} active status", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Plan Features

    [HttpGet("plans/{planId}/features")]
    public async Task<IActionResult> GetPlanFeatures(int planId)
    {
        try
        {
            var result = await _adminService.GetPlanFeaturesAsync(planId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting features for plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("plans/{planId}/features")]
    public async Task<IActionResult> AddPlanFeature(int planId, [FromBody] CreatePlanFeatureRequest request)
    {
        try
        {
            var result = await _adminService.AddPlanFeatureAsync(planId, request);
            return CreatedAtAction(nameof(AddPlanFeature), new { planId, featureId = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feature to plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("plans/{planId}/features/{featureId}")]
    public async Task<IActionResult> UpdatePlanFeature(int planId, int featureId, [FromBody] UpdatePlanFeatureRequest request)
    {
        try
        {
            var result = await _adminService.UpdatePlanFeatureAsync(planId, featureId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature {FeatureId} in plan {PlanId}", featureId, planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("plans/{planId}/features/{featureId}")]
    public async Task<IActionResult> DeletePlanFeature(int planId, int featureId)
    {
        try
        {
            await _adminService.DeletePlanFeatureAsync(planId, featureId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature {FeatureId} from plan {PlanId}", featureId, planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion

    #region Subscription Management

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetAllSubscriptions([FromQuery] SubscriptionFilter filter)
    {
        try
        {
            var result = await _adminService.GetAllSubscriptionsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("subscriptions/{subscriptionId}")]
    public async Task<IActionResult> GetSubscriptionById(int subscriptionId)
    {
        try
        {
            var result = await _adminService.GetSubscriptionByIdAsync(subscriptionId);
            if (result == null)
                return NotFound(new { error = $"Subscription {subscriptionId} not found" });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("users/{userId}/subscriptions")]
    public async Task<IActionResult> GetUserSubscriptionHistory(int userId)
    {
        try
        {
            var result = await _adminService.GetUserSubscriptionHistoryAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("subscriptions/{subscriptionId}/cancel")]
    public async Task<IActionResult> CancelSubscription(int subscriptionId, [FromBody] AdminCancelRequest request)
    {
        try
        {
            await _adminService.CancelSubscriptionAsync(subscriptionId, request);
            return Ok(new { message = "Subscription cancelled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("subscriptions/{subscriptionId}/extend")]
    public async Task<IActionResult> ExtendSubscription(int subscriptionId, [FromBody] ExtendRequest request)
    {
        try
        {
            await _adminService.ExtendSubscriptionAsync(subscriptionId, request);
            return Ok(new { message = $"Subscription extended by {request.Months} months" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("subscriptions/{subscriptionId}/refund")]
    public async Task<IActionResult> IssueRefund(int subscriptionId, [FromBody] RefundRequest request)
    {
        try
        {
            await _adminService.IssueRefundAsync(subscriptionId, request);
            return Ok(new { message = "Refund issued successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing refund for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #endregion
    
    // Analytics Endpoints

    [HttpGet("analytics/overview")]
    public async Task<IActionResult> GetAnalyticsOverview([FromQuery] DateRangeFilter? filter)
    {
        try
        {
            filter ??= new DateRangeFilter();
            var result = await _analyticsService.GetAnalyticsOverviewAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics overview");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("analytics/revenue")]
    public async Task<IActionResult> GetRevenueAnalytics([FromQuery] DateRangeFilter? filter)
    {
        try
        {
            filter ??= new DateRangeFilter();
            var result = await _analyticsService.GetRevenueAnalyticsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("analytics/subscriptions-by-plan")]
    public async Task<IActionResult> GetSubscriptionsByPlan()
    {
        try
        {
            var result = await _analyticsService.GetSubscriptionsByPlanAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions by plan");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("analytics/churn")]
    public async Task<IActionResult> GetChurnRate([FromQuery] DateRangeFilter? filter)
    {
        try
        {
            filter ??= new DateRangeFilter();
            var result = await _analyticsService.GetChurnRateAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting churn rate");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", controller = "admin" });
    }
}
