namespace Connection.Application.Interfaces;

public interface IConnectionRepository
{
    Task<Connection.Domain.Entities.Connection> CreateAsync(Connection.Domain.Entities.Connection entity);
    Task UpdateAsync(Connection.Domain.Entities.Connection entity);
    Task<Connection.Domain.Entities.Connection?> GetByIdAsync(int id);
    Task<IEnumerable<Connection.Domain.Entities.Connection>> GetAllAsync();

    Task<Connection.Domain.Entities.Room> CreateRoomAsync(Connection.Domain.Entities.Room room);
    Task<Connection.Domain.Entities.Message> CreateMessageAsync(Connection.Domain.Entities.Message message);
    Task<Connection.Domain.Entities.Room?> GetRoomByIdAsync(int id);
    Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByConnectionAsync(int connectionId);
    Task<IEnumerable<Connection.Domain.Entities.Message>> GetMessagesByRoomAsync(int roomId);
    Task<IEnumerable<Connection.Domain.Entities.Message>> GetLatestMessagesByRoomAsync(int roomId, int limit);
    Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByUserIdAsync(int userId);
    Task UpdateRoomAsync(Connection.Domain.Entities.Room room);
    Task<IEnumerable<Connection.Application.DTOs.RoomSummaryRaw>> GetRoomSummariesByUserIdAsync(int userId);
    // Bulk mark-by-ids removed (auto mark on join used). Keep mark-room for auto behavior.
    Task<List<int>> MarkRoomMessagesAsReadAsync(int roomId, int userId);
}
