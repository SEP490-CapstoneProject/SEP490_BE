using System;
using System.Linq;
using System.Threading.Tasks;
using Connection.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Connection.Infrastructure.Repositories;
using Connection.Application.Services;

namespace Connection.Tests;

public class ConnectionServiceTests
{
    [Fact]
    public async Task CreateMessage_UpdatesRoomLastMessAt()
    {
        var options = new DbContextOptionsBuilder<ConnectionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ConnectionDbContext(options);

        var connection = new Connection.Domain.Entities.Connection { UserIdFrom = 1, UserIdTo = 2, ProfileId = 10, CreateAt = DateTime.UtcNow };
        context.Connections.Add(connection);
        await context.SaveChangesAsync();

        var room = new Connection.Domain.Entities.Room { ConnectionId = connection.Id, CreatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var repo = new ConnectionRepository(context);
        var service = new ConnectionService(repo);

        var message = new Connection.Domain.Entities.Message
        {
            UserId = 1,
            MessageRoomId = room.Id,
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
            Status = 0
        };

        var created = await service.CreateMessageAsync(message);

        var updatedRoom = await context.Rooms.FindAsync(room.Id);
        Assert.NotNull(updatedRoom);
        Assert.Equal(created.CreatedAt, updatedRoom.LastMessAt);
    }
}
