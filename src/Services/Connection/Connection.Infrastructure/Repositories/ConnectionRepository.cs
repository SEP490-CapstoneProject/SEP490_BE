using Connection.Application.Interfaces;
using Connection.Domain.Entities;
using Connection.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Connection.Infrastructure.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ConnectionDbContext _context;

    public ConnectionRepository(ConnectionDbContext context)
    {
        _context = context;
    }

    public async Task<Connection.Domain.Entities.Connection> CreateAsync(Connection.Domain.Entities.Connection entity)
    {
        _context.Connections.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Connection.Domain.Entities.Connection entity)
    {
        _context.Connections.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<Connection.Domain.Entities.Room> CreateRoomAsync(Connection.Domain.Entities.Room room)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<Connection.Domain.Entities.Message> CreateMessageAsync(Connection.Domain.Entities.Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<Connection.Domain.Entities.Connection?> GetByIdAsync(int id)
    {
        return await _context.Connections.Include(c => c.Rooms).ThenInclude(r => r.Messages).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Connection>> GetAllAsync()
    {
        return await _context.Connections.Include(c => c.Rooms).ThenInclude(r => r.Messages).ToListAsync();
    }

    public async Task<Connection.Domain.Entities.Room?> GetRoomByIdAsync(int id)
    {
        return await _context.Rooms.Include(r => r.Messages).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByConnectionAsync(int connectionId)
    {
        return await _context.Rooms.Where(r => r.ConnectionId == connectionId).Include(r => r.Messages).ToListAsync();
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Room>> GetRoomsByUserIdAsync(int userId)
    {
        return await _context.Rooms
            .Include(r => r.Messages)
            .Include(r => r.Connection)
            .Where(r => r.Connection.UserIdFrom == userId || r.Connection.UserIdTo == userId)
            .ToListAsync();
    }

    public async Task UpdateRoomAsync(Connection.Domain.Entities.Room room)
    {
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Message>> GetMessagesByRoomAsync(int roomId)
    {
        return await _context.Messages.Where(m => m.MessageRoomId == roomId).ToListAsync();
    }
}
