namespace Platform.Engine.Services.DataExecution;

using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;
using Platform.Core.Domain.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;

/// <summary>
/// Orchestrates data operation execution across different providers
/// </summary>
public class DataExecutionEngine
{
    private readonly IEnumerable<IDataProvider> _providers;
    private readonly IEnumerable<IOutputGenerator> _outputGenerators;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IJobTrackingService _jobTracking;
    private readonly IMetadataNormalizationService _normalizationService;
    private const int QuickJobTimeoutSeconds = 30;
    private const int QuickJobMaxRows = 10000;
    
    public DataExecutionEngine(
        IEnumerable<IDataProvider> providers,
        IEnumerable<IOutputGenerator> outputGenerators,
        IWorkflowRuntime workflowRuntime,
        IJobTrackingService jobTracking,
        IMetadataNormalizationService normalizationService)
    {
        _providers = providers;
        _outputGenerators = outputGenerators;
        _workflowRuntime = workflowRuntime;
        _jobTracking = jobTracking;
        _normalizationService = normalizationService;
    }
    
    public async Task<DataResult> ExecuteQuickJobAsync(
        string providerType,
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        string outputFormat = "JSON",
        Guid? projectId = null)
    {
        // Apply Metadata Virtualization (Normalization)
        if (projectId.HasValue)
        {
            await _normalizationService.NormalizeAsync(projectId.Value, metadata);
        }

        // Select appropriate provider
        var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider '{providerType}' not found");
        }
        
        // Validate metadata
        var validation = await provider.ValidateAsync(metadata);
        if (!validation.IsValid)
        {
            throw new ValidationException(
                $"Validation failed: {string.Join(", ", validation.Errors.Select(e => e.Message))}"
            );
        }
        
        // Check estimated row count
        var estimatedRows = await provider.EstimateRowCountAsync(metadata, parameters, context);
        if (estimatedRows > QuickJobMaxRows)
        {
            throw new InvalidOperationException(
                $"Result set too large for Quick Job ({estimatedRows} rows). " +
                $"Use Long-Running mode or add filters to reduce data volume."
            );
        }
        
        // Execute with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QuickJobTimeoutSeconds));
        
        try
        {
            var result = await provider.ExecuteAsync(metadata, parameters, context, cts.Token);
            
            // Virtualize Result (Shadow Properties)
            if (projectId.HasValue && !string.IsNullOrEmpty(metadata.RootEntity))
            {
                _normalizationService.VirtualizeResult(projectId.Value, result, metadata.RootEntity);
            }

            // Generate output format if not JSON
            if (outputFormat != "JSON" && result.Success && result.Data != null)
            {
                var generator = _outputGenerators.FirstOrDefault(g => g.Format == outputFormat);
                if (generator != null)
                {
                    var dataEnumerable = result.Data as IEnumerable<object> ?? new[] { result.Data };
                    result.Output = await generator.GenerateAsync(
                        dataEnumerable,
                        new OutputOptions { Format = outputFormat },
                        cts.Token
                    );
                }
            }
            
            return result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Query exceeded {QuickJobTimeoutSeconds} second limit. " +
                "Convert to Long-Running Job or optimize your query."
            );
        }
    }
    
    public async Task<string> QueueLongRunningJobAsync(
        string providerType,
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        string outputFormat = "Excel",
        string? reportTitle = null,
        Guid? projectId = null)
    {
        // Apply Metadata Virtualization
        if (projectId.HasValue)
        {
             await _normalizationService.NormalizeAsync(projectId.Value, metadata);
        }

        // Select appropriate provider
        var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider '{providerType}' not found");
        }
        
        // Validate metadata
        var validation = await provider.ValidateAsync(metadata);
        if (!validation.IsValid)
        {
            throw new ValidationException(
                $"Validation failed: {string.Join(", ", validation.Errors.Select(e => e.Message))}"
            );
        }
        
        // Create job ID
        var jobId = Guid.NewGuid().ToString();
        
        // Start Elsa 3 workflow
        var workflowInput = new Dictionary<string, object>
        {
            ["JobId"] = jobId,
            ["Metadata"] = metadata,
            ["Parameters"] = parameters,
            ["Context"] = context,
            ["ProviderType"] = providerType,
            ["OutputFormat"] = outputFormat,
            ["ReportTitle"] = reportTitle ?? "Report",
            ["UserId"] = context.UserId,
            ["ChunkSize"] = 1000,
            ["ContainerName"] = "reports",
            ["IncludeHeaders"] = true
        };
        
        var result = await _workflowRuntime.StartWorkflowAsync(
            "LongRunningReportWorkflow",
            new StartWorkflowRuntimeParams
            {
                Input = workflowInput
            });
        
        // Track job
        await _jobTracking.CreateJobAsync(new JobInstance
        {
            JobId = jobId,
            UserId = context.UserId,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            WorkflowInstanceId = result.WorkflowInstanceId,
            ReportTitle = reportTitle,
            OutputFormat = outputFormat
        });
        
        return jobId;
    }
}

/// <summary>
/// Custom exception for validation errors
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
