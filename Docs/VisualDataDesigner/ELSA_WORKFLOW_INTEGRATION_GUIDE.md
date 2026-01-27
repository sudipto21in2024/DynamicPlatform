# Visual Data Designer: Elsa Workflow Integration Guide

## 1. Overview

This guide explains how to integrate **Elsa Workflow** with the Visual Data Designer for executing long-running data operations. Using Elsa provides superior workflow orchestration, built-in state management, retry logic, and visual monitoring compared to traditional background job systems.

---

## 2. Architecture

### 2.1. Execution Flow

```
User Request
    ↓
DataExecutionEngine (decides Quick vs Long-Running)
    ↓
[Quick Job] → Execute immediately (30s timeout)
    ↓
[Long-Running Job] → Start Elsa Workflow
    ↓
Elsa Workflow Instance
    ├─→ ExecuteDataQueryActivity (chunked processing)
    ├─→ GenerateReportOutputActivity (Excel/PDF/CSV)
    ├─→ UploadToStorageActivity (Blob storage)
    └─→ NotifyUserActivity (Email/SignalR)
```

### 2.2. Key Components

| Component | Purpose |
|-----------|---------|
| **DataExecutionEngine** | Orchestrates execution, decides Quick vs Long-Running |
| **Elsa Custom Activities** | Reusable workflow steps for data operations |
| **Workflow Definitions** | Pre-built workflows for common scenarios |
| **Job Tracking Service** | Maps JobId to Elsa WorkflowInstanceId |
| **Elsa Studio** | Visual monitoring and debugging |

---

## 3. Custom Elsa Activities

### 3.1. ExecuteDataQueryActivity

**Purpose**: Executes the data query with chunked processing for large datasets

**Location**: `Platform.Engine/Workflows/Activities/ExecuteDataQueryActivity.cs`

**Implementation**:

```csharp
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

namespace Platform.Engine.Workflows.Activities;

[Activity(
    Category = "Data Operations",
    DisplayName = "Execute Data Query",
    Description = "Executes a data query with chunked processing for large datasets"
)]
public class ExecuteDataQueryActivity : Activity
{
    private readonly IDataProvider _entityProvider;
    
    [ActivityInput(Hint = "The data operation metadata")]
    public DataOperationMetadata Metadata { get; set; } = null!;
    
    [ActivityInput(Hint = "Query parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    [ActivityInput(Hint = "Execution context")]
    public ExecutionContext Context { get; set; } = null!;
    
    [ActivityInput(Hint = "Chunk size for processing", DefaultValue = 1000)]
    public int ChunkSize { get; set; } = 1000;
    
    [ActivityOutput]
    public List<object> ResultData { get; set; } = new();
    
    [ActivityOutput]
    public long TotalRows { get; set; }
    
    public ExecuteDataQueryActivity(IEnumerable<IDataProvider> providers)
    {
        _entityProvider = providers.First(p => p.ProviderType == "Entity");
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var allData = new List<object>();
        var offset = 0;
        
        // Estimate total rows
        TotalRows = await _entityProvider.EstimateRowCountAsync(Metadata, Parameters, Context);
        
        // Process in chunks
        while (true)
        {
            // Update metadata for current chunk
            var chunkMetadata = CloneMetadata(Metadata);
            chunkMetadata.Limit = ChunkSize;
            chunkMetadata.Offset = offset;
            
            // Execute chunk
            var result = await _entityProvider.ExecuteAsync(
                chunkMetadata,
                Parameters,
                Context,
                context.CancellationToken
            );
            
            if (!result.Success || result.Data == null)
                break;
            
            var chunkData = result.Data as IEnumerable<object> ?? new[] { result.Data };
            var chunkList = chunkData.ToList();
            
            if (!chunkList.Any())
                break;
            
            allData.AddRange(chunkList);
            
            // Update progress
            var progress = (int)((offset + chunkList.Count) * 100.0 / TotalRows);
            context.SetVariable("Progress", progress);
            context.SetVariable("RowsProcessed", offset + chunkList.Count);
            
            offset += ChunkSize;
            
            // Check if we've processed all data
            if (chunkList.Count < ChunkSize)
                break;
        }
        
        ResultData = allData;
        
        return Done();
    }
    
    private DataOperationMetadata CloneMetadata(DataOperationMetadata original)
    {
        // Deep clone implementation
        return new DataOperationMetadata
        {
            OperationType = original.OperationType,
            RootEntity = original.RootEntity,
            Fields = original.Fields,
            Joins = original.Joins,
            Filters = original.Filters,
            OrderBy = original.OrderBy
        };
    }
}
```

