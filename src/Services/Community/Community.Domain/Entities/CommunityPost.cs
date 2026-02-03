using RecruitmentPlatform.Common;

namespace Community.Domain.Entities;

public class CommunityPost : BaseEntity
{
    public int UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CoverImageVideo { get; set; } = string.Empty;
    public int? PortfolioId { get; set; }
    public int FavoriteCount { get; set; } = 0;
    public int Status { get; set; }
    
    // Navigation properties
    public ICollection<CommunityPostSave> Saves { get; set; } = new List<CommunityPostSave>();
    public ICollection<CommunityPostFavorite> Favorites { get; set; } = new List<CommunityPostFavorite>();
    public ICollection<CommunityPostMedia> Media { get; set; } = new List<CommunityPostMedia>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
