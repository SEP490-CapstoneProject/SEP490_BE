using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notification.Application.Interfaces
{
    public interface IFcmService
    {
        Task<string?> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
        Task<bool> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
        Task<bool> ValidateTokenAsync(string deviceToken);
    }
}
