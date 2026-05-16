using System.Text.Json.Serialization;

namespace Company.Application.Models.Events;

public class PostReportCreatedNotificationEvent
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "post.report.created";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("actorId")]
    public string ActorId { get; set; } = string.Empty;

    [JsonPropertyName("actorType")]
    public string ActorType { get; set; } = "USER";

    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "COMPANY_REPORT_REVIEW";

    [JsonPropertyName("targetRoles")]
    public string[]? TargetRoles { get; set; } = new[] { "MODERATOR", "ADMIN" };

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
