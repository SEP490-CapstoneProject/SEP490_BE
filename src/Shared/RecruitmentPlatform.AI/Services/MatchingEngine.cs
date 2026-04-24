using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;

namespace RecruitmentPlatform.AI.Services;

public sealed class MatchingEngine : IMatchingEngine
{
    private readonly IVectorSimilarity _similarity;
    private readonly IScoringHelper _scoring;
    private readonly MatchingOptions _options;

    public MatchingEngine(IVectorSimilarity similarity, IScoringHelper scoring)
    {
        _similarity = similarity;
        _scoring = scoring;
        _options = new MatchingOptions();
    }

    public PagedMatchResult Match(MatchingRequest request, IReadOnlyCollection<MatchingCandidate> candidates, int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Min(_options.MaxPageSize, Math.Max(1, pageSize));

        var filtered = candidates
            .Where(IsQualityCandidate)
            .ToList();

        var prefiltered = filtered
            .Select(candidate => new
            {
                Candidate = candidate,
                Overlap = ComputeSkillOverlap(request.SourceSkills, candidate.Skills)
            })
            .OrderByDescending(x => x.Overlap)
            .ThenByDescending(x => x.Candidate.UpdatedAt)
            .ThenBy(x => x.Candidate.Id)
            .Take(_options.PreFilterTake)
            .Select(x => x.Candidate)
            .ToList();

        var scored = prefiltered
            .Select(candidate =>
            {
                var cosine = _similarity.CosineSimilaritySafe(request.SourceEmbedding, candidate.Embedding);
                var skillScore = _scoring.ComputeSkillScore(request.SourceSkills, candidate.Skills);
                var categoryScore = _scoring.ComputeCategoryScore(request.SourceCategories, candidate.Categories);
                var finalScore = _scoring.ComputeFinalScore(cosine, skillScore, categoryScore);
                return new ScoredMatch
                {
                    Id = candidate.Id,
                    Title = candidate.Title,
                    Cosine = cosine,
                    SkillScore = skillScore,
                    CategoryScore = categoryScore,
                    FinalScore = finalScore,
                    UpdatedAt = candidate.UpdatedAt
                };
            })
            .Where(match => match.FinalScore >= _options.MinimumFinalScore)
            .OrderByDescending(match => match.FinalScore)
            .ThenByDescending(match => match.UpdatedAt)
            .ThenBy(match => match.Id)
            .ToList();

        var total = scored.Count;
        var items = scored
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new PagedMatchResult
        {
            Items = items,
            Total = total,
            Page = safePage,
            PageSize = safePageSize
        };
    }

    private bool IsQualityCandidate(MatchingCandidate candidate)
    {
        if (!string.Equals(candidate.EmbeddingStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (candidate.Skills.Count == 0)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(candidate.Description) && candidate.Description.Trim().Length >= _options.MinimumDescriptionLength;
    }

    private static int ComputeSkillOverlap(IReadOnlyCollection<string> sourceSkills, IReadOnlyCollection<string> candidateSkills)
    {
        var source = sourceSkills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToHashSet();

        if (source.Count == 0)
        {
            return 0;
        }

        var candidate = candidateSkills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToHashSet();

        return source.Count(skill => candidate.Contains(skill));
    }
}
