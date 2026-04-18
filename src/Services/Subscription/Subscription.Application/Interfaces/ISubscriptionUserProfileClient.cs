namespace Subscription.Application.Interfaces;

public interface ISubscriptionUserProfileClient
{
    Task<Dictionary<int, SubscriptionUserProfileDto>> GetUsersBatchAsync(IEnumerable<int> userIds);
}

public class SubscriptionUserProfileDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
}
