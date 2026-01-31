namespace Platform.Engine.Interfaces;

/// <summary>
/// SignalR service interface
/// </summary>
public interface ISignalRService
{
    Task SendToUserAsync(string userId, string eventName, object data);
    Task SendToAllAsync(string eventName, object data);
}
