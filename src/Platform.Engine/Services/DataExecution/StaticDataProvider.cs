namespace Platform.Engine.Services.DataExecution;

using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Static data provider for testing and mocking
/// </summary>
public class StaticDataProvider : IDataProvider
{
    public string ProviderType => "Static";
    
    public Task<DataResult> ExecuteAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // Simply return the parameters as data
        var result = new DataResult
        {
            Success = true,
            Data = parameters,
            RowCount = parameters.Count,
            ExecutionTimeSeconds = 0
        };
        
        return Task.FromResult(result);
    }
    
    public Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata)
    {
        // Static provider always validates successfully
        return Task.FromResult(new ValidationResult { IsValid = true });
    }
    
    public Task<long> EstimateRowCountAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context)
    {
        // Static data count is just the parameter count
        return Task.FromResult((long)parameters.Count);
    }
}
