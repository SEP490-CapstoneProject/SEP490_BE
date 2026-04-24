using Connection.Application.Interfaces;
using RecruitmentPlatform.Contracts.Time;

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
        conn.CreateAt = VietnamTime.Now();
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
        message.CreatedAt = VietnamTime.Now();
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

    public async Task<Connection.Application.DTOs.RoomSummaryRaw?> GetRoomSummaryByConnectionIdAsync(int connectionId, int userId)
    {
        return await _repo.GetRoomSummaryByConnectionIdAsync(connectionId, userId);
    }

    public async Task<List<int>> MarkRoomMessagesAsReadAsync(int roomId, int userId)
    {
        return await _repo.MarkRoomMessagesAsReadAsync(roomId, userId);
    }

    public async Task<Connection.Domain.Entities.Connection?> UpdateConnectionStatusAsync(int connectionId, RecruitmentPlatform.Contracts.Enums.ConnectionStatus status, int currentUserId)
    {
        var conn = await _repo.GetByIdAsync(connectionId);
        if (conn == null) return null;

        conn.Status = status.ToString();

        if (status == RecruitmentPlatform.Contracts.Enums.ConnectionStatus.MATCHED)
        {
            conn.ConnectionAt = VietnamTime.Now();
            // create room if not exists for this connection
            var rooms = await _repo.GetRoomsByConnectionAsync(conn.Id);
            if (rooms == null || !rooms.Any())
            {
                var room = new Connection.Domain.Entities.Room
                {
                    ConnectionId = conn.Id,
                    CreatedAt = VietnamTime.Now(),
                    LastMessAt = null
                };
                await _repo.CreateRoomAsync(room);
            }
        }
        else if (status == RecruitmentPlatform.Contracts.Enums.ConnectionStatus.BLOCK)
        {
            // The user who triggers BLOCK is recorded in BlockId
            conn.BlockId = currentUserId;
        }
        else if (status == RecruitmentPlatform.Contracts.Enums.ConnectionStatus.STORED)
        {
            // Reset unread count by marking all messages in this connection's rooms as read
            var rooms = await _repo.GetRoomsByConnectionAsync(conn.Id);
            foreach (var room in rooms)
            {
                await _repo.MarkRoomMessagesAsReadAsync(room.Id, currentUserId);
            }
        }

        await _repo.UpdateAsync(conn);
        return conn;
    }

    public async Task<IEnumerable<(int Id, int UserIdFrom, int UserIdTo)>> GetRoomUsersAsync(int roomId)
    {
        return await _repo.GetRoomUsersAsync(roomId);
    }

    public async Task<int> GetUnreadMessageCountAsync(int roomId, int userId)
    {
        return await _repo.GetUnreadMessageCountAsync(roomId, userId);
    }

    public async Task<string?> GetConnectionStatusByUsersAsync(int userId1, int userId2)
    {
        return await _repo.GetConnectionStatusByUsersAsync(userId1, userId2);
    }
}
