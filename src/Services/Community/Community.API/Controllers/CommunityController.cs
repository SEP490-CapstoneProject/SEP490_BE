using Microsoft.AspNetCore.Mvc;
using Community.Application.Interfaces;
using Community.Domain.Entities;

namespace Community.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        ICommunityService service, 
        IHttpClientFactory httpClientFactory,
        ILogger<CommunityController> logger)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // POST OPERATIONS
    
    [HttpGet("posts")]
    public async Task<IActionResult> GetAllPosts()
    {
        var posts = await _service.GetAllPostsAsync();
        return Ok(posts);
    }

    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetPostById(int id)
    {
        var post = await _service.GetPostByIdAsync(id);
        if (post == null) return NotFound();
        return Ok(post);
    }

    [HttpGet("posts/user/{userId}")]
    public async Task<IActionResult> GetPostsByUser(int userId)
    {
        var posts = await _service.GetPostsByUserIdAsync(userId);
        return Ok(posts);
    }

    /// <summary>
    /// Create a post with optional image/video upload
    /// </summary>
    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromForm] int userId, [FromForm] string description, 
        [FromForm] int? portfolioId, [FromForm] int status, [FromForm] IFormFile? coverMedia)
    {
        string? mediaUrl = null;

        // Upload cover media to Media Service if provided
        if (coverMedia != null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MediaService");
                var formData = new MultipartFormDataContent();
                formData.Add(new StreamContent(coverMedia.OpenReadStream()), "file", coverMedia.FileName);
                formData.Add(new StringContent("community/posts"), "folder");

                var endpoint = coverMedia.ContentType.StartsWith("video/") 
                    ? "/api/upload/video" 
                    : "/api/upload/image";

                var response = await client.PostAsync(endpoint, formData);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<MediaUploadResponse>();
                    mediaUrl = result?.Url;
                }
                else
                {
                    _logger.LogWarning("Media upload failed: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading media to Media Service");
                return StatusCode(500, new { error = "Failed to upload media" });
            }
        }

        var post = new CommunityPost
        {
            UserId = userId,
            Description = description,
            CoverImageVideo = mediaUrl ?? string.Empty,
            PortfolioId = portfolioId,
            Status = status
        };

        var created = await _service.CreatePostAsync(post);
        return CreatedAtAction(nameof(GetPostById), new { id = created.Id }, created);
    }

    [HttpPut("posts/{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] CommunityPost post)
    {
        if (id != post.Id) return BadRequest();
        await _service.UpdatePostAsync(post);
        return NoContent();
    }

    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        await _service.DeletePostAsync(id);
        return NoContent();
    }

    // SAVE/FAVORITE OPERATIONS

    [HttpPost("posts/{postId}/save")]
    public async Task<IActionResult> SavePost(int postId, [FromQuery] int userId)
    {
        var result = await _service.SavePostAsync(postId, userId);
        if (!result) return BadRequest("Post already saved");
        return Ok();
    }

    [HttpDelete("posts/{postId}/save")]
    public async Task<IActionResult> UnsavePost(int postId, [FromQuery] int userId)
    {
        var result = await _service.UnsavePostAsync(postId, userId);
        if (!result) return NotFound("Save not found");
        return NoContent();
    }

    [HttpPost("posts/{postId}/favorite")]
    public async Task<IActionResult> FavoritePost(int postId, [FromQuery] int userId)
    {
        var result = await _service.FavoritePostAsync(postId, userId);
        if (!result) return BadRequest("Post already favorited");
        return Ok();
    }

    [HttpDelete("posts/{postId}/favorite")]
    public async Task<IActionResult> UnfavoritePost(int postId, [FromQuery] int userId)
    {
        var result = await _service.UnfavoritePostAsync(postId, userId);
        if (!result) return NotFound("Favorite not found");
        return NoContent();
    }

    [HttpGet("posts/saved/{userId}")]
    public async Task<IActionResult> GetSavedPosts(int userId)
    {
        var posts = await _service.GetSavedPostsAsync(userId);
        return Ok(posts);
    }

    [HttpGet("posts/favorited/{userId}")]
    public async Task<IActionResult> GetFavoritedPosts(int userId)
    {
        var posts = await _service.GetFavoritedPostsAsync(userId);
        return Ok(posts);
    }

    // COMMENT OPERATIONS

    [HttpGet("posts/{postId}/comments")]
    public async Task<IActionResult> GetComments(int postId)
    {
        var comments = await _service.GetCommentsAsync(postId);
        return Ok(comments);
    }

    [HttpPost("posts/{postId}/comments")]
    public async Task<IActionResult> AddComment(int postId, [FromQuery] int userId, [FromBody] string content)
    {
        var comment = await _service.AddCommentAsync(postId, userId, content);
        return CreatedAtAction(nameof(GetComments), new { postId }, comment);
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        await _service.DeleteCommentAsync(commentId);
        return NoContent();
    }

    // REPLY OPERATIONS

    [HttpGet("comments/{commentId}/replies")]
    public async Task<IActionResult> GetReplies(int commentId)
    {
        var replies = await _service.GetRepliesAsync(commentId);
        return Ok(replies);
    }

    [HttpPost("comments/{commentId}/replies")]
    public async Task<IActionResult> AddReply(int commentId, [FromQuery] int userId, [FromQuery] int? replyToUserId, [FromBody] string content)
    {
        var reply = await _service.AddReplyAsync(commentId, userId, replyToUserId, content);
        return CreatedAtAction(nameof(GetReplies), new { commentId }, reply);
    }

    [HttpDelete("replies/{replyId}")]
    public async Task<IActionResult> DeleteReply(int replyId)
    {
        await _service.DeleteReplyAsync(replyId);
        return NoContent();
    }
}

// DTO for Media Service response
public class MediaUploadResponse
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? PublicId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
