using Connection.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Connection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly IConnectionService _service;
    private readonly IHttpClientFactory _httpClientFactory;

    public ConnectionController(IConnectionService service, IHttpClientFactory httpClientFactory)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CreateConnection([FromBody] Connection.Domain.Entities.Connection conn)
    {
        var created = await _service.CreateConnectionAsync(conn);
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

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] Connection.Domain.Entities.Room room)
    {
        var created = await _service.CreateRoomAsync(room);
        return CreatedAtAction(nameof(GetRoomById), new { id = created.Id }, created);
    }

    [HttpGet("rooms/{id}")]
    public async Task<IActionResult> GetRoomById(int id)
    {
        var room = await _service.GetRoomByIdAsync(id);
        if (room == null) return NotFound();
        return Ok(room);
    }

    [HttpGet("rooms/connection/{connectionId}")]
    public async Task<IActionResult> GetRoomsByConnection(int connectionId)
    {
        var rooms = await _service.GetRoomsByConnectionAsync(connectionId);
        return Ok(rooms);
    }

    [HttpGet("rooms/summary/{userId}")]
    public async Task<IActionResult> GetRoomSummaries(int userId)
    {
        var rooms = await _service.GetRoomsByUserIdAsync(userId);
        var client = _httpClientFactory.CreateClient("UserProfile");
        var summaries = new List<Connection.Application.DTOs.RoomSummaryDto>();

        foreach (var room in rooms)
        {
            var last = room.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unread = room.Messages?.Count(m => m.Status == 0 && m.UserId != userId) ?? 0;

            // Determine other user id
            var conn = room.Connection;
            int otherUserId = (conn.UserIdFrom == userId) ? conn.UserIdTo : conn.UserIdFrom;
            int profileId = conn.ProfileId;

            string name = otherUserId.ToString();
            string? avatar = null;
            string? cover = null;
            string role = "USER";

            // Try company first
            try
            {
                var res = await client.GetAsync($"/api/company/{profileId}");
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
                    var res2 = await client.GetAsync($"/api/employee/{profileId}");
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
                RoomId = room.Id,
                Name = name,
                Avatar = avatar,
                CoverImage = cover,
                Role = role,
                LastContent = last?.Content,
                LastAt = last?.CreatedAt,
                UnreadCount = unread
            });
        }

        return Ok(summaries);
    }

    [HttpPost("rooms/{roomId}/messages")]
    public async Task<IActionResult> CreateMessage(int roomId, [FromBody] Connection.Domain.Entities.Message message)
    {
        message.MessageRoomId = roomId;
        var created = await _service.CreateMessageAsync(message);
        return CreatedAtAction(nameof(GetMessagesByRoom), new { roomId = roomId }, created);
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<IActionResult> GetMessagesByRoom(int roomId)
    {
        var messages = await _service.GetMessagesByRoomAsync(roomId);
        return Ok(messages);
    }
}