---

### 3.2. GenerateReportOutputActivity

**Purpose**: Generates output file in specified format (Excel, PDF, CSV)

**Location**: `Platform.Engine/Workflows/Activities/GenerateReportOutputActivity.cs`

**Implementation**:

```csharp
[Activity(
    Category = "Data Operations",
    DisplayName = "Generate Report Output",
    Description = "Generates report in Excel, PDF, or CSV format"
)]
public class GenerateReportOutputActivity : Activity
{
    private readonly IEnumerable<IOutputGenerator> _generators;
    
    [ActivityInput(Hint = "Data to generate report from")]
    public List<object> Data { get; set; } = new();
    
    [ActivityInput(Hint = "Output format (Excel, PDF, CSV)")]
    public string OutputFormat { get; set; } = "Excel";
    
    [ActivityInput(Hint = "Report title")]
    public string? Title { get; set; }
    
    [ActivityOutput]
    public Stream OutputFile { get; set; } = null!;
    
    [ActivityOutput]
    public string FileName { get; set; } = string.Empty;
    
    public GenerateReportOutputActivity(IEnumerable<IOutputGenerator> generators)
    {
        _generators = generators;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var generator = _generators.FirstOrDefault(g => g.Format == OutputFormat);
        if (generator == null)
        {
            throw new InvalidOperationException($"No generator found for format: {OutputFormat}");
        }
        
        var options = new OutputOptions
        {
            Format = OutputFormat,
            Title = Title,
            IncludeHeaders = true
        };
        
        OutputFile = await generator.GenerateAsync(Data, options, context.CancellationToken);
        
        var extension = OutputFormat.ToLower() switch
        {
            "excel" => "xlsx",
            "pdf" => "pdf",
            "csv" => "csv",
            _ => "bin"
        };
        
        FileName = $"report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";
        
        return Done();
    }
}
```

---

### 3.3. UploadToStorageActivity

**Purpose**: Uploads generated file to blob storage

**Location**: `Platform.Engine/Workflows/Activities/UploadToStorageActivity.cs`

**Implementation**:

```csharp
[Activity(
    Category = "Data Operations",
    DisplayName = "Upload to Storage",
    Description = "Uploads file to blob storage and returns download URL"
)]
public class UploadToStorageActivity : Activity
{
    private readonly IBlobStorageService _storageService;
    
    [ActivityInput(Hint = "File stream to upload")]
    public Stream FileStream { get; set; } = null!;
    
    [ActivityInput(Hint = "File name")]
    public string FileName { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Job ID")]
    public string JobId { get; set; } = string.Empty;
    
    [ActivityOutput]
    public string DownloadUrl { get; set; } = string.Empty;
    
    public UploadToStorageActivity(IBlobStorageService storageService)
    {
        _storageService = storageService;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var containerName = "reports";
        var blobPath = $"{JobId}/{FileName}";
        
        DownloadUrl = await _storageService.UploadAsync(
            containerName,
            blobPath,
            FileStream,
            context.CancellationToken
        );
        
        return Done();
    }
}
```

---

### 3.4. NotifyUserActivity

**Purpose**: Sends notification to user about job completion

**Location**: `Platform.Engine/Workflows/Activities/NotifyUserActivity.cs`

**Implementation**:

