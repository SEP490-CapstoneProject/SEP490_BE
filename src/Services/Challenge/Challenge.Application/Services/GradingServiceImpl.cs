using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;

namespace Challenge.Application.Services;

public class GradingService : IGradingService
{
    private readonly IGeminiAIService _geminiService;
    private readonly ISubmissionCriteriaScoreRepository _criteriaScoreRepository;
    private readonly ILogger<GradingService> _logger;

    public GradingService(
        IGeminiAIService geminiService,
        ISubmissionCriteriaScoreRepository criteriaScoreRepository,
        ILogger<GradingService> logger)
    {
        _geminiService = geminiService;
        _criteriaScoreRepository = criteriaScoreRepository;
        _logger = logger;
    }

    public async Task<(double overallScore, Dictionary<int, double> criteriaScores, string feedback)> GradeSubmissionAsync(
        ChallengeSubmission submission,
        ChallengeVersion version)
    {
        var aiResult = await _geminiService.GradeSubmissionAsync(version, submission.SubmissionContent);

        var criteriaScores = aiResult.CriteriaScores
            .Select((pair, index) => new { index, pair.Value.Score })
            .ToDictionary(item => item.index + 1, item => (double)item.Score);

        _logger.LogInformation(
            "Graded submission {SubmissionId} using model {Model} with score {Score}",
            submission.Id,
            aiResult.ModelName,
            aiResult.OverallScore);

        return ((double)aiResult.OverallScore, criteriaScores, aiResult.Feedback);
    }
}
