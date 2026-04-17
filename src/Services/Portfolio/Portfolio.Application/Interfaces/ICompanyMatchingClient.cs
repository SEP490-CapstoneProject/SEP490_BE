using RecruitmentPlatform.AI.Models;

namespace Portfolio.Application.Interfaces;

public interface ICompanyMatchingClient
{
    Task<IReadOnlyList<MatchingCandidate>> GetJobCandidatesAsync(CancellationToken cancellationToken = default);
}
