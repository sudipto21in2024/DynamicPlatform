namespace Platform.Core.Domain.Entities;

/// <summary>
/// Entity for storing report definitions
/// </summary>
public class ReportDefinition
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public ReportType Type { get; set; }
    
    public ExecutionMode ExecutionMode { get; set; }
    
    public string MetadataJson { get; set; } = string.Empty; // Serialized DataOperationMetadata
    
    public string OutputFormat { get; set; } = "JSON";
    
    public string? ParametersJson { get; set; } // Serialized parameter definitions
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public enum ReportType
{
    Visual,
    Code
}

public enum ExecutionMode
{
    Quick,
    LongRunning
}

/// <summary>
/// Entity for tracking job execution instances
/// </summary>
public class JobInstance
{
    public Guid Id { get; set; }
    
    public string JobId { get; set; } = string.Empty;
    
    public Guid? ReportDefinitionId { get; set; }
    
    public ReportDefinition? ReportDefinition { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public string? WorkflowInstanceId { get; set; } // Link to Elsa workflow
    
    public JobStatus Status { get; set; }
    
    public string? OutputFormat { get; set; }
    
    public string? ReportTitle { get; set; }
    
    public int Progress { get; set; } // 0-100
    
    public long RowsProcessed { get; set; }
    
    public long TotalRows { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime? EstimatedCompletion { get; set; }
    
    public string? DownloadUrl { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Entity for user notifications
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; }
    
    public string? ActionUrl { get; set; }
    
    public bool IsRead { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string? DataJson { get; set; } // Serialized extra data
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
