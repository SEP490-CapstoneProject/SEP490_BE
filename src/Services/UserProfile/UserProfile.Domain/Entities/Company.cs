using RecruitmentPlatform.Common;

namespace UserProfile.Domain.Entities;

public class Company : BaseEntity
{
    public int UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ActivityField { get; set; }
    public string? CoverImage { get; set; }
    public string? Avatar { get; set; }
    public int? TaxIdentification { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}
