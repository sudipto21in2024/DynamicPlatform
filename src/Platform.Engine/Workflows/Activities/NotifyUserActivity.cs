namespace Platform.Engine.Workflows.Activities;

using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

/// <summary>
/// Elsa activity for notifying users about job completion
/// </summary>
[Activity(
    Category = "Data Operations",
    DisplayName = "Notify User",
    Description = "Sends notification about job completion via email and SignalR"
)]
public class NotifyUserActivity : Activity
{
    private readonly INotificationService _notificationService;
    
    [ActivityInput(Hint = "User ID to notify")]
    public string UserId { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Job ID")]
    public string JobId { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Download URL")]
    public string? DownloadUrl { get; set; }
    
    [ActivityInput(Hint = "Job status (Completed/Failed)", DefaultValue = "Completed")]
    public string Status { get; set; } = "Completed";
    
    [ActivityInput(Hint = "Error message (if failed)")]
    public string? ErrorMessage { get; set; }
    
    [ActivityInput(Hint = "Report title")]
    public string? ReportTitle { get; set; }
    
    [ActivityInput(Hint = "Total rows processed")]
    public long TotalRows { get; set; }
    
    public NotifyUserActivity(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var isSuccess = Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        
        var notification = new Notification
        {
            UserId = UserId,
            Title = isSuccess 
                ? "Report Ready for Download" 
                : "Report Generation Failed",
            Message = isSuccess
                ? $"Your report '{ReportTitle ?? "Report"}' is ready with {TotalRows:N0} rows. Click to download."
                : $"Report generation failed: {ErrorMessage}",
            Type = isSuccess ? NotificationType.Success : NotificationType.Error,
            ActionUrl = DownloadUrl,
            CreatedAt = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                ["JobId"] = JobId,
                ["Status"] = Status,
                ["TotalRows"] = TotalRows
            }
        };
        
        // Send notification
        await _notificationService.SendAsync(notification);
        
        // Send real-time SignalR notification
        await _notificationService.SendSignalRAsync(UserId, "JobCompleted", new
        {
            JobId,
            Status,
            DownloadUrl,
            ErrorMessage,
            ReportTitle,
            TotalRows
        });
        
        return Done();
    }
}

/// <summary>
/// Notification model
/// </summary>
public class Notification
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Notification type enum
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Interface for notification service
/// </summary>
public interface INotificationService
{
    Task SendAsync(Notification notification);
    Task SendSignalRAsync(string userId, string eventName, object data);
}
