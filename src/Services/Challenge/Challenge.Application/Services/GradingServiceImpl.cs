using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;

namespace Challenge.Application.Services;

public class GradingService : IGradingService
{
    private readonly IGeminiAIService _geminiService;
    private readonly ISubmissionCriteriaScoreRepository _criteriaScoreRepository;
    private readonly IChallengeCriteriaRepository _challengeCriteriaRepository;
    private readonly IEvaluationCriteriaRepository _evaluationCriteriaRepository;
    private readonly ILogger<GradingService> _logger;

    public GradingService(
        IGeminiAIService geminiService,
        ISubmissionCriteriaScoreRepository criteriaScoreRepository,
        IChallengeCriteriaRepository challengeCriteriaRepository,
        IEvaluationCriteriaRepository evaluationCriteriaRepository,
        ILogger<GradingService> logger)
    {
        _geminiService = geminiService;
        _criteriaScoreRepository = criteriaScoreRepository;
        _challengeCriteriaRepository = challengeCriteriaRepository;
        _evaluationCriteriaRepository = evaluationCriteriaRepository;
        _logger = logger;
    }

    public async Task<(double overallScore, Dictionary<string, double> criteriaScores, string feedback)> GradeSubmissionAsync(
        ChallengeSubmission submission,
        ChallengeVersion version)
    {
        var aiResult = await _geminiService.GradeSubmissionAsync(version, submission.SubmissionContent);

        var criteriaScores = aiResult.CriteriaScores
            .ToDictionary(pair => pair.Key, pair => (double)pair.Value.Score, StringComparer.OrdinalIgnoreCase);

        await PersistCriteriaScoresAsync(submission.Id, version.Id, criteriaScores);

        _logger.LogInformation(
            "Graded submission {SubmissionId} using model {Model} with score {Score}",
            submission.Id,
            aiResult.ModelName,
            aiResult.OverallScore);

        return ((double)aiResult.OverallScore, criteriaScores, aiResult.Feedback);
    }

    private async Task PersistCriteriaScoresAsync(
        Guid submissionId,
        Guid versionId,
        Dictionary<string, double> criteriaScores)
    {
        if (criteriaScores.Count == 0)
        {
            _logger.LogWarning("No criteriaScores were returned for submission {SubmissionId}", submissionId);
            return;
        }

        var versionCriteria = await _challengeCriteriaRepository.GetByVersionAsync(versionId);
        if (versionCriteria.Count == 0)
        {
            _logger.LogWarning("No challenge criteria found for version {VersionId}; skipping SUBMISSION_CRITERIA_SCORES", versionId);
            return;
        }

        var criteriaEntities = await _evaluationCriteriaRepository.GetByIdsAsync(versionCriteria.Select(x => x.CriteriaId));
        var nameToCriteriaId = criteriaEntities
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => NormalizeKey(x.Name), x => x.Id);

        var scoreEntities = new List<SubmissionCriteriaScore>();
        var usedCriteriaIds = new HashSet<Guid>();
        var unresolvedPairs = new List<KeyValuePair<string, double>>();
        foreach (var pair in criteriaScores)
        {
            var normalizedName = NormalizeKey(pair.Key);
            if (!nameToCriteriaId.TryGetValue(normalizedName, out var criteriaId))
            {
                var fallback = nameToCriteriaId.Keys
                    .FirstOrDefault(key => key.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)
                                           || normalizedName.Contains(key, StringComparison.OrdinalIgnoreCase));
                if (fallback is null)
                {
                    unresolvedPairs.Add(pair);
                    continue;
                }

                criteriaId = nameToCriteriaId[fallback];
            }

            usedCriteriaIds.Add(criteriaId);

            scoreEntities.Add(new SubmissionCriteriaScore
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                CriteriaId = criteriaId,
                Score = (decimal)pair.Value,
                Feedback = string.Empty,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (unresolvedPairs.Count > 0)
        {
            var remainingCriteriaIds = versionCriteria
                .Select(x => x.CriteriaId)
                .Where(id => !usedCriteriaIds.Contains(id))
                .ToList();

            var fallbackCount = Math.Min(remainingCriteriaIds.Count, unresolvedPairs.Count);
            for (var i = 0; i < fallbackCount; i++)
            {
                var unresolved = unresolvedPairs[i];
                var fallbackCriteriaId = remainingCriteriaIds[i];

                scoreEntities.Add(new SubmissionCriteriaScore
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    CriteriaId = fallbackCriteriaId,
                    Score = (decimal)unresolved.Value,
                    Feedback = $"Fallback-mapped from AI key: {unresolved.Key}",
                    CreatedAt = DateTime.UtcNow
                });

                _logger.LogWarning(
                    "Fallback-mapped unresolved criteria '{CriteriaName}' to CriteriaId {CriteriaId} for submission {SubmissionId}",
                    unresolved.Key,
                    fallbackCriteriaId,
                    submissionId);
            }
        }

        await _criteriaScoreRepository.AddRangeAsync(scoreEntities);
        _logger.LogInformation(
            "Persisted {Count} submission criteria scores for submission {SubmissionId}",
            scoreEntities.Count,
            submissionId);
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}
