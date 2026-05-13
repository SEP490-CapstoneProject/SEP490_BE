namespace Challenge.Application.DTOs;

public class ModerateChallengeDto
{
    public string Action { get; set; } // approve, reject
    public string RejectionReason { get; set; } // optional, for reject
}
