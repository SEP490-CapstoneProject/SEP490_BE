using Challenge.Application.Clients;

namespace Challenge.Infrastructure.Clients;

public class GeminiAIClient : IGeminiAIClient
{
    private readonly ILogger<GeminiAIClient> _logger;

    public GeminiAIClient(
        HttpClient httpClient,
        ILogger<GeminiAIClient> logger,
        IConfiguration configuration)
    {
        _logger = logger;
    }

    public Task<ChallengeAnalysisResult> AnalyzeChallengeAsync(string description, string expectedSolution)
    {
        _logger.LogInformation("AnalyzeChallengeAsync called");
        return Task.FromResult(new ChallengeAnalysisResult
        {
            Difficulty = 5,
            DifficultyLabel = "Medium",
            SkillWeights = new Dictionary<string, double> { { "General", 1 } },
            ExtractedCriteria = new List<string> { "Functionality", "Code Quality" },
            Analysis = "Fallback analysis"
        });
    }

    public Task<SubmissionGradingResult> GradeSubmissionAsync(
        string challengeDescription,
        List<string> criteria,
        string userSubmission)
    {
        _logger.LogInformation("GradeSubmissionAsync called");
        return Task.FromResult(new SubmissionGradingResult
        {
            OverallScore = 5,
            CriteriaScores = new Dictionary<string, double> { { "Quality", 5 } },
            Feedback = "Good attempt. Consider the following improvements..."
        });
    }
}
