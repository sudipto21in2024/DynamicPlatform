namespace Platform.Engine.Workflows.Activities;

using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Elsa activity for executing data queries with chunked processing
/// </summary>
[Activity(
    Category = "Data Operations",
    DisplayName = "Execute Data Query",
    Description = "Executes a data query with chunked processing for large datasets"
)]
public class ExecuteDataQueryActivity : Activity
{
    private readonly IEnumerable<IDataProvider> _providers;
    
    [ActivityInput(Hint = "The data operation metadata")]
    public DataOperationMetadata Metadata { get; set; } = null!;
    
    [ActivityInput(Hint = "Query parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    [ActivityInput(Hint = "Execution context")]
    public ExecutionContext Context { get; set; } = null!;
    
    [ActivityInput(Hint = "Provider type (Entity, API, Workflow, Static)", DefaultValue = "Entity")]
    public string ProviderType { get; set; } = "Entity";
    
    [ActivityInput(Hint = "Chunk size for processing", DefaultValue = 1000)]
    public int ChunkSize { get; set; } = 1000;
    
    [ActivityOutput]
    public List<object> ResultData { get; set; } = new();
    
    [ActivityOutput]
    public long TotalRows { get; set; }
    
    [ActivityOutput]
    public double ExecutionTimeSeconds { get; set; }
    
    public ExecuteDataQueryActivity(IEnumerable<IDataProvider> providers)
    {
        _providers = providers;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var startTime = DateTime.UtcNow;
        
        // Get the appropriate provider
        var provider = _providers.FirstOrDefault(p => p.ProviderType == ProviderType);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider '{ProviderType}' not found");
        }
        
        var allData = new List<object>();
        var offset = 0;
        
        // Estimate total rows
        TotalRows = await provider.EstimateRowCountAsync(Metadata, Parameters, Context);
        
        // Set initial progress
        context.WorkflowExecutionContext.SetVariable("Progress", 0);
        context.WorkflowExecutionContext.SetVariable("TotalRows", TotalRows);
        context.WorkflowExecutionContext.SetVariable("RowsProcessed", 0);
        
        // Process in chunks
        while (true)
        {
            // Clone metadata for current chunk
            var chunkMetadata = CloneMetadata(Metadata);
            chunkMetadata.Limit = ChunkSize;
            chunkMetadata.Offset = offset;
            
            // Execute chunk
            var result = await provider.ExecuteAsync(
                chunkMetadata,
                Parameters,
                Context,
                context.CancellationToken
            );
            
            if (!result.Success || result.Data == null)
            {
                if (!result.Success)
                {
                    throw new Exception($"Query execution failed: {result.ErrorMessage}");
                }
                break;
            }
            
            var chunkData = result.Data as IEnumerable<object> ?? new[] { result.Data };
            var chunkList = chunkData.ToList();
            
            if (!chunkList.Any())
                break;
            
            allData.AddRange(chunkList);
            offset += chunkList.Count;
            
            // Update progress
            var progress = TotalRows > 0 ? (int)(offset * 100.0 / TotalRows) : 100;
            context.WorkflowExecutionContext.SetVariable("Progress", progress);
            context.WorkflowExecutionContext.SetVariable("RowsProcessed", offset);
            
            // Check if we've processed all data
            if (chunkList.Count < ChunkSize)
                break;
        }
        
        ResultData = allData;
        ExecutionTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
        
        return Done();
    }
    
    private DataOperationMetadata CloneMetadata(DataOperationMetadata original)
    {
        return new DataOperationMetadata
        {
            OperationType = original.OperationType,
            RootEntity = original.RootEntity,
            Fields = original.Fields?.ToList(),
            Joins = original.Joins?.ToList(),
            Filters = original.Filters,
            Aggregations = original.Aggregations?.ToList(),
            GroupBy = original.GroupBy?.ToList(),
            OrderBy = original.OrderBy?.ToList(),
            CalculatedFields = original.CalculatedFields?.ToList(),
            WindowFunctions = original.WindowFunctions?.ToList(),
            UnionQueries = original.UnionQueries?.ToList()
        };
    }
}
