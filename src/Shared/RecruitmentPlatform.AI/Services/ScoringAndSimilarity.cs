using RecruitmentPlatform.AI.Abstractions;

namespace RecruitmentPlatform.AI.Services;

public sealed class CosineSimilarityService : IVectorSimilarity
{
    public double CosineSimilaritySafe(float[]? a, float[]? b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        if (a.Length != b.Length)
        {
            return 0;
        }

        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA <= 0 || normB <= 0)
        {
            return 0;
        }

        var cosine = dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        if (double.IsNaN(cosine) || double.IsInfinity(cosine))
        {
            return 0;
        }

        return Clamp01(Math.Max(0, cosine));
    }

    private static double Clamp01(double value) => Math.Min(1, Math.Max(0, value));
}

public sealed class ScoringHelper : IScoringHelper
{
    public double ComputeSkillScore(IReadOnlyCollection<string> requiredSkills, IReadOnlyCollection<string> candidateSkills)
    {
        if (requiredSkills.Count == 0)
        {
            return 0;
        }

        var required = requiredSkills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (required.Count == 0)
        {
            return 0;
        }

        var candidate = candidateSkills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToHashSet();

        var matched = required.Count(skill => candidate.Contains(skill));
        return Clamp01((double)matched / required.Count);
    }

    public double ComputeCategoryScore(IReadOnlyCollection<string> requiredCategories, IReadOnlyCollection<string> candidateCategories)
    {
        if (requiredCategories.Count == 0)
        {
            return 0;
        }

        var required = requiredCategories
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (required.Count == 0)
        {
            return 0;
        }

        var candidate = candidateCategories
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToHashSet();

        if (required.Count == 1)
        {
            return candidate.Contains(required[0]) ? 1 : 0;
        }

        var overlap = required.Count(category => candidate.Contains(category));
        return Clamp01((double)overlap / required.Count);
    }

    public double ComputeFinalScore(double cosine, double skillScore, double categoryScore)
    {
        var finalScore = Clamp01(cosine) * 0.7 + Clamp01(skillScore) * 0.2 + Clamp01(categoryScore) * 0.1;
        return Math.Round(Clamp01(finalScore), 4, MidpointRounding.AwayFromZero);
    }

    private static double Clamp01(double value) => Math.Min(1, Math.Max(0, value));
}
