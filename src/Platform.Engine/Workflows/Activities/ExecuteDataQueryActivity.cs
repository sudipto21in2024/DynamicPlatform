namespace Platform.Engine.Workflows.Activities;

using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Elsa activity for executing data queries with chunked processing
/// </summary>
[Activity("Data Operations", "Execute Data Query", Description = "Executes a data query with chunked processing for large datasets")]
public class ExecuteDataQueryActivity : CodeActivity
{
    [Input(Description = "The data operation metadata")]
    public Input<DataOperationMetadata> Metadata { get; set; } = default!;
    
    [Input(Description = "Query parameters")]
    public Input<Dictionary<string, object>> Parameters { get; set; } = default!;
    
    [Input(Description = "Execution context")]
    public Input<ExecutionContext> Context { get; set; } = default!;
    
    [Input(Description = "Provider type (Entity, API, Workflow, Static)", DefaultValue = "Entity")]
    public Input<string> ProviderType { get; set; } = new("Entity");

    [Input(Description = "The Project ID for metadata virtualization")]
    public Input<Guid?> ProjectId { get; set; } = default!;
    
    [Input(Description = "Chunk size for processing", DefaultValue = 1000)]
    public Input<int> ChunkSize { get; set; } = new(1000);
    
    [Output]
    public Output<List<object>> ResultData { get; set; } = default!;
    
    [Output]
    public Output<long> TotalRows { get; set; } = default!;
    
    [Output]
    public Output<double> ExecutionTimeSeconds { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var providers = context.GetRequiredService<IEnumerable<IDataProvider>>();
        var normalizationService = context.GetRequiredService<IMetadataNormalizationService>();
        
        var startTime = DateTime.UtcNow;
        var providerTypeStr = ProviderType.Get(context);
        var projectId = ProjectId.Get(context);
        
        // Get the appropriate provider
        var provider = providers.FirstOrDefault(p => p.ProviderType == providerTypeStr);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider '{providerTypeStr}' not found");
        }
        
        var metadata = Metadata.Get(context);
        
        // APPLY METADATA VIRTUALIZATION
        if (projectId.HasValue)
        {
            await normalizationService.NormalizeAsync(projectId.Value, metadata);
        }

        var parameters = Parameters.Get(context) ?? new Dictionary<string, object>();
        var execContext = Context.Get(context);
        var chunkSize = ChunkSize.Get(context);
        
        var allData = new List<object>();
        var offset = 0;
        
        // Estimate total rows
        var totalRows = await provider.EstimateRowCountAsync(metadata, parameters, execContext);
        TotalRows.Set(context, totalRows);
        
        // Set initial progress in workflow variables
        context.SetVariable("Progress", 0);
        context.SetVariable("TotalRows", totalRows);
        context.SetVariable("RowsProcessed", 0);
        
        // Process in chunks
        while (true)
        {
            // Clone metadata for current chunk
            var chunkMetadata = CloneMetadata(metadata);
            chunkMetadata.Limit = chunkSize;
            chunkMetadata.Offset = offset;
            
            // Execute chunk
            var result = await provider.ExecuteAsync(
                chunkMetadata,
                parameters,
                execContext,
                context.CancellationToken
            );

            // VIRTUALIZE RESULT (SHADOW PROPERTIES)
            if (projectId.HasValue && !string.IsNullOrEmpty(metadata.RootEntity))
            {
                normalizationService.VirtualizeResult(projectId.Value, result, metadata.RootEntity);
            }
            
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
            var progress = totalRows > 0 ? (int)(offset * 100.0 / totalRows) : 100;
            context.SetVariable("Progress", progress);
            context.SetVariable("RowsProcessed", offset);
            
            // Check if we've processed all data
            if (chunkList.Count < chunkSize)
                break;
        }
        
        ResultData.Set(context, allData);
        ExecutionTimeSeconds.Set(context, (DateTime.UtcNow - startTime).TotalSeconds);
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
