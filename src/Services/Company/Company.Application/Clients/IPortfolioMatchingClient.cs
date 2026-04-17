using RecruitmentPlatform.AI.Models;

namespace Company.Application.Clients;

public interface IPortfolioMatchingClient
{
    Task<IReadOnlyList<MatchingCandidate>> GetPortfolioCandidatesAsync(CancellationToken cancellationToken = default);
}
