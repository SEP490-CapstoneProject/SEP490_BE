using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/follow-categories")]
[Authorize(Roles = "RECRUITER")]
public class FollowCategoriesController : ControllerBase
{
    private readonly IPortfolioFollowCategoryService _categoryService;
    private readonly ILogger<FollowCategoriesController> _logger;

    public FollowCategoriesController(
        IPortfolioFollowCategoryService categoryService,
        ILogger<FollowCategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFollowCategoryRequest request)
    {
        try
        {
            var result = await _categoryService.CreateAsync(request);
            return StatusCode(201, result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating follow category");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCategories()
    {
        try
        {
            var result = await _categoryService.GetMyCategoriesAsync();
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting follow categories");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{categoryId:int}")]
    public async Task<IActionResult> Update(int categoryId, [FromBody] UpdateFollowCategoryRequest request)
    {
        try
        {
            var result = await _categoryService.UpdateAsync(categoryId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating follow category {CategoryId}", categoryId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{categoryId:int}")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        try
        {
            await _categoryService.DeleteAsync(categoryId);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting follow category {CategoryId}", categoryId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
