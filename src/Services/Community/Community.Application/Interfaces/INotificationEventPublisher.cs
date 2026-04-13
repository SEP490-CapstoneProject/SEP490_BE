using Community.Application.Models.Events;

namespace Community.Application.Interfaces;

public interface INotificationEventPublisher
{
    Task PublishPostFavoriteNotificationAsync(PostFavoriteNotificationEvent evt, CancellationToken cancellationToken = default);
}