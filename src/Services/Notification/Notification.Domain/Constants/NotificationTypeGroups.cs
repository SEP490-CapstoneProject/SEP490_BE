namespace Notification.Domain.Constants;

public static class NotificationTypeGroups
{
    public static readonly string[] CommunityTypes =
    [
        "COMMUNITY",
        "POST_FAVORITE",
        "COMMUNITY_REPORT_REVIEW"
    ];

    public const string CommunityCategory = "community";
    public const string SystemCategory = "system";

    public static bool IsCommunityType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return CommunityTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    public static string ResolveCategory(string? type)
        => IsCommunityType(type) ? CommunityCategory : SystemCategory;
}
