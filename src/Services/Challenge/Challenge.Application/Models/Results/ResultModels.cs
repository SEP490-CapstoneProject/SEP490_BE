namespace Challenge.Application.Models.Results;

/// <summary>
/// Result models for service operations
/// </summary>

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, object>? Data { get; set; }

    public static OperationResult Ok(string message = "Operation successful")
        => new() { Success = true, Message = message };

    public static OperationResult Fail(string message)
        => new() { Success = false, Message = message };

    public static OperationResult Ok(string message, Dictionary<string, object> data)
        => new() { Success = true, Message = message, Data = data };
}

public class OperationResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    public static OperationResult<T> Ok(T data, string message = "Operation successful")
        => new() { Success = true, Message = message, Data = data };

    public static OperationResult<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public class GradingResult
{
    public double OverallScore { get; set; }
    public Dictionary<int, double> CriteriaScores { get; set; } = new();
    public Dictionary<int, double> SkillPoints { get; set; } = new();
    public string Feedback { get; set; } = "";
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
}

public class SkillVerificationResult
{
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public double TotalPoints { get; set; }
    public string VerificationLevel { get; set; } = "";
    public int ChallengeCount { get; set; }
    public DateTime LastVerifiedAt { get; set; } = DateTime.UtcNow;
}
