using Microsoft.AspNetCore.SignalR;
using Connection.Application.Interfaces;
using System.Security.Claims;

namespace Connection.API.Hubs;

public class ChatHub : Hub
{
    private readonly IConnectionService _service;

    public ChatHub(IConnectionService service)
    {
        _service = service;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public async Task JoinRoom(int roomId)
    {
        // add connection to group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

        // get userId from JWT claims
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            // cannot determine user, just return after joining
            return;
        }

        // mark messages in room as read for this user (auto behavior when opening room)
        var updatedMessageIds = await _service.MarkRoomMessagesAsReadAsync(roomId, userId);

        if (updatedMessageIds != null && updatedMessageIds.Any())
        {
            // notify group about read receipts
            await Clients.Group(roomId.ToString()).SendAsync("MessagesRead", new { roomId, userId, messageIds = updatedMessageIds });
        }
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task SendMessage(int roomId, string content)
    {
        // get sender id from JWT claims
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var senderId))
        {
            // unauthorized or missing claim
            return;
        }

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
