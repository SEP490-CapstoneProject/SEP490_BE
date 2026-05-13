using Challenge.Application.Clients;
using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;

namespace Challenge.Application.Services;

public class GradingService : IGradingService
{
    public GradingService(
        IGeminiAIClient geminiClient,
        ISubmissionCriteriaScoreRepository criteriaScoreRepository,
        ILogger<GradingService> logger)
    {
    }

    public Task<(double overallScore, Dictionary<int, double> criteriaScores, string feedback)> GradeSubmissionAsync(
        ChallengeSubmission submission,
        ChallengeVersion version)
        => Task.FromResult((8.5d, new Dictionary<int, double>(), "Graded."));
}
