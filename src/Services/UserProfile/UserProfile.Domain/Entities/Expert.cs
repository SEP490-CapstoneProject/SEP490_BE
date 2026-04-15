using RecruitmentPlatform.Common;

namespace UserProfile.Domain.Entities;

public class Expert : BaseEntity
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
}
