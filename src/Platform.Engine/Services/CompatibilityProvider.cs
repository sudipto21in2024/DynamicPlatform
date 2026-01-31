using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Interfaces;
using Platform.Engine.Models;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Services;

public class CompatibilityProvider : ICompatibilityProvider
{
    private readonly IRepository<ProjectSnapshot> _snapshotRepository;
    private readonly Dictionary<Guid, ProjectHistory> _cache = new();

    public CompatibilityProvider(IRepository<ProjectSnapshot> snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task LoadHistoryAsync(Guid projectId)
    {
        if (_cache.ContainsKey(projectId)) return;

        var snapshots = await _snapshotRepository.GetAllAsync(
            filter: s => s.ProjectId == projectId && s.IsPublished,
            orderBy: q => q.OrderBy(s => s.CreatedAt)
        );

        var history = new ProjectHistory();

        foreach (var snapshot in snapshots)
        {
            var artifacts = JsonSerializer.Deserialize<List<Artifact>>(snapshot.Content);
            if (artifacts == null) continue;

            foreach (var artifact in artifacts)
            {
                if (artifact.Type == ArtifactType.Entity)
                {
                    var entity = JsonSerializer.Deserialize<EntityMetadata>(artifact.Content);
                    if (entity == null) continue;

                    // Track Entity Identity
                    history.GuidToCurrentName[entity.Id] = entity.Name;
                    history.NameToGuid[$"Entity:{entity.Name}"] = entity.Id;

                    // Track Field Identities
                    foreach (var field in entity.Fields)
                    {
                        history.GuidToCurrentName[field.Id] = field.Name;
                        history.NameToGuid[$"Field:{entity.Name}:{field.Name}"] = field.Id;
                        history.FieldToParent[field.Id] = entity.Name;
                    }
                }
            }
        }

        _cache[projectId] = history;
    }

    public string ResolveCurrentName(Guid projectId, string legacyName, MetadataType type, string? parentName = null)
    {
        if (!_cache.TryGetValue(projectId, out var history)) return legacyName;

        if (type == MetadataType.Entity)
        {
            string key = $"Entity:{legacyName}";
            if (history.NameToGuid.TryGetValue(key, out var guid))
            {
                return history.GuidToCurrentName.TryGetValue(guid, out var currentName) ? currentName : legacyName;
            }
        }
        else if (type == MetadataType.Field && !string.IsNullOrEmpty(parentName))
        {
            // 1. Try resolving the field with the provided parentName
            string key = $"Field:{parentName}:{legacyName}";
            if (history.NameToGuid.TryGetValue(key, out var guid))
            {
                return history.GuidToCurrentName.TryGetValue(guid, out var currentName) ? currentName : legacyName;
            }

            // 2. Fallback search
            foreach (var entry in history.NameToGuid.Where(k => k.Key.EndsWith($":{legacyName}")))
            {
                if (history.GuidToCurrentName.TryGetValue(entry.Value, out var currentFieldName))
                {
                    return currentFieldName;
                }
            }
        }

        return legacyName;
    }

    public Dictionary<string, string> GetResultMappings(Guid projectId, string currentEntityName)
    {
        if (!_cache.TryGetValue(projectId, out var history)) return new();

        var mappings = new Dictionary<string, string>();
        
        var entityKey = $"Entity:{currentEntityName}";
        if (!history.NameToGuid.TryGetValue(entityKey, out var entityGuid)) return mappings;

        foreach (var nameKey in history.NameToGuid.Where(k => k.Key.StartsWith("Field:")))
        {
            var parts = nameKey.Key.Split(':');
            var fieldName = parts[2];
            var fieldGuid = nameKey.Value;

            var currentPhysicalFieldName = history.GuidToCurrentName.GetValueOrDefault(fieldGuid);
            if (currentPhysicalFieldName != null && fieldName != currentPhysicalFieldName)
            {
                mappings[currentPhysicalFieldName] = fieldName;
            }
        }

        return mappings;
    }

    private class ProjectHistory
    {
        public Dictionary<Guid, string> GuidToCurrentName { get; set; } = new();
        public Dictionary<string, Guid> NameToGuid { get; set; } = new();
        public Dictionary<Guid, string> FieldToParent { get; set; } = new();
    }
}
