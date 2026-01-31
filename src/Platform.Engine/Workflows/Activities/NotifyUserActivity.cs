namespace Platform.Engine.Workflows.Activities;

using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Platform.Core.Domain.Entities;
using Platform.Engine.Interfaces;

/// <summary>
/// Elsa activity for notifying users about job completion
/// </summary>
[Activity("Data Operations", "Notify User", Description = "Sends notification about job completion via email and SignalR")]
public class NotifyUserActivity : CodeActivity
{
    [Input(Description = "User ID to notify")]
    public Input<string> UserId { get; set; } = default!;
    
    [Input(Description = "Job ID")]
    public Input<string> JobId { get; set; } = default!;
    
    [Input(Description = "Download URL")]
    public Input<string?> DownloadUrl { get; set; } = default!;
    
    [Input(Description = "Job status (Completed/Failed)", DefaultValue = "Completed")]
    public Input<string> Status { get; set; } = new("Completed");
    
    [Input(Description = "Error message (if failed)")]
    public Input<string?> ErrorMessage { get; set; } = default!;
    
    [Input(Description = "Report title")]
    public Input<string?> ReportTitle { get; set; } = default!;
    
    [Input(Description = "Total rows processed")]
    public Input<long> TotalRows { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var notificationService = context.GetRequiredService<INotificationService>();
        
        var userId = UserId.Get(context);
        var jobId = JobId.Get(context);
        var status = Status.Get(context);
        var downloadUrl = DownloadUrl.Get(context);
        var errorMessage = ErrorMessage.Get(context);
        var reportTitle = ReportTitle.Get(context);
        var totalRows = TotalRows.Get(context);
        
        var isSuccess = status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        
        var notification = new Notification
        {
            UserId = userId,
            Title = isSuccess 
                ? "Report Ready for Download" 
                : "Report Generation Failed",
            Message = isSuccess
                ? $"Your report '{reportTitle ?? "Report"}' is ready with {totalRows:N0} rows. Click to download."
                : $"Report generation failed: {errorMessage}",
            Type = isSuccess ? NotificationType.Success : NotificationType.Error,
            ActionUrl = downloadUrl,
            CreatedAt = DateTime.UtcNow,
            DataJson = System.Text.Json.JsonSerializer.Serialize(new 
            {
                JobId = jobId,
                Status = status,
                TotalRows = totalRows
            })
        };
        
        // Send notification
        await notificationService.SendAsync(notification);
        
        // Send real-time SignalR notification
        await notificationService.SendSignalRAsync(userId, "JobCompleted", new
        {
            JobId = jobId,
            Status = status,
            DownloadUrl = downloadUrl,
            ErrorMessage = errorMessage,
            ReportTitle = reportTitle,
            TotalRows = totalRows
        });
    }
}
