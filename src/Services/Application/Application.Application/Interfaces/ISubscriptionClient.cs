namespace Application.Application.Interfaces;

public interface ISubscriptionClient
{
    Task<EntitlementsDto?> GetEntitlementsAsync(int userId);
}

public class EntitlementsDto
{
    public int Version { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Dictionary<string, object> Features { get; set; } = new();
    public DateTime ExpiredAt { get; set; }
}
