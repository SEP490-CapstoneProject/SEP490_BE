using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.AspNetCore.SignalR;
using Connection.API.Hubs;
using Connection.Application.Interfaces;
using Connection.Domain.Entities;
using System.Threading;
using Connection.Application.DTOs;
using System.Security.Claims;

namespace Connection.Tests;

public class ChatHubTests
{
    [Fact]
    public async Task SendMessage_CallsServiceAndBroadcasts()
    {
        var mockService = new Mock<IConnectionService>();
        mockService.Setup(s => s.CreateMessageAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => { m.Id = 123; return m; });
        mockService.Setup(s => s.GetRoomByIdAsync(It.IsAny<int>())).ReturnsAsync(new Room { Id = 1, ConnectionId = 10 });
        mockService.Setup(s => s.GetRoomUsersAsync(It.IsAny<int>())).ReturnsAsync(new[] { (Id: 10, UserIdFrom: 1, UserIdTo: 2) });
        mockService.Setup(s => s.GetUnreadMessageCountAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(1);

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var mockPublisher = new Mock<IConnectionEventPublisher>();
        mockPublisher
            .Setup(p => p.PublishNewMessageNotificationAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<System.DateTime>(),
                It.IsAny<NotificationActorDto?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockResolver = new Mock<IUserProfileResolver>();
        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationActorDto { Id = 1, Name = "Alice", Avatar = "avatar.png", Role = "USER" });

        var hub = new ChatHub(mockService.Object, mockPublisher.Object, mockResolver.Object)
        {
            Clients = mockClients.Object,
            Context = CreateHubContext()
        };

        await hub.SendMessage(1, "hi");

        mockService.Verify(s => s.CreateMessageAsync(It.IsAny<Message>()), Times.Once);
        mockClients.Verify(c => c.Group("1"), Times.Once);
        mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static HubCallerContext CreateHubContext()
    {
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "1")],
            "test")));
        return context.Object;
    }
}
