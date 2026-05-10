namespace Notification.Infrastructure.Messaging;
using RecruitmentPlatform.Contracts.Time;
using System.Text.Json;
using System.Text.Json.Serialization;

public class NotificationEvent
{
    public string? EventId { get; set; }
    public string EventType { get; set; } = default!;
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string[]? TargetRoles { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Type { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = VietnamTime.Now();
    public NotificationEventUser? Author { get; set; }
    public NotificationEventUser? ReplyToUser { get; set; }
    public int? ReplyToUserId { get; set; }
}

public class NotificationEventUser
{
    [JsonConverter(typeof(NumericToStringConverter))]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
}

/// <summary>
/// Converter to handle author.id being sent as number or string from community service
/// </summary>
public class NumericToStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
