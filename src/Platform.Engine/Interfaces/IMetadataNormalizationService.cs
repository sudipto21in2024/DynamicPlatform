using System;
using System.Threading.Tasks;
using Platform.Engine.Models.DataExecution;

namespace Platform.Engine.Interfaces;

/// <summary>
/// Normalizes legacy data operation metadata to the current physical schema.
/// </summary>
public interface IMetadataNormalizationService
{
    /// <summary>
    /// Rewrites the metadata in-place to use current entity and field names.
    /// </summary>
    Task NormalizeAsync(Guid projectId, DataOperationMetadata metadata);
    
    /// <summary>
    /// Adds legacy alias properties to the result set for backward compatibility.
    /// </summary>
    void VirtualizeResult(Guid projectId, DataResult result, string rootEntity);
}
