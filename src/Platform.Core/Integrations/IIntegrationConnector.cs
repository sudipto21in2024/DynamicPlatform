using System.Threading.Tasks;

namespace Platform.Core.Integrations;

public interface IIntegrationConnector
{
    string Name { get; }
    string Provider { get; } // e.g. "Stripe", "SendGrid"
    Task<IntegrationResult> ExecuteAsync(IntegrationContext context);
}

public class IntegrationContext
{
    public string Action { get; set; } = string.Empty;
    public object? Payload { get; set; }
}

public class IntegrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}
