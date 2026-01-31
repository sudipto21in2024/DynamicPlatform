using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Platform.Core.Domain.Entities;
using Platform.Engine.Interfaces;
using Platform.Engine.Models;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Services;

public class MetadataDiffService : IMetadataDiffService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MigrationPlan Compare(ProjectSnapshot? oldSnapshot, ProjectSnapshot currentSnapshot)
    {
        var oldArtifacts = oldSnapshot != null 
            ? JsonSerializer.Deserialize<List<Artifact>>(oldSnapshot.Content, _jsonOptions) ?? new()
            : new List<Artifact>();

        var currentArtifacts = JsonSerializer.Deserialize<List<Artifact>>(currentSnapshot.Content, _jsonOptions) ?? new();

        var plan = Compare(oldArtifacts, currentArtifacts);
        plan.FromVersion = oldSnapshot?.Version ?? "0.0.0";
        plan.ToVersion = currentSnapshot.Version;
        plan.ProjectId = currentSnapshot.ProjectId;

        return plan;
    }

    public MigrationPlan Compare(IEnumerable<Artifact> oldArtifacts, IEnumerable<Artifact> currentArtifacts)
    {
        var plan = new MigrationPlan();

        // Group artifacts by type for easier processing
        foreach (ArtifactType type in Enum.GetValues(typeof(ArtifactType)))
        {
            var oldItems = oldArtifacts.Where(a => a.Type == type).ToList();
            var currentItems = currentArtifacts.Where(a => a.Type == type).ToList();

            ProcessArtifactCategory(type, oldItems, currentItems, plan);
        }

        return plan;
    }

    private void ProcessArtifactCategory(ArtifactType type, List<Artifact> oldItems, List<Artifact> currentItems, MigrationPlan plan)
    {
        switch (type)
        {
            case ArtifactType.Entity:
                CompareEntities(oldItems, currentItems, plan);
                break;
            // Add other types as needed (Pages, Workflows, etc.)
        }
    }

    private void CompareEntities(List<Artifact> oldItems, List<Artifact> currentItems, MigrationPlan plan)
    {
        var oldEntities = oldItems.Select(a => JsonSerializer.Deserialize<EntityMetadata>(a.Content, _jsonOptions)!).ToList();
        var currentEntities = currentItems.Select(a => JsonSerializer.Deserialize<EntityMetadata>(a.Content, _jsonOptions)!).ToList();

        var oldDict = oldEntities.ToDictionary(e => e.Id);
        var currentDict = currentEntities.ToDictionary(e => e.Id);

        // Process Additions and Updates
        foreach (var current in currentEntities)
        {
            if (!oldDict.TryGetValue(current.Id, out var old))
            {
                plan.Deltas.Add(new MigrationDelta
                {
                    Type = MetadataType.Entity,
                    Action = DeltaAction.Added,
                    ElementId = current.Id,
                    Name = current.Name
                });

                // Add all fields as additions
                foreach (var field in current.Fields)
                {
                    var fieldDelta = new MigrationDelta
                    {
                        Type = MetadataType.Field,
                        Action = DeltaAction.Added,
                        ElementId = field.Id,
                        Name = field.Name,
                        ParentId = current.Id
                    };
                    fieldDelta.Changes["Type"] = new PropertyChange { OldValue = null, NewValue = field.Type, IsBreaking = false };
                    plan.Deltas.Add(fieldDelta);
                }
            }
            else
            {
                // Entity exists, check for renames and internal changes
                var entityDelta = new MigrationDelta
                {
                    Type = MetadataType.Entity,
                    Action = DeltaAction.Updated,
                    ElementId = current.Id,
                    Name = current.Name
                };

                if (old.Name != current.Name)
                {
                    entityDelta.Action = DeltaAction.Renamed;
                    entityDelta.PreviousName = old.Name;
                    entityDelta.Changes["Name"] = new PropertyChange { OldValue = old.Name, NewValue = current.Name, IsBreaking = false };
                }

                CompareFields(old.Fields, current.Fields, current.Id, plan);

                if (entityDelta.Changes.Any())
                {
                    plan.Deltas.Add(entityDelta);
                }
            }
        }

        // Process Deletions
        foreach (var old in oldEntities)
        {
            if (!currentDict.ContainsKey(old.Id))
            {
                plan.Deltas.Add(new MigrationDelta
                {
                    Type = MetadataType.Entity,
                    Action = DeltaAction.Removed,
                    ElementId = old.Id,
                    Name = old.Name
                });
            }
        }
    }

    private void CompareFields(List<FieldMetadata> oldFields, List<FieldMetadata> currentFields, Guid entityId, MigrationPlan plan)
    {
        var oldDict = oldFields.ToDictionary(f => f.Id);
        var currentDict = currentFields.ToDictionary(f => f.Id);

        foreach (var current in currentFields)
        {
            if (!oldDict.TryGetValue(current.Id, out var old))
            {
                var fieldDelta = new MigrationDelta
                {
                    Type = MetadataType.Field,
                    Action = DeltaAction.Added,
                    ElementId = current.Id,
                    Name = current.Name,
                    ParentId = entityId
                };
                fieldDelta.Changes["Type"] = new PropertyChange { OldValue = null, NewValue = current.Type, IsBreaking = false };
                plan.Deltas.Add(fieldDelta);
            }
            else
            {
                var fieldDelta = new MigrationDelta
                {
                    Type = MetadataType.Field,
                    Action = DeltaAction.Updated,
                    ElementId = current.Id,
                    Name = current.Name,
                    ParentId = entityId
                };

                if (old.Name != current.Name)
                {
                    fieldDelta.Action = DeltaAction.Renamed;
                    fieldDelta.PreviousName = old.Name;
                    fieldDelta.Changes["Name"] = new PropertyChange { OldValue = old.Name, NewValue = current.Name, IsBreaking = false };
                }

                if (old.Type != current.Type)
                {
                    var isBreaking = IsTypeChangeBreaking(old.Type, current.Type);
                    fieldDelta.Changes["Type"] = new PropertyChange { OldValue = old.Type, NewValue = current.Type, IsBreaking = isBreaking };
                }

                if (old.IsRequired != current.IsRequired)
                {
                    // False -> True is breaking (existing data might be null)
                    var isBreaking = !old.IsRequired && current.IsRequired;
                    fieldDelta.Changes["IsRequired"] = new PropertyChange { OldValue = old.IsRequired, NewValue = current.IsRequired, IsBreaking = isBreaking };
                }

                if (fieldDelta.Changes.Any())
                {
                    plan.Deltas.Add(fieldDelta);
                }
            }
        }

        foreach (var old in oldFields)
        {
            if (!currentDict.ContainsKey(old.Id))
            {
                plan.Deltas.Add(new MigrationDelta
                {
                    Type = MetadataType.Field,
                    Action = DeltaAction.Removed,
                    ElementId = old.Id,
                    Name = old.Name,
                    ParentId = entityId
                });
            }
        }
    }

    private bool IsTypeChangeBreaking(string oldType, string newType)
    {
        // Define safe conversions
        if (oldType == "int" && newType == "decimal") return false;
        if (oldType == "int" && newType == "string") return false;
        if (oldType == "decimal" && newType == "string") return false;
        
        // Everything else is potentially breaking
        return true;
    }
}
