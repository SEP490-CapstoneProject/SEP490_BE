using Microsoft.AspNetCore.SignalR;
using Connection.Application.Interfaces;

namespace Connection.API.Hubs;

public class ChatHub : Hub
{
    private readonly IConnectionService _service;

    public ChatHub(IConnectionService service)
    {
        _service = service;
    }

    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task SendMessage(int roomId, string content, int senderId)
    {
        var message = new Connection.Domain.Entities.Message
        {
            UserId = senderId,
            MessageRoomId = roomId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            Status = 0
        };

        var created = await _service.CreateMessageAsync(message);

        // Broadcast to group
        await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", created);
    }
}