```csharp
[Activity(
    Category = "Data Operations",
    DisplayName = "Notify User",
    Description = "Sends notification about job completion"
)]
public class NotifyUserActivity : Activity
{
    private readonly INotificationService _notificationService;
    
    [ActivityInput(Hint = "User ID to notify")]
    public string UserId { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Job ID")]
    public string JobId { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Download URL")]
    public string DownloadUrl { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Job status (Completed/Failed)")]
    public string Status { get; set; } = "Completed";
    
    [ActivityInput(Hint = "Error message (if failed)")]
    public string? ErrorMessage { get; set; }
    
    public NotifyUserActivity(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var notification = new Notification
        {
            UserId = UserId,
            Title = Status == "Completed" 
                ? "Report Ready for Download" 
                : "Report Generation Failed",
            Message = Status == "Completed"
                ? $"Your report is ready. Click to download."
                : $"Report generation failed: {ErrorMessage}",
            Type = Status == "Completed" ? "Success" : "Error",
            ActionUrl = DownloadUrl,
            CreatedAt = DateTime.UtcNow
        };
        
        await _notificationService.SendAsync(notification);
        
        // Also send SignalR notification for real-time update
        await _notificationService.SendSignalRAsync(UserId, "JobCompleted", new
        {
            JobId,
            Status,
            DownloadUrl,
            ErrorMessage
        });
        
        return Done();
    }
}
```

---

## 4. Workflow Definitions

### 4.1. Long-Running Report Workflow

**Location**: `Platform.Engine/Workflows/Definitions/LongRunningReportWorkflow.cs`

```csharp
using Elsa.Builders;

namespace Platform.Engine.Workflows.Definitions;

public class LongRunningReportWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .WithDisplayName("Long-Running Report Generation")
            .WithDescription("Executes data query, generates report, and notifies user")
            
            // Step 1: Execute data query with chunked processing
            .StartWith<ExecuteDataQueryActivity>(activity =>
            {
                activity.Metadata = new Input<DataOperationMetadata>(context => 
                    context.GetVariable<DataOperationMetadata>("Metadata")!);
                activity.Parameters = new Input<Dictionary<string, object>>(context => 
                    context.GetVariable<Dictionary<string, object>>("Parameters")!);
                activity.Context = new Input<ExecutionContext>(context => 
                    context.GetVariable<ExecutionContext>("Context")!);
            })
            .WithName("ExecuteQuery")
            
            // Step 2: Generate output file
            .Then<GenerateReportOutputActivity>(activity =>
            {
                activity.Data = new Input<List<object>>(context => 
                    context.GetVariable<List<object>>("ExecuteQuery", "ResultData")!);
                activity.OutputFormat = new Input<string>(context => 
                    context.GetVariable<string>("OutputFormat")!);
                activity.Title = new Input<string?>(context => 
                    context.GetVariable<string>("ReportTitle"));
            })
            .WithName("GenerateOutput")
            
            // Step 3: Upload to storage
            .Then<UploadToStorageActivity>(activity =>
            {
                activity.FileStream = new Input<Stream>(context => 
                    context.GetVariable<Stream>("GenerateOutput", "OutputFile")!);
                activity.FileName = new Input<string>(context => 
                    context.GetVariable<string>("GenerateOutput", "FileName")!);
                activity.JobId = new Input<string>(context => 
                    context.GetVariable<string>("JobId")!);
            })
            .WithName("UploadFile")
            
            // Step 4: Notify user of completion
            .Then<NotifyUserActivity>(activity =>
            {
                activity.UserId = new Input<string>(context => 
                    context.GetVariable<string>("UserId")!);
                activity.JobId = new Input<string>(context => 
                    context.GetVariable<string>("JobId")!);
                activity.DownloadUrl = new Input<string>(context => 
                    context.GetVariable<string>("UploadFile", "DownloadUrl")!);
                activity.Status = new Input<string>("Completed");
            })
            .WithName("NotifyUser");
    }
}
```

---

## 5. DataExecutionEngine Integration

### 5.1. Updated Implementation

**Location**: `Platform.Engine/Services/DataExecution/DataExecutionEngine.cs`

