namespace Connection.Application.Interfaces;

public interface IConnectionService
{
    Task<Connection.Domain.Entities.Connection> CreateConnectionAsync(Connection.Domain.Entities.Connection conn);
    Task UpdateConnectionAsync(Connection.Domain.Entities.Connection conn);
    Task<Connection.Domain.Entities.Connection?> GetConnectionByIdAsync(int id);
    Task<IEnumerable<Connection.Domain.Entities.Connection>> GetAllConnectionsAsync();

    Task<Connection.Domain.Entities.Room> CreateRoomAsync(Connection.Domain.Entities.Room room);
    Task<Connection.Domain.Entities.Message> CreateMessageAsync(Connection.Domain.Entities.Message message);
    Task<Connection.Domain.Entities.Room?> GetRoomByIdAsync(int id);
    Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByConnectionAsync(int connectionId);
    Task<IEnumerable<Connection.Domain.Entities.Message>> GetMessagesByRoomAsync(int roomId);
    Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByUserIdAsync(int userId);
    Task<IEnumerable<Connection.Domain.Entities.Message>> GetLatestMessagesByRoomAsync(int roomId, int limit);
    Task<IEnumerable<Connection.Application.DTOs.RoomSummaryRaw>> GetRoomSummariesByUserIdAsync(int userId);
    Task<Connection.Application.DTOs.RoomSummaryRaw?> GetRoomSummaryByConnectionIdAsync(int connectionId, int userId);
    // Bulk mark-by-ids removed (auto mark on join used).
    Task<List<int>> MarkRoomMessagesAsReadAsync(int roomId, int userId);
    Task<Connection.Domain.Entities.Connection?> UpdateConnectionStatusAsync(int connectionId, RecruitmentPlatform.Contracts.Enums.ConnectionStatus status, int currentUserId);
    Task<IEnumerable<(int Id, int UserIdFrom, int UserIdTo)>> GetRoomUsersAsync(int roomId);
    Task<int> GetUnreadMessageCountAsync(int roomId, int userId);
    Task<(int ConnectionId, string? Status)> GetConnectionStatusByUsersAsync(int userId1, int userId2);
    Task<string?> GetConnectionStatusByIdAsync(int connectionId);
}
