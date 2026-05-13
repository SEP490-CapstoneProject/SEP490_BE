namespace Notification.Domain.Entities
{
    public class PushNotificationLogEntity
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public int DeviceTokenId { get; set; }
        public string Status { get; set; } // Sent, Failed, Bounced
        public string MessageId { get; set; } // FCM Message ID
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
