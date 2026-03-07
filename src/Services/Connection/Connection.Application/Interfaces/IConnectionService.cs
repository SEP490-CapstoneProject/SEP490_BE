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
}
