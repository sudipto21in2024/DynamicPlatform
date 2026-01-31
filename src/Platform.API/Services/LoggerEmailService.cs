namespace Platform.API.Services;

using Platform.Engine.Interfaces;
using Platform.Engine.Services;

/// <summary>
/// Basic email service logging to console/logger for development
/// </summary>
public class LoggerEmailService : IEmailService
{
    private readonly ILogger<LoggerEmailService> _logger;

    public LoggerEmailService(ILogger<LoggerEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, bool isHtml = false)
    {
        _logger.LogInformation("📧 [MOCK EMAIL] To: {To}, Subject: {Subject}", to, subject);
        _logger.LogDebug("Body: {Body}", body);
        
        // In a real implementation, you would use SmtpClient or SendGrid here
        return Task.CompletedTask;
    }
}
