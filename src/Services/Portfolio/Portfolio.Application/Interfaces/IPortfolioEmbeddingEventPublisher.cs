namespace Portfolio.Application.Interfaces;

public interface IPortfolioEmbeddingEventPublisher
{
    Task PublishPortfolioChangedAsync(int portfolioId, CancellationToken cancellationToken = default);
}
