using Notification.Domain.Entities;
using System.Collections.Generic;

namespace Notification.Application.Mappers
{
    public class FcmNotificationMapper
    {
        public static Dictionary<string, string> ToFcmDataPayload(NotificationEntity notification)
        {
            if (notification == null)
            {
                return new Dictionary<string, string>();
            }

            var data = new Dictionary<string, string>
            {
                { "notificationId", notification.Id.ToString() },
                { "notificationType", notification.Type },
                { "deepLink", $"app://notification/{notification.Id}" }
            };

            if (!string.IsNullOrEmpty(notification.ActorId))
            {
                data["actorId"] = notification.ActorId;
            }

            if (!string.IsNullOrEmpty(notification.ObjectId))
            {
                data["objectId"] = notification.ObjectId;
            }

            if (!string.IsNullOrEmpty(notification.EventId))
            {
                data["eventId"] = notification.EventId;
            }

            return data;
        }

        public static (string Title, string Body) ToFcmNotificationText(NotificationEntity notification)
        {
            if (notification == null)
            {
                return ("Notification", "");
            }

            // Use the pre-formatted title and content from the database
            return (notification.Title, notification.Content);
        }

        public static string GetPriorityForNotificationType(string notificationType)
        {
            // High priority for important notifications (chat, direct messages)
            var highPriorityTypes = new[]
            {
                "MESSAGE",
                "CHAT",
                "DIRECT_MESSAGE",
                "MENTION",
                "JOB_APPLICATION_STATUS_CHANGED"
            };

            // Normal priority for regular activities
            var normalPriorityTypes = new[]
            {
                "POST_FAVORITE",
                "POST_COMMENT",
                "COMMENT_REPLY",
                "CONNECTION_REQUEST",
                "CONNECTION_ACCEPTED"
            };

            if (highPriorityTypes.Contains(notificationType))
            {
                return "high";
            }

            if (normalPriorityTypes.Contains(notificationType))
            {
                return "normal";
            }

            // Default to normal for unknown types
            return "normal";
        }

        public static int GetTtlForNotificationType(string notificationType)
        {
            // 1 hour TTL for high priority
            if (notificationType.StartsWith("MESSAGE") || notificationType.StartsWith("CHAT"))
            {
                return 3600;
            }

            // 24 hours TTL for regular activities
            return 86400;
        }
    }
}
