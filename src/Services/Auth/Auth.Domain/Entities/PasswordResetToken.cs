using RecruitmentPlatform.Common;

namespace Auth.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public User User { get; set; } = null!;
}
