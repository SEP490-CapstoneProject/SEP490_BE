using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

/// <summary>
/// Portfolio Preview API Endpoints
/// Generates AI-powered portfolio previews for quick recruiter scanning
/// </summary>
[ApiController]
[Route("api/portfolios/{portfolioId}/preview")]
[Authorize]
public class PortfolioPreviewController : ControllerBase
{
    private readonly IPortfolioPreviewService _previewService;
    private readonly ILogger<PortfolioPreviewController> _logger;

    public PortfolioPreviewController(
        IPortfolioPreviewService previewService,
        ILogger<PortfolioPreviewController> logger)
    {
        _previewService = previewService;
        _logger = logger;
    }

    /// <summary>
    /// Generate or regenerate portfolio preview
    /// Portfolio owner can generate a preview by providing portfolio ID and optional highlights
    /// </summary>
    /// <param name="portfolioId">Portfolio ID</param>
    /// <param name="request">Generate request with optional highlights description</param>
    /// <returns>Generated preview with JSON structure</returns>
    /// <response code="200">Preview generated successfully</response>
    /// <response code="400">Invalid request or portfolio not approved</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Portfolio not found</response>
    /// <response code="500">Server error during generation</response>
    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePreview(
        int portfolioId,
        [FromBody] GeneratePortfolioPreviewRequest? request)
    {
        try
        {
            _logger.LogInformation("Generating preview for portfolio {PortfolioId}", portfolioId);

            var highlightsDescription = request?.HighlightsDescription;

            // Call service to generate preview
            var result = await _previewService.GeneratePreviewAsync(portfolioId, highlightsDescription);

            if (result == null || !result.Success)
            {
                _logger.LogWarning("Failed to generate preview: {Message}", result?.Message);
                return BadRequest(new
                {
                    success = false,
                    message = result?.Message ?? "Failed to generate preview"
                });
            }

            _logger.LogInformation("✅ Preview generated successfully for portfolio {PortfolioId}", portfolioId);
            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Preview
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while generating the preview",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get portfolio preview
    /// Recruiters can view preview if portfolio is public
    /// Portfolio owner/admin can always view their preview
    /// </summary>
    /// <param name="portfolioId">Portfolio ID</param>
    /// <returns>Current portfolio preview</returns>
    /// <response code="200">Preview found and returned</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - portfolio is private</response>
    /// <response code="404">Portfolio or preview not found</response>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPreview(int portfolioId)
    {
        try
        {
            _logger.LogInformation("Retrieving preview for portfolio {PortfolioId}", portfolioId);

            var preview = await _previewService.GetPreviewAsync(portfolioId);

            if (preview == null)
            {
                _logger.LogWarning("Preview not found for portfolio {PortfolioId}", portfolioId);
                return NotFound(new
                {
                    success = false,
                    message = "Preview not found for this portfolio"
                });
            }

            _logger.LogInformation("✅ Preview retrieved successfully for portfolio {PortfolioId}", portfolioId);
            return Ok(new
            {
                success = true,
                data = preview
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving preview for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving the preview",
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request DTO for generating portfolio preview
/// </summary>
public class GeneratePortfolioPreviewRequest
{
    /// <summary>
    /// Optional description of highlights to emphasize in the preview
    /// If not provided, defaults to portfolio name/description
    /// </summary>
    public string? HighlightsDescription { get; set; }
}
