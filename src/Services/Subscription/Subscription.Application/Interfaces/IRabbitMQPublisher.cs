using Subscription.Application.Events;

namespace Subscription.Application.Interfaces;

public interface IRabbitMQPublisher
{
    Task PublishAsync<T>(T @event) where T : BaseEvent;
    Task<bool> IsHealthyAsync();
}
