namespace Challenge.Application.DTOs;

/// <summary>
/// Submission data with user profile info (for creator view)
/// </summary>
public class SubmissionWithUserDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    
    // User info from UserProfile service
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserAvatar { get; set; } = string.Empty;
    
    // Submission details
    public string SubmissionStatus { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string SubmissionContent { get; set; } = string.Empty;
    public string GitHubLink { get; set; } = string.Empty;
    
    // Evaluation details
    public decimal? EvaluationScore { get; set; }
    public string EvaluationStatus { get; set; } = string.Empty;
    public DateTime? EvaluatedAt { get; set; }
    public string Feedback { get; set; } = string.Empty;
    
    public int AttemptCount { get; set; }
}

/// <summary>
/// Submission data for participant view (own submissions only)
/// </summary>
public class ParticipantSubmissionDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    
    // Submission details
    public string SubmissionStatus { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string SubmissionContent { get; set; } = string.Empty;
    public string GitHubLink { get; set; } = string.Empty;
    
    // Evaluation details
    public decimal? EvaluationScore { get; set; }
    public string EvaluationStatus { get; set; } = string.Empty;
    public DateTime? EvaluatedAt { get; set; }
    public string Feedback { get; set; } = string.Empty;
    
    public int AttemptCount { get; set; }
}

/// <summary>
/// Paginated response for creator submissions list
/// </summary>
public class SubmissionListResponseDto
{
    public List<SubmissionWithUserDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

/// <summary>
/// Paginated response for participant submissions list
/// </summary>
public class ParticipantSubmissionListResponseDto
{
    public List<ParticipantSubmissionDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

/// <summary>
/// Challenge data for participant view (grouped by submitted challenge)
/// </summary>
public class ParticipantSubmittedChallengeDto
{
    public Guid ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public string ChallengeDescription { get; set; } = string.Empty;
    public DateTime ChallengeDeadline { get; set; }
    public DateTime? PublishedAt { get; set; }

    public Guid LatestSubmissionId { get; set; }
    public string LatestSubmissionStatus { get; set; } = string.Empty;
    public DateTime LatestSubmittedAt { get; set; }
    public decimal? LatestEvaluationScore { get; set; }
    public string LatestEvaluationStatus { get; set; } = string.Empty;
    public DateTime? LatestEvaluatedAt { get; set; }
    public string LatestFeedback { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
}

/// <summary>
/// Paginated response for participant submitted challenges list
/// </summary>
public class ParticipantSubmittedChallengeListResponseDto
{
    public List<ParticipantSubmittedChallengeDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
