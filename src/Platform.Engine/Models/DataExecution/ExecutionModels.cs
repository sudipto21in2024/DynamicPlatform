namespace Platform.Engine.Models.DataExecution;

/// <summary>
/// Execution context containing user and tenant information
/// </summary>
public class ExecutionContext
{
    public string UserId { get; set; } = string.Empty;
    
    public string TenantId { get; set; } = string.Empty;
    
    public string UserTimezone { get; set; } = "UTC";
    
    public string ServerTimezone { get; set; } = "UTC";
    
    public Dictionary<string, object>? AdditionalContext { get; set; }
}

/// <summary>
/// Result of a data operation execution
/// </summary>
public class DataResult
{
    public bool Success { get; set; }
    
    public object? Data { get; set; }
    
    public int RowCount { get; set; }
    
    public double ExecutionTimeSeconds { get; set; }
    
    public List<ColumnMetadata>? Columns { get; set; }
    
    public Stream? Output { get; set; } // For file outputs
    
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Metadata about a column in the result set
/// </summary>
public class ColumnMetadata
{
    public string Name { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    
    public bool IsNullable { get; set; }
}

/// <summary>
/// Validation result for metadata
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    
    public List<ValidationError> Errors { get; set; } = new();
}

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public List<string>? Suggestions { get; set; }
}

/// <summary>
/// Options for output generation
/// </summary>
public class OutputOptions
{
    public string Format { get; set; } = "JSON";
    
    public bool IncludeHeaders { get; set; } = true;
    
    public string? Title { get; set; }
    
    public Dictionary<string, object>? CustomOptions { get; set; }
}
