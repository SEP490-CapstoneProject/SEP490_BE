using RecruitmentPlatform.Common;

namespace Community.Domain.Entities;

public class ReplyComment : BaseEntity
{
    public int CommentId { get; set; }
    public int UserId { get; set; }
    public int? ReplyToUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    
    // Navigation property
    public Comment Comment { get; set; } = null!;
}
