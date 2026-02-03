using Microsoft.AspNetCore.Mvc;
using Community.Application.Interfaces;
using Community.Domain.Entities;

namespace Community.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _service;

    public CommunityController(ICommunityService service)
    {
        _service = service;
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

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CommunityPost post)
    {
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
