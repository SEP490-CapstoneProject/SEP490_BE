namespace Notification.Domain.Constants;

public static class NotificationTypeGroups
{
    public static readonly string[] CommunityTypes = new[]
    {
        "COMMUNITY",
        "POST_FAVORITE",
        "COMMUNITY_REPORT_REVIEW"
    };

    public static readonly string[] MessageTypes = new[]
    {
        "CHAT_MESSAGE"
    };

    public const string CommunityCategory = "community";
    public const string SystemCategory = "system";
    public const string MessageCategory = "message";

    public static bool IsCommunityType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return CommunityTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsMessageType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return MessageTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    public static string ResolveCategory(string? type)
        => IsCommunityType(type) ? CommunityCategory : IsMessageType(type) ? MessageCategory : SystemCategory;
}