```csharp
public class DataExecutionEngine
{
    private readonly IEnumerable<IDataProvider> _providers;
    private readonly IEnumerable<IOutputGenerator> _outputGenerators;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IJobTrackingService _jobTracking;
    
    public async Task<string> QueueLongRunningJobAsync(
        string providerType,
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        string outputFormat = "Excel",
        string? reportTitle = null)
    {
        // Validate
        var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
        if (provider == null)
            throw new InvalidOperationException($"Provider '{providerType}' not found");
        
        var validation = await provider.ValidateAsync(metadata);
        if (!validation.IsValid)
            throw new ValidationException($"Validation failed: {string.Join(", ", validation.Errors.Select(e => e.Message))}");
        
        // Generate job ID
        var jobId = Guid.NewGuid().ToString();
        
        // Start Elsa workflow
        var workflowInput = new
        {
            JobId = jobId,
            Metadata = metadata,
            Parameters = parameters,
            Context = context,
            OutputFormat = outputFormat,
            ReportTitle = reportTitle,
            UserId = context.UserId
        };
        
        var workflowInstance = await _workflowRunner.RunWorkflowAsync(
            "LongRunningReportWorkflow",
            workflowInput
        );
        
        // Track job
        await _jobTracking.CreateJobAsync(new JobInstance
        {
            JobId = jobId,
            UserId = context.UserId,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            WorkflowInstanceId = workflowInstance.Id
        });
        
        return jobId;
    }
}
```

---

## 6. Job Tracking Service

### 6.1. Interface

**Location**: `Platform.Engine/Interfaces/IJobTrackingService.cs`

```csharp
public interface IJobTrackingService
{
    Task CreateJobAsync(JobInstance job);
    Task<JobInstance?> GetJobAsync(string jobId);
    Task UpdateProgressAsync(string jobId, int progress, long rowsProcessed);
    Task CompleteJobAsync(string jobId, string downloadUrl);
    Task FailJobAsync(string jobId, string errorMessage);
}
```

### 6.2. Implementation

**Location**: `Platform.Engine/Services/JobTrackingService.cs`

```csharp
public class JobTrackingService : IJobTrackingService
{
    private readonly IWorkflowInstanceStore _workflowStore;
    private readonly IRepository<JobInstance> _jobRepository;
    
    public async Task<JobInstance?> GetJobAsync(string jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;
        
        // Get workflow instance for current status
        var workflowInstance = await _workflowStore.FindByIdAsync(job.WorkflowInstanceId);
        if (workflowInstance != null)
        {
            job.Status = workflowInstance.WorkflowStatus switch
            {
                WorkflowStatus.Running => JobStatus.Running,
                WorkflowStatus.Finished => JobStatus.Completed,
                WorkflowStatus.Faulted => JobStatus.Failed,
                _ => JobStatus.Queued
            };
            
            // Get progress from workflow variables
            if (workflowInstance.Variables.TryGetValue("Progress", out var progress))
            {
                job.Progress = Convert.ToInt32(progress);
            }
            
            if (workflowInstance.Variables.TryGetValue("RowsProcessed", out var rows))
            {
                job.RowsProcessed = Convert.ToInt64(rows);
            }
        }
        
        return job;
    }
    
    public async Task UpdateProgressAsync(string jobId, int progress, long rowsProcessed)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Progress = progress;
            job.RowsProcessed = rowsProcessed;
            await _jobRepository.UpdateAsync(job);
        }
    }
}
```

---

## 7. Error Handling & Retry

### 7.1. Workflow-Level Error Handling

Add fault handling to workflow:

```csharp
public void Build(IWorkflowBuilder builder)
{
    builder
        .StartWith<ExecuteDataQueryActivity>()
        .WithName("ExecuteQuery")
        
        .Then<GenerateReportOutputActivity>()
        .WithName("GenerateOutput")
        
        // Add fault handler
        .When(OutcomeNames.Done)
        .Then<UploadToStorageActivity>()
        .WithName("UploadFile")
        
        .When(OutcomeNames.Done)
        .Then<NotifyUserActivity>()
        .WithName("NotifySuccess")
        
        // Handle faults
        .Add<Fault>(fault =>
        {
            fault.When(OutcomeNames.Fault)
                .Then<NotifyUserActivity>(activity =>
                {
                    activity.Status = new Input<string>("Failed");
                    activity.ErrorMessage = new Input<string?>(context => 
                        context.GetVariable<string>("Fault", "Message"));
                })
                .WithName("NotifyFailure");
        });
}
```

