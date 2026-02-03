using RecruitmentPlatform.Common;

namespace Community.Domain.Entities;

public class CommunityPostSave : BaseEntity
{
    public int CommunityPostId { get; set; }
    public int UserId { get; set; }
    
    // Navigation property
    public CommunityPost CommunityPost { get; set; } = null!;
}
