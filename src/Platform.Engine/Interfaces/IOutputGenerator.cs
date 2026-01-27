namespace Platform.Engine.Interfaces;

using Platform.Engine.Models.DataExecution;

/// <summary>
/// Interface for output generators that convert data to various formats
/// </summary>
public interface IOutputGenerator
{
    /// <summary>
    /// Output format (Excel, PDF, CSV, etc.)
    /// </summary>
    string Format { get; }
    
    /// <summary>
    /// Generates output in the specified format
    /// </summary>
    Task<Stream> GenerateAsync(
        IEnumerable<object> data,
        OutputOptions options,
        CancellationToken cancellationToken = default
    );
}
