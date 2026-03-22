namespace Community.Application.DTOs;

public class CommentUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
}

public class ReplyCommentDto
{
    public int Id { get; set; }
    public CommentUserDto Author { get; set; } = null!;
    public CommentUserDto? ReplyToUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class PostCommentDto
{
    public int Id { get; set; }
    public CommentUserDto Author { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public List<ReplyCommentDto> Replies { get; set; } = new();
}

public class PostCommentsResponseDto
{
    public int PostId { get; set; }
    public List<PostCommentDto> Comments { get; set; } = new();
}
