namespace Interview.Application.DTOs;

public class CandidateDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
}

public class PostDto
{
    public int PostId { get; set; }
    public string Position { get; set; } = string.Empty;
}

public class InterviewItemDto
{
    public int InterviewId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;

    public CandidateDto Candidate { get; set; } = new();
    public PostDto Post { get; set; } = new();

    public int Round { get; set; }
    public string Type { get; set; } = string.Empty;

    public string? Platform { get; set; }
    public string? Link { get; set; }
    public string? Building { get; set; }
    public string? Room { get; set; }

    public string Status { get; set; } = string.Empty;
    public string InterviewerName { get; set; } = string.Empty;
}
