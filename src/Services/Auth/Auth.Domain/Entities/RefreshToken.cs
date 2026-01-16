using RecruitmentPlatform.Common;

namespace Auth.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
    public bool Revoked { get; set; } = false;
    
    public User User { get; set; } = null!;
}
