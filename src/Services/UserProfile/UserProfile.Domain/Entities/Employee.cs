using RecruitmentPlatform.Common;

namespace UserProfile.Domain.Entities;

public class Employee : BaseEntity
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; } = 1; // 1 = Active, 0 = Inactive
}
