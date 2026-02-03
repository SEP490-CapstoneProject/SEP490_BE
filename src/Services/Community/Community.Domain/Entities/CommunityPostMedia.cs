using RecruitmentPlatform.Common;

namespace Community.Domain.Entities;

public class CommunityPostMedia : BaseEntity
{
    public int CommunityPostId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
    // Navigation property
    public CommunityPost CommunityPost { get; set; } = null!;
}
