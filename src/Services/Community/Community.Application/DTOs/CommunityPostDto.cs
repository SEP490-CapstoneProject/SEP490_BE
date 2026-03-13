namespace Community.Application.DTOs;

public class AuthorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Role { get; set; } = "USER"; // "USER" | "COMPANY"
}

public class PortfolioPreviewDto
{
    public string Type { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class CommunityPostDto
{
    public int Id { get; set; }
    public AuthorDto Author { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> Media { get; set; } = new();
    public int? PortfolioId { get; set; }
    public PortfolioPreviewDto? PortfolioPreview { get; set; }
    public int FavoriteCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsFavorited { get; set; }
    public bool IsSaved { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class CursorPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

public class CreatePostRequest
{
    public string Description { get; set; } = string.Empty;
    public int? PortfolioId { get; set; }
    public int Status { get; set; } = 1;
    public string? CoverImageKey { get; set; }
}

public class UpdatePostRequest
{
    public string? Description { get; set; }
    public int? PortfolioId { get; set; }
    public int? Status { get; set; }
}

public class AddCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class AddReplyRequest
{
    public string Content { get; set; } = string.Empty;
    public int? ReplyToUserId { get; set; }
}

public class FeedCountsResult
{
    public Dictionary<int, int> CommentCounts { get; set; } = new();
    public HashSet<int> FavoritedPostIds { get; set; } = new();
    public HashSet<int> SavedPostIds { get; set; } = new();
}
