using RecruitmentPlatform.Common;

namespace Interview.Domain.Entities;

public class Interview : BaseEntity
{
    public int ApplicationId { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public string PostPosition { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int Round { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InterviewerName { get; set; } = string.Empty;
}
