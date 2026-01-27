namespace Platform.Engine.Services.DataExecution;

using Microsoft.EntityFrameworkCore;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Data provider for querying platform entities
/// </summary>
public class EntityDataProvider : IDataProvider
{
    private readonly IQueryBuilder _queryBuilder;
    private readonly IServiceProvider _serviceProvider;
    
    public string ProviderType => "Entity";
    
    public EntityDataProvider(IQueryBuilder queryBuilder, IServiceProvider serviceProvider)
    {
        _queryBuilder = queryBuilder;
        _serviceProvider = serviceProvider;
    }
    
    public async Task<DataResult> ExecuteAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Build the query
            var query = _queryBuilder.BuildQuery(metadata, context);
            
            // Execute the query
            var data = await query.ToListAsync(cancellationToken);
            
            var executionTime = (DateTime.UtcNow - startTime).TotalSeconds;
            
            return new DataResult
            {
                Success = true,
                Data = data,
                RowCount = data.Count,
                ExecutionTimeSeconds = executionTime
            };
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Query execution exceeded timeout limit");
        }
        catch (Exception ex)
        {
            return new DataResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds
            };
        }
    }
    
    public async Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata)
    {
        var errors = new List<ValidationError>();
        
        // Validate root entity exists
        if (string.IsNullOrEmpty(metadata.RootEntity))
        {
            errors.Add(new ValidationError
            {
                Field = "RootEntity",
                Message = "Root entity is required"
            });
        }
        
        // Validate joins don't create circular references
        if (metadata.Joins != null && metadata.Joins.Any())
        {
            var joinPath = new HashSet<string> { metadata.RootEntity! };
            foreach (var join in metadata.Joins)
            {
                if (joinPath.Contains(join.Entity))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "Joins",
                        Message = $"Circular join detected: {join.Entity} is already in the join path"
                    });
                }
                joinPath.Add(join.Entity);
            }
        }
        
        // TODO: Validate field names against entity schema
        // TODO: Validate filter operators match field types
        
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
    
    public async Task<long> EstimateRowCountAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context)
    {
        try
        {
            // Build query without pagination
            var countMetadata = new DataOperationMetadata
            {
                OperationType = metadata.OperationType,
                RootEntity = metadata.RootEntity,
                Joins = metadata.Joins,
                Filters = metadata.Filters
            };
            
            var query = _queryBuilder.BuildQuery(countMetadata, context);
            return await query.LongCountAsync();
        }
        catch
        {
            // If estimation fails, return -1 to indicate unknown
            return -1;
        }
    }
}
