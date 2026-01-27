namespace Platform.Engine.Services;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Platform.Engine.Workflows.Activities;

/// <summary>
/// Notification service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly ISignalRService _signalRService;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationService> _logger;
    
    public NotificationService(
        IEmailService emailService,
        ISignalRService signalRService,
        IRepository<Notification> notificationRepository,
        IOptions<NotificationOptions> options,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _signalRService = signalRService;
        _notificationRepository = notificationRepository;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task SendAsync(Notification notification)
    {
        try
        {
            // Save notification to database
            await _notificationRepository.AddAsync(notification);
            
            // Send email if enabled
            if (_options.EnableEmail)
            {
                await SendEmailNotificationAsync(notification);
            }
            
            _logger.LogInformation(
                "Notification sent to user {UserId}: {Title}",
                notification.UserId,
                notification.Title
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send notification to user {UserId}",
                notification.UserId
            );
            throw;
        }
    }
    
    public async Task SendSignalRAsync(string userId, string eventName, object data)
    {
        try
        {
            if (_options.EnableSignalR)
            {
                await _signalRService.SendToUserAsync(userId, eventName, data);
                
                _logger.LogDebug(
                    "SignalR notification sent to user {UserId}: {EventName}",
                    userId,
                    eventName
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR notification to user {UserId}",
                userId
            );
            // Don't throw - SignalR failures shouldn't break the workflow
        }
    }
    
    private async Task SendEmailNotificationAsync(Notification notification)
    {
        var emailTemplate = notification.Type switch
        {
            NotificationType.Success => _options.SuccessEmailTemplate,
            NotificationType.Error => _options.ErrorEmailTemplate,
            NotificationType.Warning => _options.WarningEmailTemplate,
            _ => _options.InfoEmailTemplate
        };
        
        var emailBody = emailTemplate
            .Replace("{Title}", notification.Title)
            .Replace("{Message}", notification.Message)
            .Replace("{ActionUrl}", notification.ActionUrl ?? "#");
        
        await _emailService.SendAsync(
            to: notification.UserId, // Assuming UserId is email
            subject: notification.Title,
            body: emailBody,
            isHtml: true
        );
    }
}

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = false);
}

/// <summary>
/// SignalR service interface
/// </summary>
public interface ISignalRService
{
    Task SendToUserAsync(string userId, string eventName, object data);
    Task SendToAllAsync(string eventName, object data);
}

/// <summary>
/// Notification configuration options
/// </summary>
public class NotificationOptions
{
    public const string SectionName = "Notifications";
    
    /// <summary>
    /// Enable email notifications
    /// </summary>
    public bool EnableEmail { get; set; } = true;
    
    /// <summary>
    /// Enable SignalR real-time notifications
    /// </summary>
    public bool EnableSignalR { get; set; } = true;
    
    /// <summary>
    /// Email template for success notifications
    /// </summary>
    public string SuccessEmailTemplate { get; set; } = @"
        <h2>{Title}</h2>
        <p>{Message}</p>
        <a href='{ActionUrl}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
            Download Report
        </a>
    ";
    
    /// <summary>
    /// Email template for error notifications
    /// </summary>
    public string ErrorEmailTemplate { get; set; } = @"
        <h2 style='color: #f44336;'>{Title}</h2>
        <p>{Message}</p>
    ";
    
    /// <summary>
    /// Email template for warning notifications
    /// </summary>
    public string WarningEmailTemplate { get; set; } = @"
        <h2 style='color: #ff9800;'>{Title}</h2>
        <p>{Message}</p>
    ";
    
    /// <summary>
    /// Email template for info notifications
    /// </summary>
    public string InfoEmailTemplate { get; set; } = @"
        <h2>{Title}</h2>
        <p>{Message}</p>
    ";
}
