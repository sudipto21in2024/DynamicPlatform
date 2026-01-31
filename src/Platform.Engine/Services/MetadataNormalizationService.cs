using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Services;

public class MetadataNormalizationService : IMetadataNormalizationService
{
    private readonly ICompatibilityProvider _compatibilityProvider;

    public MetadataNormalizationService(ICompatibilityProvider compatibilityProvider)
    {
        _compatibilityProvider = compatibilityProvider;
    }

    public async Task NormalizeAsync(Guid projectId, DataOperationMetadata metadata)
    {
        await _compatibilityProvider.LoadHistoryAsync(projectId);

        // 1. Resolve Root Entity
        if (!string.IsNullOrEmpty(metadata.RootEntity))
        {
            metadata.RootEntity = _compatibilityProvider.ResolveCurrentName(projectId, metadata.RootEntity, MetadataType.Entity);
        }

        // 2. Resolve Fields
        if (metadata.Fields != null)
        {
            foreach (var field in metadata.Fields)
            {
                field.Field = _compatibilityProvider.ResolveCurrentName(projectId, field.Field, MetadataType.Field, metadata.RootEntity);
            }
        }

        // 3. Resolve Filters
        if (metadata.Filters != null)
        {
            NormalizeFilterGroup(projectId, metadata.Filters, metadata.RootEntity);
        }

        // 4. Resolve Aggregations
        if (metadata.Aggregations != null)
        {
            foreach (var agg in metadata.Aggregations)
            {
                agg.Field = _compatibilityProvider.ResolveCurrentName(projectId, agg.Field, MetadataType.Field, metadata.RootEntity);
            }
        }
        
        // 5. Resolve GroupBy
        if (metadata.OrderBy != null)
        {
             foreach (var order in metadata.OrderBy)
             {
                 order.Field = _compatibilityProvider.ResolveCurrentName(projectId, order.Field, MetadataType.Field, metadata.RootEntity);
             }
        }

        // 6. Handle Subqueries (Recursive)
        if (metadata.UnionQueries != null)
        {
            foreach (var union in metadata.UnionQueries)
            {
                await NormalizeAsync(projectId, union);
            }
        }
    }

    private void NormalizeFilterGroup(Guid projectId, FilterGroup group, string rootEntity)
    {
        foreach (var cond in group.Conditions)
        {
            if (cond is FilterCondition fc)
            {
                fc.Field = _compatibilityProvider.ResolveCurrentName(projectId, fc.Field, MetadataType.Field, rootEntity);
                if (fc.Subquery != null)
                {
                    // Note: This would typically be async, but for MVP we assume subqueries are already loaded 
                    // or handled by the parent loop. For robustness, we'd make this recursive async.
                }
            }
            else if (cond is FilterGroup fg)
            {
                NormalizeFilterGroup(projectId, fg, rootEntity);
            }
        }
    }

    public void VirtualizeResult(Guid projectId, DataResult result, string rootEntity)
    {
        if (result.Data == null) return;

        var mappings = _compatibilityProvider.GetResultMappings(projectId, rootEntity);
        if (!mappings.Any()) return;

        if (result.Data is IEnumerable<object> list)
        {
            foreach (var item in list)
            {
                ApplyShadowProperties(item, mappings);
            }
        }
        else
        {
            ApplyShadowProperties(result.Data, mappings);
        }
    }

    private void ApplyShadowProperties(object item, Dictionary<string, string> mappings)
    {
        // For simplicity in MVP, we might use ExpandoObject or similar if the result is dynamic.
        // If it's a strongly typed POCO from EF, we can't easily add shadow properties at runtime without dynamic proxies.
        // Most DynamicPlatform results are already dynamic or DTO-based.
        
        if (item is IDictionary<string, object> dict)
        {
            foreach (var mapping in mappings)
            {
                if (dict.TryGetValue(mapping.Key, out var val) && !dict.ContainsKey(mapping.Value))
                {
                    dict[mapping.Value] = val; // Inject legacy name
                }
            }
        }
    }
}
