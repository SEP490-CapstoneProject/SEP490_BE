namespace Company.Application.Interfaces;

public interface ICompanyEmbeddingEventPublisher
{
    Task PublishCompanyPostChangedAsync(int postId, CancellationToken cancellationToken = default);
}