### 7.2. Activity-Level Retry

Add retry policy to activities:

```csharp
[Activity(
    Category = "Data Operations",
    DisplayName = "Execute Data Query"
)]
[RetryPolicy(MaxRetries = 3, RetryDelaySeconds = 5)]
public class ExecuteDataQueryActivity : Activity
{
    // Implementation
}
```

---

## 8. Monitoring & Debugging

### 8.1. Elsa Studio Integration

Access workflow monitoring at: `/elsa/workflows`

Features:
- View running workflow instances
- See current activity execution
- Inspect workflow variables
- View execution history
- Manually retry failed workflows

### 8.2. Custom Dashboard

Create a job monitoring dashboard:

```typescript
// Frontend component
export class JobMonitorComponent {
    async getJobStatus(jobId: string) {
        const response = await fetch(`/api/data/jobs/${jobId}/status`);
        return await response.json();
    }
    
    async getWorkflowInstance(workflowInstanceId: string) {
        const response = await fetch(`/api/elsa/workflow-instances/${workflowInstanceId}`);
        return await response.json();
    }
}
```

---

## 9. Scheduling Reports

### 9.1. Timer-Triggered Workflows

Create scheduled report workflow:

```csharp
public class ScheduledReportWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .WithDisplayName("Scheduled Daily Report")
            
            // Trigger daily at 6 AM
            .StartWith<TimerEvent>(timer =>
            {
                timer.TimeoutExpression = new Input<TimeSpan>("0.06:00:00");
                timer.Mode = TimerEventMode.Interval;
            })
            
            // Execute report workflow
            .Then<RunWorkflow>(run =>
            {
                run.WorkflowDefinitionId = new Input<string>("LongRunningReportWorkflow");
                run.Input = new Input<object>(context => new
                {
                    Metadata = GetDailyReportMetadata(),
                    OutputFormat = "Excel"
                });
            });
    }
}
```

---

## 10. Best Practices

### 10.1. Chunking Strategy

- Default chunk size: **1000 rows**
- Adjust based on data complexity
- Monitor memory usage
- Use streaming for very large datasets

### 10.2. Timeout Configuration

```csharp
[Activity(Timeout = "00:30:00")] // 30 minutes max per activity
public class ExecuteDataQueryActivity : Activity
{
    // Implementation
}
```

### 10.3. Resource Management

- Dispose streams properly
- Clear large variables after use
- Use `IAsyncDisposable` for async cleanup

### 10.4. Progress Reporting

Update progress every 100 rows:

```csharp
if (offset % 100 == 0)
{
    context.SetVariable("Progress", (int)(offset * 100.0 / TotalRows));
    await context.WorkflowExecutionContext.WorkflowInstance.SaveAsync();
}
```

---

## 11. Testing

### 11.1. Unit Testing Activities

```csharp
[Fact]
public async Task ExecuteDataQueryActivity_ProcessesInChunks()
{
    // Arrange
    var activity = new ExecuteDataQueryActivity(mockProviders);
    var context = new ActivityExecutionContext(...);
    
    // Act
    var result = await activity.ExecuteAsync(context);
    
    // Assert
    Assert.Equal(1000, activity.ResultData.Count);
}
```

### 11.2. Integration Testing Workflows

```csharp
[Fact]
public async Task LongRunningReportWorkflow_CompletesSuccessfully()
{
    // Arrange
    var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();
    
    // Act
    var instance = await workflowRunner.RunWorkflowAsync(
        "LongRunningReportWorkflow",
        testInput
    );
    
    // Assert
    Assert.Equal(WorkflowStatus.Finished, instance.WorkflowStatus);
}
```

---

## 12. Summary

Using Elsa Workflow for long-running data operations provides:

✅ **Built-in state management** - No need for separate job tracking  
✅ **Visual monitoring** - See execution in Elsa Studio  
✅ **Retry & compensation** - Automatic error handling  
✅ **Scheduling** - Timer-based report generation  
✅ **Scalability** - Distributed execution support  
✅ **Consistency** - Unified workflow platform  

This approach leverages your existing Elsa infrastructure and provides a more robust, maintainable solution than traditional background job systems.
