using Connection.Application.Interfaces;

namespace Connection.Application.Services;

public class ConnectionService : IConnectionService
{
    private readonly Connection.Application.Interfaces.IConnectionRepository _repo;

    public ConnectionService(Connection.Application.Interfaces.IConnectionRepository repo)
    {
        _repo = repo;
    }

    public async Task<Connection.Domain.Entities.Connection> CreateConnectionAsync(Connection.Domain.Entities.Connection conn)
    {
        // Sanitize input: server sets timestamps and initial status, ignore any nested rooms/messages from client
        conn.Id = 0; // ensure EF will insert
        conn.Rooms = new List<Connection.Domain.Entities.Room>();
        conn.CreateAt = DateTime.UtcNow;
        conn.ConnectionAt = null;
        conn.Status = RecruitmentPlatform.Contracts.Enums.ConnectionStatus.PENDING.ToString();

        return await _repo.CreateAsync(conn);
    }

    public async Task UpdateConnectionAsync(Connection.Domain.Entities.Connection conn)
    {
        await _repo.UpdateAsync(conn);
    }

    public async Task<Connection.Domain.Entities.Connection?> GetConnectionByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Connection>> GetAllConnectionsAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<Connection.Domain.Entities.Room> CreateRoomAsync(Connection.Domain.Entities.Room room)
    {
        return await _repo.CreateRoomAsync(room);
    }

    public async Task<Connection.Domain.Entities.Message> CreateMessageAsync(Connection.Domain.Entities.Message message)
    {
        // Sanitize input: use only MessageRoomId (room id) and ignore any nested Room object
        message.Id = 0;
        message.Room = null;
        message.CreatedAt = DateTime.UtcNow;
        // ensure default status (UNREAD = 0) if not provided
        if (message.Status != 1 && message.Status != 2)
        {
            message.Status = 0;
        }

        var created = await _repo.CreateMessageAsync(message);
        // update room last message time
        var room = await _repo.GetRoomByIdAsync(created.MessageRoomId);
        if (room != null)
        {
            room.LastMessAt = created.CreatedAt;
            await _repo.UpdateRoomAsync(room);
        }
        return created;
    }

    public async Task<Connection.Domain.Entities.Room?> GetRoomByIdAsync(int id)
    {
        return await _repo.GetRoomByIdAsync(id);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByConnectionAsync(int connectionId)
    {
        return await _repo.GetRoomsByConnectionAsync(connectionId);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Message>> GetMessagesByRoomAsync(int roomId)
    {
        return await _repo.GetMessagesByRoomAsync(roomId);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByUserIdAsync(int userId)
    {
        return await _repo.GetRoomsByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Message>> GetLatestMessagesByRoomAsync(int roomId, int limit)
    {
        return await _repo.GetLatestMessagesByRoomAsync(roomId, limit);
    }

    public async Task<IEnumerable<Connection.Application.DTOs.RoomSummaryRaw>> GetRoomSummariesByUserIdAsync(int userId)
    {
        return await _repo.GetRoomSummariesByUserIdAsync(userId);
    }

    public async Task<List<int>> MarkRoomMessagesAsReadAsync(int roomId, int userId)
    {
        return await _repo.MarkRoomMessagesAsReadAsync(roomId, userId);
    }

    public async Task<Connection.Domain.Entities.Connection?> UpdateConnectionStatusAsync(int connectionId, RecruitmentPlatform.Contracts.Enums.ConnectionStatus status)
    {
        var conn = await _repo.GetByIdAsync(connectionId);
        if (conn == null) return null;

        conn.Status = status.ToString();
        if (status == RecruitmentPlatform.Contracts.Enums.ConnectionStatus.MATCHED)
        {
            conn.ConnectionAt = DateTime.UtcNow;
            // create room if not exists for this connection
            var rooms = await _repo.GetRoomsByConnectionAsync(conn.Id);
            if (rooms == null || !rooms.Any())
            {
                var room = new Connection.Domain.Entities.Room
                {
                    ConnectionId = conn.Id,
                    CreatedAt = DateTime.UtcNow,
                    LastMessAt = null
                };
                await _repo.CreateRoomAsync(room);
            }
        }

        await _repo.UpdateAsync(conn);
        return conn;
    }
}
