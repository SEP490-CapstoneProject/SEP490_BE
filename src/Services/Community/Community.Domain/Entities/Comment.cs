using RecruitmentPlatform.Common;

namespace Community.Domain.Entities;

public class Comment : BaseEntity
{
    public int CommunityPostId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    
    // Navigation properties
    public CommunityPost CommunityPost { get; set; } = null!;
    public ICollection<ReplyComment> Replies { get; set; } = new List<ReplyComment>();
}
