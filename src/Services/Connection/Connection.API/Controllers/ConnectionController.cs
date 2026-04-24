using Connection.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Connection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly IConnectionService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.ChatHub> _hubContext;
    private readonly IConnectionEventPublisher _eventPublisher;

    public ConnectionController(
        IConnectionService service,
        IHttpClientFactory httpClientFactory,
        Microsoft.AspNetCore.SignalR.IHubContext<Hubs.ChatHub> hubContext,
        IConnectionEventPublisher eventPublisher)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateConnection([FromBody] Connection.Application.DTOs.CreateConnectionRequest req)
    {
        var conn = new Connection.Domain.Entities.Connection
        {
            UserIdFrom = req.UserIdFrom,
            UserIdTo = req.UserIdTo,
            ProfileId = req.ProfileId
            // Status/CreateAt will be set by service
        };

        var created = await _service.CreateConnectionAsync(conn);

        // 1) Realtime via ChatHub (direct — for users connected to this service)
        await _hubContext.Clients.Group($"user_{created.UserIdTo}").SendAsync("ConnectionRequested", new
        {
            connectionId = created.Id,
            fromUserId   = created.UserIdFrom,
            toUserId     = created.UserIdTo,
            profileId    = created.ProfileId,
            status       = created.Status,
            createdAt    = created.CreateAt
        });

        // 2) Realtime via Realtime Service (for users connected to /hubs/realtime)
        _ = _eventPublisher.PublishConnectionRequestedAsync(
            created.Id, created.UserIdFrom, created.UserIdTo,
            created.ProfileId, created.CreateAt);

        return CreatedAtAction(nameof(GetConnectionById), new { id = created.Id }, created);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetConnectionById(int id)
    {
        var conn = await _service.GetConnectionByIdAsync(id);
        if (conn == null) return NotFound();
        return Ok(conn);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllConnectionsAsync();
        return Ok(list);
    }

    public class UpdateStatusRequest { public string Status { get; set; } = string.Empty; }

    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        if (string.IsNullOrEmpty(request?.Status)) return BadRequest(new { error = "Status is required" });

        if (!System.Enum.TryParse<RecruitmentPlatform.Contracts.Enums.ConnectionStatus>(request.Status, true, out var status))
        {
            return BadRequest(new { error = "Invalid status value" });
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        var updated = await _service.UpdateConnectionStatusAsync(id, status, currentUserId);
        if (updated == null) return NotFound();

        // Realtime notifications based on the new status
        if (status == RecruitmentPlatform.Contracts.Enums.ConnectionStatus.MATCHED)
        {
            // 1) Via ChatHub (direct)
            await _hubContext.Clients.Group($"user_{updated.UserIdFrom}").SendAsync("ConnectionAccepted", new
            {
                connectionId = updated.Id,
                fromUserId   = updated.UserIdFrom,
                toUserId     = updated.UserIdTo,
                status       = updated.Status
            });

            // 2) Via Realtime Service
            _ = _eventPublisher.PublishConnectionAcceptedAsync(
                updated.Id, updated.UserIdFrom, updated.UserIdTo,
                updated.ConnectionAt ?? DateTime.UtcNow);
        }
        else if (status == RecruitmentPlatform.Contracts.Enums.ConnectionStatus.BLOCK)
        {
            // Notify the other party that the connection was blocked
            int blockedUserId = updated.UserIdFrom == currentUserId ? updated.UserIdTo : updated.UserIdFrom;
            await _hubContext.Clients.Group($"user_{blockedUserId}").SendAsync("ConnectionBlocked", new
            {
                connectionId = updated.Id,
                blockedBy    = currentUserId,
                status       = updated.Status
            });
        }

        return Ok(updated);
    }

    // Room creation is handled automatically when a Connection is matched; manual room endpoints removed

    /// <summary>
    /// Kiểm tra trạng thái connection giữa 2 user (bỏ qua các connection STORED).
    /// Trả về: { status: "PENDING" | "MATCHED" | "BLOCK" } hoặc { status: null } nếu không tìm thấy.
    /// </summary>
    [HttpGet("status/by-users")]
    public async Task<IActionResult> GetConnectionStatusByUsers([FromQuery] int userId1, [FromQuery] int userId2)
    {
        if (userId1 <= 0 || userId2 <= 0)
            return BadRequest(new { error = "userId1 and userId2 are required" });

        var status = await _service.GetConnectionStatusByUsersAsync(userId1, userId2);
        return Ok(new { status });
    }

    [HttpGet("rooms/summary/{userId}")]
    public async Task<IActionResult> GetRoomSummaries(int userId)
    {
        var raw = await _service.GetRoomSummariesByUserIdAsync(userId);
        var client = _httpClientFactory.CreateClient("UserProfile");
        var summaries = new List<Connection.Application.DTOs.RoomSummaryDto>();

        foreach (var room in raw)
        {
            int otherUserId = room.UserIdFrom == userId ? room.UserIdTo : room.UserIdFrom;
            string name = otherUserId.ToString();
            string? avatar = null;
            string? cover = null;
            string role = "USER";

            try
            {
                var res = await client.GetAsync($"/api/company/by-user/{otherUserId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("companyName", out var comp)) name = comp.GetString() ?? name;
                    if (doc.RootElement.TryGetProperty("avatar", out var av)) avatar = av.GetString();
                    if (doc.RootElement.TryGetProperty("coverImage", out var cv)) cover = cv.GetString();
                    role = "COMPANY";
                }
                else
                {
                    var res2 = await client.GetAsync($"/api/employee/by-user/{otherUserId}");
                    if (res2.IsSuccessStatusCode)
                    {
                        var json2 = await res2.Content.ReadAsStringAsync();
                        using var doc2 = System.Text.Json.JsonDocument.Parse(json2);
                        if (doc2.RootElement.TryGetProperty("name", out var nm)) name = nm.GetString() ?? name;
                        if (doc2.RootElement.TryGetProperty("avatar", out var av2)) avatar = av2.GetString();
                        if (doc2.RootElement.TryGetProperty("coverImage", out var cv2)) cover = cv2.GetString();
                        role = "USER";
                    }
                }
            }
            catch
            {
                // ignore profile fetch errors
            }

            summaries.Add(new Connection.Application.DTOs.RoomSummaryDto
            {
                RoomId = room.RoomId,
                Name = name,
                Avatar = avatar,
                CoverImage = cover,
                Role = role,
                LastContent = room.LastContent,
                LastAt = room.LastAt,
                UnreadCount = room.UnreadCount
            });
        }

        return Ok(summaries);
    }

    /// <summary>
    /// Lấy room summary của 1 connection cụ thể theo connectionId.
    /// userId dùng để tính unreadCount đúng (tin nhắn chưa đọc của user đó).
    /// </summary>
    [HttpGet("rooms/summary/by-connection/{connectionId}")]
    public async Task<IActionResult> GetRoomSummaryByConnection(int connectionId, [FromQuery] int userId)
    {
        if (userId <= 0)
            return BadRequest(new { error = "userId is required" });

        var room = await _service.GetRoomSummaryByConnectionIdAsync(connectionId, userId);
        if (room == null)
            return NotFound(new { error = "Room not found for the given connectionId" });

        int otherUserId = room.UserIdFrom == userId ? room.UserIdTo : room.UserIdFrom;
        string name = otherUserId.ToString();
        string? avatar = null;
        string? cover = null;
        string role = "USER";

        var client = _httpClientFactory.CreateClient("UserProfile");
        try
        {
            var res = await client.GetAsync($"/api/company/by-user/{otherUserId}");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("companyName", out var comp)) name = comp.GetString() ?? name;
                if (doc.RootElement.TryGetProperty("avatar", out var av)) avatar = av.GetString();
                if (doc.RootElement.TryGetProperty("coverImage", out var cv)) cover = cv.GetString();
                role = "COMPANY";
            }
            else
            {
                var res2 = await client.GetAsync($"/api/employee/by-user/{otherUserId}");
                if (res2.IsSuccessStatusCode)
                {
                    var json2 = await res2.Content.ReadAsStringAsync();
                    using var doc2 = System.Text.Json.JsonDocument.Parse(json2);
                    if (doc2.RootElement.TryGetProperty("name", out var nm)) name = nm.GetString() ?? name;
                    if (doc2.RootElement.TryGetProperty("avatar", out var av2)) avatar = av2.GetString();
                    if (doc2.RootElement.TryGetProperty("coverImage", out var cv2)) cover = cv2.GetString();
                    role = "USER";
                }
            }
        }
        catch
        {
            // ignore profile fetch errors
        }

        var summary = new Connection.Application.DTOs.RoomSummaryDto
        {
            RoomId = room.RoomId,
            Name = name,
            Avatar = avatar,
            CoverImage = cover,
            Role = role,
            LastContent = room.LastContent,
            LastAt = room.LastAt,
            UnreadCount = room.UnreadCount
        };

        return Ok(summary);
    }

    [HttpPost("rooms/{roomId}/messages")]
    [Authorize]
    public async Task<IActionResult> CreateMessage(int roomId, [FromBody] Connection.Application.DTOs.CreateMessageRequest req)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        var message = new Connection.Domain.Entities.Message
        {
            MessageRoomId = roomId,
            UserId = currentUserId,
            Content = req.Content
            // CreatedAt/Status set by service
        };

        var created = await _service.CreateMessageAsync(message);
        return CreatedAtAction(nameof(GetLatestMessages), new { roomId = roomId }, created);
    }

    [HttpGet("rooms/{roomId}/messages/latest")]
    public async Task<IActionResult> GetLatestMessages(int roomId, [FromQuery] int limit = 50)
    {
        var msgs = await _service.GetLatestMessagesByRoomAsync(roomId, limit);

        var result = msgs.Select(m => new Connection.Application.DTOs.MessageDto
        {
            Id = m.Id,
            MessageRoomId = m.MessageRoomId,
            UserId = m.UserId,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            Status = m.Status == 1 ? "READ" : m.Status == 2 ? "DELIVERED" : "UNREAD"
        }).ToList();

        return Ok(result);
    }

    // Bulk tick-mark endpoint removed; messages are auto-marked READ when user joins a room (via SignalR JoinRoom)

    [HttpPost("rooms/{roomId}/mark-read")]
    [Authorize]
    public async Task<IActionResult> MarkRoomRead(int roomId)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        var updated = await _service.MarkRoomMessagesAsReadAsync(roomId, currentUserId);
        
        if (updated != null && updated.Any())
        {
            // notify group about read receipts
            await _hubContext.Clients.Group(roomId.ToString()).SendAsync("MessagesRead", new { roomId, userId = currentUserId, messageIds = updated });

            // notify the specific other user
            var roomUsers = await _service.GetRoomUsersAsync(roomId);
            var roomConn = roomUsers.FirstOrDefault();
            if (roomConn != default)
            {
                var otherUserId = roomConn.UserIdFrom == currentUserId ? roomConn.UserIdTo : roomConn.UserIdFrom;
                await _hubContext.Clients.Group($"user_{otherUserId}").SendAsync("MessagesRead", new { roomId, userId = currentUserId, messageIds = updated });
            }
        }

        return Ok(new { updated = updated?.Count ?? 0 });
    }
}
