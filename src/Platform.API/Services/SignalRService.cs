namespace Platform.API.Services;

using Microsoft.AspNetCore.SignalR;
using Platform.API.Hubs;
using Platform.Engine.Interfaces;
using Platform.Engine.Services;

/// <summary>
/// Service for sending SignalR notifications using the NotificationHub
/// </summary>
public class SignalRService : ISignalRService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(string userId, string eventName, object data)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Attempted to send SignalR message to empty userId");
                return;
            }

            await _hubContext.Clients.User(userId).SendAsync(eventName, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR message to user {UserId}", userId);
            // We don't throw here to avoid disrupting the flow if realtime notification fails
        }
    }

    public async Task SendToAllAsync(string eventName, object data)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(eventName, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR message to all users");
        }
    }
}
