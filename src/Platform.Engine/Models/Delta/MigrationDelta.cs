using System;
using System.Collections.Generic;

namespace Platform.Engine.Models.Delta;

public enum DeltaAction
{
    Added,
    Removed,
    Updated,
    Renamed
}

public enum MetadataType
{
    Entity,
    Field,
    Relation,
    Enum,
    EnumValue,
    Page,
    Widget
}

public class MigrationDelta
{
    public MetadataType Type { get; set; }
    public DeltaAction Action { get; set; }
    public Guid ElementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PreviousName { get; set; }
    
    // Details of what changed (e.g., "Field Type changed from Int to String")
    public Dictionary<string, PropertyChange> Changes { get; set; } = new();
    
    // Parent context (e.g., Entity ID for a Field)
    public Guid? ParentId { get; set; }
}

public class PropertyChange
{
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public bool IsBreaking { get; set; }
}

public class MigrationPlan
{
    public Guid ProjectId { get; set; }
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public List<MigrationDelta> Deltas { get; set; } = new();
    public bool HasBreakingChanges => Deltas.Any(d => d.Changes.Values.Any(c => c.IsBreaking));
}
