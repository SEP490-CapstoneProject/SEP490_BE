using Microsoft.AspNetCore.SignalR;
using Connection.Application.Interfaces;
using System.Security.Claims;
using System.Linq;

namespace Connection.API.Hubs;

public class ChatHub : Hub
{
    private readonly IConnectionService _service;
    private readonly IConnectionEventPublisher _eventPublisher;
    private readonly IUserProfileResolver _userProfileResolver;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int RoomId, int UserId)> ActiveRoomConnections = new();

    public ChatHub(IConnectionService service, IConnectionEventPublisher eventPublisher, IUserProfileResolver userProfileResolver)
    {
        _service = service;
        _eventPublisher = eventPublisher;
        _userProfileResolver = userProfileResolver;
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ActiveRoomConnections.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
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

        ActiveRoomConnections[Context.ConnectionId] = (roomId, userId);

        // mark messages in room as read for this user (auto behavior when opening room)
        var updatedMessageIds = await _service.MarkRoomMessagesAsReadAsync(roomId, userId);

        if (updatedMessageIds != null && updatedMessageIds.Any())
        {
            // notify group about read receipts
            await Clients.Group(roomId.ToString()).SendAsync("MessagesRead", new { roomId, userId, messageIds = updatedMessageIds });

            // Ensure the other user gets the read receipt even if they are NOT in the room right now (e.g., they are in the home page)
            var roomUsers = await _service.GetRoomUsersAsync(roomId);
            var roomConn = roomUsers.FirstOrDefault();
            if (roomConn != default)
            {
                var otherUserId = roomConn.UserIdFrom == userId ? roomConn.UserIdTo : roomConn.UserIdFrom;
                await Clients.Group($"user_{otherUserId}").SendAsync("MessagesRead", new { roomId, userId, messageIds = updatedMessageIds });
            }
        }
    }

    public async Task LeaveRoom(int roomId)
    {
        ActiveRoomConnections.TryRemove(Context.ConnectionId, out _);
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

        var isReceiverInRoom = ActiveRoomConnections.Values.Any(v => v.RoomId == roomId && v.UserId != senderId);

        var message = new Connection.Domain.Entities.Message
        {
            UserId = senderId,
            MessageRoomId = roomId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            Status = isReceiverInRoom ? 1 : 0
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
            var roomConn = roomUsers.FirstOrDefault();
            if (roomConn != default)
            {
                // The other person in the connection
                var targetUserId = roomConn.UserIdFrom == senderId ? roomConn.UserIdTo : roomConn.UserIdFrom;

                var unreadCount = await _service.GetUnreadMessageCountAsync(roomId, targetUserId);

                // Send full room summary structure matching API response
                await Clients.Group($"user_{targetUserId}").SendAsync("RoomUpdated",
                    new
                    {
                        roomId,
                        profileId = room.Connection?.ProfileId ?? 0,
                        connectionId = room.ConnectionId,
                        userIdFrom = roomConn.UserIdFrom,
                        userIdTo = roomConn.UserIdTo,
                        lastContent = dto.Content,
                        lastAt = dto.CreatedAt,
                        unreadCount
                    });

                // Publish to Realtime Service (for users on /hubs/realtime)
                var senderProfile = await _userProfileResolver.ResolveAsync(senderId);

                _ = _eventPublisher.PublishNewMessageNotificationAsync(
                    created.Id, roomId, senderId, targetUserId,
                    dto.Content, dto.CreatedAt, senderProfile);
            }
        }
    }
}
