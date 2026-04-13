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

        // Subscribe user to personal notification group for Home & Room List
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
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

        // Broadcast DTO (not entity) to avoid circular reference serialization errors
        var dto = new Connection.Application.DTOs.MessageDto
        {
            Id = created.Id,
            MessageRoomId = created.MessageRoomId,
            UserId = created.UserId,
            Content = created.Content,
            CreatedAt = created.CreatedAt,
            Status = created.Status == 1 ? "READ" : created.Status == 2 ? "DELIVERED" : "UNREAD"
        };

        // 3️⃣ Broadcast to users INSIDE the room (viewing chat)
        await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", dto);

        // 2️⃣ & 1️⃣ Notify users NOT in the room (room list + home)
        // Get room & connection info for RoomSummary structure
        var room = await _service.GetRoomByIdAsync(roomId);
        if (room == null) return;

        var roomUsers = await _service.GetRoomUsersAsync(roomId);
        if (roomUsers != null)
        {
            foreach (var user in roomUsers)
            {
                // Don't notify the sender
                if (user.Id != senderId)
                {
                    var unreadCount = await _service.GetUnreadMessageCountAsync(roomId, user.Id);

                    // Send full room summary structure matching API response
                    await Clients.Group($"user_{user.Id}").SendAsync("RoomUpdated",
                        new
                        {
                            roomId,
                            profileId = room.Connection?.ProfileId ?? 0,
                            connectionId = room.ConnectionId,
                            userIdFrom = user.UserIdFrom,
                            userIdTo = user.UserIdTo,
                            lastContent = dto.Content,
                            lastAt = dto.CreatedAt,
                            unreadCount
                        });
                }
            }
        }
    }
}
