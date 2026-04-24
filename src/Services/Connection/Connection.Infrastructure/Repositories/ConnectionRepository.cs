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

    public async Task<IEnumerable<Connection.Application.DTOs.RoomSummaryRaw>> GetRoomSummariesByUserIdAsync(int userId)
    {
        // Single-query projection to get last message and unread count per room
        // Exclude connections with STORED status (hidden/archived by user)
        var query = from r in _context.Rooms
                    join c in _context.Connections on r.ConnectionId equals c.Id
                    where (c.UserIdFrom == userId || c.UserIdTo == userId)
                          && c.Status != RecruitmentPlatform.Contracts.Enums.ConnectionStatus.STORED.ToString()
                    select new
                    {
                        Room = r,
                        Connection = c
                    };

        var list = await query
            .Select(x => new Connection.Application.DTOs.RoomSummaryRaw
            {
                RoomId = x.Room.Id,
                ProfileId = x.Connection.ProfileId,
                ConnectionId = x.Connection.Id,
                UserIdFrom = x.Connection.UserIdFrom,
                UserIdTo = x.Connection.UserIdTo,
                LastContent = x.Room.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                LastAt = x.Room.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefault(),
                UnreadCount = x.Room.Messages.Count(m => m.Status == 0 && m.UserId != userId)
            })
            .OrderByDescending(r => r.LastAt)  // Sort by LastAt descending (newest first)
            .ToListAsync();

        return list;
    }

    public async Task<Connection.Application.DTOs.RoomSummaryRaw?> GetRoomSummaryByConnectionIdAsync(int connectionId, int userId)
    {
        return await (from r in _context.Rooms
                      join c in _context.Connections on r.ConnectionId equals c.Id
                      where c.Id == connectionId
                      select new Connection.Application.DTOs.RoomSummaryRaw
                      {
                          RoomId = r.Id,
                          ProfileId = c.ProfileId,
                          ConnectionId = c.Id,
                          UserIdFrom = c.UserIdFrom,
                          UserIdTo = c.UserIdTo,
                          LastContent = r.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                          LastAt = r.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefault(),
                          UnreadCount = r.Messages.Count(m => m.Status == 0 && m.UserId != userId)
                      })
                     .FirstOrDefaultAsync();
    }

    public async Task<List<int>> MarkRoomMessagesAsReadAsync(int roomId, int userId)
    {
        var msgs = await _context.Messages.Where(m => m.MessageRoomId == roomId && m.UserId != userId && m.Status != 1).ToListAsync();
        var updated = new List<int>();
        foreach (var m in msgs)
        {
            m.Status = 1; // READ
            updated.Add(m.Id);
        }
        await _context.SaveChangesAsync();
        return updated;
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Message>> GetMessagesByRoomAsync(int roomId)
    {
        return await _context.Messages.Where(m => m.MessageRoomId == roomId).ToListAsync();
    }

    public async Task<IEnumerable<Connection.Domain.Entities.Message>> GetLatestMessagesByRoomAsync(int roomId, int limit)
    {
        var items = await _context.Messages
            .Where(m => m.MessageRoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        // return in reverse chronological order (newest -> oldest) as requested
        return items;
    }

    public async Task<IEnumerable<(int Id, int UserIdFrom, int UserIdTo)>> GetRoomUsersAsync(int roomId)
    {
        var results = await _context.Rooms
            .Where(r => r.Id == roomId)
            .Include(r => r.Connection)
            .Select(r => new { r.Connection.Id, r.Connection.UserIdFrom, r.Connection.UserIdTo })
            .ToListAsync();

        return results.Select(r => (r.Id, r.UserIdFrom, r.UserIdTo));
    }

    public async Task<int> GetUnreadMessageCountAsync(int roomId, int userId)
    {
        return await _context.Messages
            .Where(m => m.MessageRoomId == roomId && m.UserId != userId && m.Status == 0)
            .CountAsync();
    }

    public async Task<string?> GetConnectionStatusByUsersAsync(int userId1, int userId2)
    {
        // Find active (non-STORED) connection between the two users
        var conn = await _context.Connections
            .Where(c =>
                ((c.UserIdFrom == userId1 && c.UserIdTo == userId2) ||
                 (c.UserIdFrom == userId2 && c.UserIdTo == userId1)) &&
                c.Status != RecruitmentPlatform.Contracts.Enums.ConnectionStatus.STORED.ToString())
            .OrderByDescending(c => c.CreateAt)
            .FirstOrDefaultAsync();

        return conn?.Status;
    }
}
