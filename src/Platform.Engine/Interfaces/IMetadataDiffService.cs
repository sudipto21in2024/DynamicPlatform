using System.Collections.Generic;
using Platform.Core.Domain.Entities;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Interfaces;

/// <summary>
/// Service responsible for analyzing differences between two project snapshots.
/// </summary>
public interface IMetadataDiffService
{
    /// <summary>
    /// Compares two snapshots and returns a migration plan.
    /// </summary>
    MigrationPlan Compare(ProjectSnapshot? oldSnapshot, ProjectSnapshot draftSnapshot);

    /// <summary>
    /// Compares current live artifacts with a candidate snapshot.
    /// </summary>
    MigrationPlan Compare(IEnumerable<Artifact> oldArtifacts, IEnumerable<Artifact> newArtifacts);
}
