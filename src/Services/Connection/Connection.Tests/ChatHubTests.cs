using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.AspNetCore.SignalR;
using Connection.API.Hubs;
using Connection.Application.Interfaces;
using Connection.Domain.Entities;
using System.Threading;

namespace Connection.Tests;

public class ChatHubTests
{
    [Fact]
    public async Task SendMessage_CallsServiceAndBroadcasts()
    {
        var mockService = new Mock<IConnectionService>();
        mockService.Setup(s => s.CreateMessageAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => { m.Id = 123; return m; });

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var hub = new ChatHub(mockService.Object)
        {
            Clients = mockClients.Object
        };

        await hub.SendMessage(1, "hi", 1);

        mockService.Verify(s => s.CreateMessageAsync(It.IsAny<Message>()), Times.Once);
        mockClients.Verify(c => c.Group("1"), Times.Once);
        mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
