using System.Collections.Generic;

namespace Platform.Engine.Models;

public class ConnectorMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ConnectorParameter> Inputs { get; set; } = new();
    public List<ConnectorParameter> Outputs { get; set; } = new();
    
    // The actual business logic provided by the user
    public string BusinessLogic { get; set; } = string.Empty;
    
    // Configuration properties (e.g., SMTP Host, ApiKey) stored as Metadata
    public List<ConnectorProperty> ConfigProperties { get; set; } = new();
}

public class ConnectorParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, int, decimal, object
}

public class ConnectorProperty
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? DefaultValue { get; set; }
}
