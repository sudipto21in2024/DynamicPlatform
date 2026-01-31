using System;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;

namespace Platform.Engine.Interfaces;

/// <summary>
/// Service responsible for managing project snapshots and versioning.
/// </summary>
public interface IVersioningService
{
    /// <summary>
    /// Creates a new immutable snapshot of the current project state.
    /// </summary>
    /// <param name="projectId">The project ID to snapshot.</param>
    /// <param name="version">The version string (e.g., "1.1.0").</param>
    /// <param name="createdBy">The user who created the snapshot.</param>
    /// <returns>The created snapshot.</returns>
    Task<ProjectSnapshot> CreateSnapshotAsync(Guid projectId, string version, string? createdBy = null);

    /// <summary>
    /// Retrieves a specific snapshot by its version.
    /// </summary>
    Task<ProjectSnapshot?> GetSnapshotAsync(Guid projectId, string version);

    /// <summary>
    /// Retrieves the latest snapshot for a project.
    /// </summary>
    Task<ProjectSnapshot?> GetLatestSnapshotAsync(Guid projectId);

    /// <summary>
    /// Retrieves the last published snapshot.
    /// </summary>
    Task<ProjectSnapshot?> GetLastPublishedSnapshotAsync(Guid projectId);
}
