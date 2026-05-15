using Challenge.Domain.Entities;
namespace Challenge.Application.Services;


public interface IGeminiAIService
{
    /// <summary>
    /// Analyze challenge to extract difficulty, skills, and criteria
    /// </summary>
    Task<ChallengeAnalysisResult> AnalyzeChallengeAsync(
        string title,
        string description,
        string expectedSolution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grade a submission against challenge criteria
    /// </summary>
    Task<SubmissionGradingResult> GradeSubmissionAsync(
        ChallengeVersion version,
        string submission,
        CancellationToken cancellationToken = default);
}

public class ChallengeAnalysisResult
{
    public decimal DifficultyScore { get; set; } // 0-10
    public string DifficultyLabel { get; set; } // Beginner, Intermediate, Hard, Expert
    public Dictionary<string, decimal> SkillWeights { get; set; } // skill name -> weight
    public List<string> EvaluationCriteria { get; set; }
}

public class SubmissionGradingResult
{
    public decimal OverallScore { get; set; } // 0-10
    public Dictionary<string, CriteriaScore> CriteriaScores { get; set; } // criteria name -> score
    public string Feedback { get; set; }
    public string ModelName { get; set; }
    public DateTime GradedAt { get; set; }
}

public class CriteriaScore
{
    public decimal Score { get; set; } // 0-10
    public string Feedback { get; set; }
}
