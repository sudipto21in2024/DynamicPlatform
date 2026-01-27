namespace Platform.Engine.Interfaces;

using Platform.Engine.Models.DataExecution;

/// <summary>
/// Interface for data providers that execute data operations
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Type of provider (Entity, API, Workflow, Static)
    /// </summary>
    string ProviderType { get; }
    
    /// <summary>
    /// Executes a data operation and returns the result
    /// </summary>
    Task<DataResult> ExecuteAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        CancellationToken cancellationToken = default
    );
    
    /// <summary>
    /// Validates the metadata before execution
    /// </summary>
    Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata);
    
    /// <summary>
    /// Estimates the number of rows that will be returned
    /// </summary>
    Task<long> EstimateRowCountAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context
    );
}
