using Challenge.Application.Clients;
using Challenge.Application.Services;
using Challenge.Domain.Entities;

namespace Challenge.Application.Services.AI;

public class GeminiAIService : IGeminiAIService
{
    private readonly IGeminiAIClient _geminiClient;
    private readonly IPromptSanitizationService _promptSanitization;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(
        IGeminiAIClient geminiClient,
        IPromptSanitizationService promptSanitization,
        ILogger<GeminiAIService> logger)
    {
        _geminiClient = geminiClient;
        _promptSanitization = promptSanitization;
        _logger = logger;
    }

    public async Task<ChallengeAnalysisResult> AnalyzeChallengeAsync(
        string title,
        string description,
        string expectedSolution,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI challenge analysis for '{Title}'", title);

        var sanitizedDescription = await _promptSanitization.SanitizePromptAsync(description, Guid.Empty, cancellationToken);
        var sanitizedExpectedSolution = await _promptSanitization.SanitizePromptAsync(expectedSolution, Guid.Empty, cancellationToken);

        var clientResult = await _geminiClient.AnalyzeChallengeAsync(
            sanitizedDescription,
            sanitizedExpectedSolution);

        return new ChallengeAnalysisResult
        {
            DifficultyScore = (decimal)clientResult.Difficulty,
            DifficultyLabel = clientResult.DifficultyLabel,
            SkillWeights = clientResult.SkillWeights.ToDictionary(pair => pair.Key, pair => (decimal)pair.Value),
            EvaluationCriteria = clientResult.ExtractedCriteria ?? new List<string>()
        };
    }

    public async Task<SubmissionGradingResult> GradeSubmissionAsync(
        ChallengeVersion version,
        string submission,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI grading for version {VersionId}", version.Id);

        var sanitizedSubmission = await _promptSanitization.SanitizePromptAsync(submission, version.Id, cancellationToken);
        var criteria = ExtractCriteria(version);
        var clientResult = await _geminiClient.GradeSubmissionAsync(
            BuildChallengeDescription(version),
            criteria,
            sanitizedSubmission);

        return new SubmissionGradingResult
        {
            OverallScore = (decimal)clientResult.OverallScore,
            Feedback = clientResult.Feedback,
            CriteriaScores = clientResult.CriteriaScores.ToDictionary(
                pair => pair.Key,
                pair => new CriteriaScore
                {
                    Score = (decimal)pair.Value,
                    Feedback = clientResult.Feedback
                }),
            ModelName = clientResult.ModelName,
            GradedAt = clientResult.GradedAt
        };
    }

    private static List<string> ExtractCriteria(ChallengeVersion version)
    {
        if (string.IsNullOrWhiteSpace(version.SkillWeightMapping))
        {
            return new List<string> { "Correctness", "Code quality", "Error handling" };
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(version.SkillWeightMapping);
            return doc.RootElement
                .EnumerateObject()
                .Select(prop => prop.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<string> { "Correctness", "Code quality", "Error handling" };
        }
    }

    private static string BuildChallengeDescription(ChallengeVersion version)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            new[]
            {
                $"Title: {version.Title}",
                $"Description: {version.Description}",
                $"Expected Solution: {version.ExpectedSolution}"
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
