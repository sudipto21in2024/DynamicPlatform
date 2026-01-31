using System;
using System.Collections.Generic;

namespace Platform.Engine.Models.Connectivity;

public class ConnectorExecutionRequest
{
    public string ConnectorName { get; set; } = string.Empty;
    public IDictionary<string, object?> Inputs { get; set; } = new Dictionary<string, object?>();
    public IDictionary<string, object?>? ConfigurationOverrides { get; set; }
}

public class ConnectorExecutionResult
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public double ExecutionTimeMs { get; set; }
}
