namespace Notification.Domain.Entities
{
    public class DeviceTokenEntity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string DeviceToken { get; set; }
        public string DeviceType { get; set; } // Android or iOS
        public string AppVersion { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
    }
}
