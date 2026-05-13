namespace Challenge.Application.Clients;

/// <summary>
/// Client for Google Gemini AI service
/// </summary>
public interface IGeminiAIClient
{
    /// <summary>
    /// Analyze a challenge and extract skills, difficulty, and criteria
    /// </summary>
    Task<ChallengeAnalysisResult> AnalyzeChallengeAsync(string description, string expectedSolution);

    /// <summary>
    /// Grade a submission against criteria
    /// </summary>
    Task<SubmissionGradingResult> GradeSubmissionAsync(
        string challengeDescription,
        List<string> criteria,
        string userSubmission);
}

public class ChallengeAnalysisResult
{
    public double Difficulty { get; set; }
    public string DifficultyLabel { get; set; } = "Medium";
    public Dictionary<string, double> SkillWeights { get; set; } = new();
    public List<string> ExtractedCriteria { get; set; } = new();
    public string Analysis { get; set; } = "";
    public string ModelName { get; set; } = "Gemini 1.5 Pro";
    public string PromptVersion { get; set; } = "v1.0";
}

public class SubmissionGradingResult
{
    public double OverallScore { get; set; }
    public Dictionary<string, double> CriteriaScores { get; set; } = new();
    public string Feedback { get; set; } = "";
    public string ModelName { get; set; } = "Gemini 1.5 Pro";
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
}
