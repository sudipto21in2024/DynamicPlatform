using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Interfaces;

/// <summary>
/// Provides identity-based resolution for renamed or evolved metadata.
/// </summary>
public interface ICompatibilityProvider
{
    /// <summary>
    /// Resolves the current physical name for a legacy logical name.
    /// </summary>
    string ResolveCurrentName(Guid projectId, string legacyName, MetadataType type, string? parentName = null);

    /// <summary>
    /// Gets all renames for a specific entity to perform result virtualization.
    /// </summary>
    Dictionary<string, string> GetResultMappings(Guid projectId, string currentEntityName);
    
    /// <summary>
    /// Syncs history for a specific project.
    /// </summary>
    Task LoadHistoryAsync(Guid projectId);
}
