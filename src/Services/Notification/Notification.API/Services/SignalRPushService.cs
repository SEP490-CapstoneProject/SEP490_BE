using Microsoft.AspNetCore.SignalR;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.API.Hubs;

namespace Notification.API.Services;

public class SignalRPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRPushService(IHubContext<NotificationHub> hubContext) => _hubContext = hubContext;

    public Task PushAsync(string userId, UserNotificationDto dto) =>
        _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", dto);
}
