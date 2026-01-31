namespace Platform.Engine.Interfaces;

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = false);
}
