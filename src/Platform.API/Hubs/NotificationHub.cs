namespace Platform.API.Hubs;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// SignalR hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User connected to NotificationHub: {UserId}", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation(exception, "User disconnected from NotificationHub: {UserId}", userId);
        await base.OnDisconnectedAsync(exception);
    }
}
