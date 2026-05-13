namespace Notification.Domain.Entities
{
    public class NotificationSettingsEntity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public bool PushNotificationsEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public bool VibrateEnabled { get; set; } = true;
        public bool ChatNotificationsEnabled { get; set; } = true;
        public bool MentionNotificationsEnabled { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
