using RecruitmentPlatform.AI.Models;

namespace RecruitmentPlatform.AI.Abstractions;

public interface IEmbeddingService
{
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public interface ITextNormalizer
{
    string BuildPortfolioText(EmbeddingTextInput input);
    string BuildJobText(EmbeddingTextInput input);
}

public interface IVectorSimilarity
{
    double CosineSimilaritySafe(float[]? a, float[]? b);
}

public interface IScoringHelper
{
    double ComputeSkillScore(IReadOnlyCollection<string> requiredSkills, IReadOnlyCollection<string> candidateSkills);
    double ComputeCategoryScore(IReadOnlyCollection<string> requiredCategories, IReadOnlyCollection<string> candidateCategories);
    double ComputeFinalScore(double cosine, double skillScore, double categoryScore);
}

public interface IMatchingEngine
{
    PagedMatchResult Match(MatchingRequest request, IReadOnlyCollection<MatchingCandidate> candidates, int page, int pageSize);
}
