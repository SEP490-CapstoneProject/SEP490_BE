using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Realtime.API.Hubs;

[Authorize]
public class RealtimeHub : Hub
{
    private readonly ILogger<RealtimeHub> _logger;

    public RealtimeHub(ILogger<RealtimeHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier
                     ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("nameid")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("Realtime connected. ConnectionId={ConnectionId}, UserId={UserId}", Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public Task JoinPost(string postId)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw new HubException("postId is required.");
        }

        _logger.LogInformation("Join post group. ConnectionId={ConnectionId}, PostId={PostId}", Context.ConnectionId, postId);
        return Groups.AddToGroupAsync(Context.ConnectionId, $"post_{postId}");
    }

    public Task LeavePost(string postId)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw new HubException("postId is required.");
        }

        _logger.LogInformation("Leave post group. ConnectionId={ConnectionId}, PostId={PostId}", Context.ConnectionId, postId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post_{postId}");
    }
}
