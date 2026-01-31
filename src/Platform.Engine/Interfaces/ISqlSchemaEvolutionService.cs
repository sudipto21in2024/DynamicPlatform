using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Engine.Models.Delta;

namespace Platform.Engine.Interfaces;

/// <summary>
/// Service responsible for generating and executing SQL DDL to evolve the database schema.
/// </summary>
public interface ISqlSchemaEvolutionService
{
    /// <summary>
    /// Generates the SQL scripts required to apply the migration plan.
    /// </summary>
    List<string> GenerateMigrationScripts(MigrationPlan plan);

    /// <summary>
    /// Executes the migration scripts against the target database.
    /// </summary>
    Task ApplyMigrationAsync(MigrationPlan plan, string connectionString);
}
