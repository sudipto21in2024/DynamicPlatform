using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Engine.Models;

namespace Platform.Engine.Services;

public class RelationNormalizationService
{
    public List<EntityMetadata> Normalize(List<EntityMetadata> entities)
    {
        // Clone the list because we will add to it
        var result = new List<EntityMetadata>(entities);
        
        // Find M:N relations
        // We look for relations where Type is ManyToMany
        // We only process one side to avoid duplicates (e.g. A->B, but ignore B->A if we already processed A->B)
        // Or we can process all and track created join tables.
        
        var processedJoinTables = new HashSet<string>();

        // We iterate a copy of the list to allow modification of the entities inside (which are ref types)
        // Note: modification of EntityMetadata objects inside 'entities' will affect the objects in 'result' (same references)
        // This is desired.
        
        foreach (var sourceEntity in entities)
        {
            // Use ToList to iterate safely while modifying
            var relations = sourceEntity.Relations.Where(r => r.Type == RelationType.ManyToMany).ToList();

            foreach (var rel in relations)
            {
                var targetEntity = entities.FirstOrDefault(e => e.Name == rel.TargetEntity);
                if (targetEntity == null) continue;

                // Determine Join Table Name
                var p1 = sourceEntity.Name;
                var p2 = targetEntity.Name;
                
                // Consistent naming: Field1Field2 (Alphabetical)
                var joinTableName = rel.JoinTableName;
                if (string.IsNullOrEmpty(joinTableName))
                {
                    joinTableName = string.Compare(p1, p2) < 0 ? $"{p1}{p2}" : $"{p2}{p1}";
                }

                if (processedJoinTables.Contains(joinTableName))
                {
                    // Already created the middle entity, but we still need to update THIS entity's relation
                    // from M:N (Target) to 1:N (JoinTable)
                    
                    // Update Source Relation
                    rel.Type = RelationType.OneToMany;
                    rel.TargetEntity = joinTableName;
                    rel.NavPropName = joinTableName + "s"; // Pluralize roughly
                    continue; // Skip creation
                }
                
                processedJoinTables.Add(joinTableName);

                // Create Middle Entity
                var middleEntity = new EntityMetadata
                {
                    Name = joinTableName,
                    Namespace = sourceEntity.Namespace,
                    // Add Id and CreatedAt? Entity base usually handles it.
                    Fields = new List<FieldMetadata>() 
                };

                // Add Relations to Source and Target
                // Middle -> Source
                middleEntity.Relations.Add(new RelationMetadata
                {
                    TargetEntity = sourceEntity.Name,
                    Type = RelationType.ManyToOne,
                    NavPropName = sourceEntity.Name,
                    ForeignKeyName = $"{sourceEntity.Name}Id"
                });

                // Middle -> Target
                middleEntity.Relations.Add(new RelationMetadata
                {
                    TargetEntity = targetEntity.Name,
                    Type = RelationType.ManyToOne,
                    NavPropName = targetEntity.Name,
                    ForeignKeyName = $"{targetEntity.Name}Id"
                });

                result.Add(middleEntity);

                // Update Source Relation
                // Change M:N to 1:N pointing to Middle Entity
                rel.Type = RelationType.OneToMany;
                rel.TargetEntity = joinTableName;
                rel.NavPropName = joinTableName + "s";

                // Update Target Relation (if it exists and is inverse)
                // If Target Entity has a relation back to Source, we need to find it and update it too.
                var inverseRel = targetEntity.Relations.FirstOrDefault(r => 
                    r.Type == RelationType.ManyToMany && 
                    r.TargetEntity == sourceEntity.Name);
                
                if (inverseRel != null)
                {
                    inverseRel.Type = RelationType.OneToMany;
                    inverseRel.TargetEntity = joinTableName;
                    inverseRel.NavPropName = joinTableName + "s";
                }
            }
        }

        return result;
    }
}
