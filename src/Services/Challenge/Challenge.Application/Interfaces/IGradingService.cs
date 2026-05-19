using Challenge.Domain.Entities;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Internal service for grading submissions using AI
/// </summary>
public interface IGradingService
{
    Task<(double overallScore, Dictionary<string, double> criteriaScores, string feedback)> GradeSubmissionAsync(
        ChallengeSubmission submission,
        ChallengeVersion version);
}
