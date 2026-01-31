namespace Platform.Engine.Interfaces;

using Platform.Core.Domain.Entities;

/// <summary>
/// Interface for notification service
/// </summary>
public interface INotificationService
{
    Task SendAsync(Notification notification);
    Task SendSignalRAsync(string userId, string eventName, object data);
}
