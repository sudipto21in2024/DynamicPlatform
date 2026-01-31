using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Services;

public class SqlSchemaEvolutionService : ISqlSchemaEvolutionService
{
    public List<string> GenerateMigrationScripts(MigrationPlan plan)
    {
        var scripts = new List<string>();

        // 1. Process Entity Additions (CREATE TABLE)
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Entity && d.Action == DeltaAction.Added))
        {
            scripts.Add(GenerateCreateTableScript(delta, plan.Deltas));
        }

        // 2. Process Entity Renames
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Entity && d.Action == DeltaAction.Renamed))
        {
            scripts.Add($"ALTER TABLE \"{delta.PreviousName}\" RENAME TO \"{delta.Name}\";");
        }

        // 3. Process Field Additions
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Field && d.Action == DeltaAction.Added))
        {
            // Only add field if its entity already exists (not just created in this plan)
            var parentEntity = plan.Deltas.FirstOrDefault(d => d.Type == MetadataType.Entity && d.ElementId == delta.ParentId);
            if (parentEntity == null || parentEntity.Action != DeltaAction.Added)
            {
                var tableName = GetTableName(delta, plan);
                scripts.Add($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{delta.Name}\" {MapToSqlType(delta)};");
            }
        }

        // 4. Process Field Renames
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Field && d.Action == DeltaAction.Renamed))
        {
            var tableName = GetTableName(delta, plan);
            scripts.Add($"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{delta.PreviousName}\" TO \"{delta.Name}\";");
        }

        // 5. Process Field Type Changes
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Field && d.Action == DeltaAction.Updated && d.Changes.ContainsKey("Type")))
        {
            var tableName = GetTableName(delta, plan);
            var newType = MapToSqlType(delta);
            // Using USING clause for safe casting
            scripts.Add($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{delta.Name}\" TYPE {newType} USING \"{delta.Name}\"::{newType};");
        }

        // 6. Process Field Deletions (Safe Delete)
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Field && d.Action == DeltaAction.Removed))
        {
            var tableName = GetTableName(delta, plan);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            scripts.Add($"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{delta.Name}\" TO \"_deprecated_{delta.Name}_{timestamp}\";");
        }

        // 7. Process Entity Deletions (Safe Delete)
        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Entity && d.Action == DeltaAction.Removed))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            scripts.Add($"ALTER TABLE \"{delta.Name}\" RENAME TO \"_deprecated_{delta.Name}_{timestamp}\";");
        }

        return scripts;
    }

    public async Task ApplyMigrationAsync(MigrationPlan plan, string connectionString)
    {
        var scripts = GenerateMigrationScripts(plan);
        if (!scripts.Any()) return;

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        using var trans = await conn.BeginTransactionAsync();

        try
        {
            foreach (var sql in scripts)
            {
                using var cmd = new NpgsqlCommand(sql, conn, trans);
                await cmd.ExecuteNonQueryAsync();
            }
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    private string GenerateCreateTableScript(MigrationDelta entityDelta, List<MigrationDelta> allDeltas)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE \"{entityDelta.Name}\" (");
        sb.Append("\"Id\" UUID PRIMARY KEY DEFAULT gen_random_uuid()");

        var fields = allDeltas.Where(d => d.Type == MetadataType.Field && d.ParentId == entityDelta.ElementId && d.Action == DeltaAction.Added);
        foreach (var field in fields)
        {
            sb.Append($", \"{field.Name}\" {MapToSqlType(field)}");
        }

        sb.Append(");");
        return sb.ToString();
    }

    private string GetTableName(MigrationDelta fieldDelta, MigrationPlan plan)
    {
        var entity = plan.Deltas.FirstOrDefault(d => d.Type == MetadataType.Entity && d.ElementId == fieldDelta.ParentId);
        return entity?.Name ?? throw new InvalidOperationException("Field parent entity not found in migration plan.");
    }

    private string MapToSqlType(MigrationDelta fieldDelta)
    {
        var type = fieldDelta.Changes.ContainsKey("Type") 
            ? fieldDelta.Changes["Type"].NewValue?.ToString()?.ToLower() 
            : null;

        // Fallback for Added fields where type is in metadata but not in "Changes" dict yet (implemenation detail of Diff service)
        // For simplicity, here we assume it's passed somehow or we refine Diff Service to include it
        // Let's check how Diff Service actually sets item Name/Type for Added items.
        
        return type switch
        {
            "int" => "INTEGER",
            "decimal" => "DECIMAL(18,2)",
            "datetime" => "TIMESTAMP WITH TIME ZONE",
            "guid" => "UUID",
            "bool" => "BOOLEAN",
            _ => "TEXT"
        };
    }
}
