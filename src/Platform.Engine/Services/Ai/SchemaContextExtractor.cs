using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Models;

namespace Platform.Engine.Services.Ai;

/// <summary>
/// Reads all Artifact records for a project from the database and converts them into
/// a structured markdown "Platform Knowledge Document" that is injected into the AI
/// system prompt as context. This is what makes the AI "aware" of your entities,
/// relations, connectors, and business rules.
/// </summary>
public class SchemaContextExtractor
{
    private readonly IArtifactRepository _repo;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SchemaContextExtractor(IArtifactRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Extracts a full context document for the given project.
    /// Returns an empty context string if projectId is null (no project scope).
    /// </summary>
    public async Task<string> ExtractAsync(Guid? projectId, CancellationToken ct = default)
    {
        if (projectId == null || projectId == Guid.Empty)
            return string.Empty;

        var artifacts = (await _repo.GetByProjectIdAsync(projectId.Value)).ToList();

        if (!artifacts.Any())
            return $"\n\n[No artifacts found for project {projectId}. Starting with a blank canvas.]\n";

        var sb = new StringBuilder();
        sb.AppendLine("\n\n=== PLATFORM KNOWLEDGE DOCUMENT ===");
        sb.AppendLine($"Project ID: {projectId}");
        sb.AppendLine($"Total Artifacts: {artifacts.Count}");
        sb.AppendLine("Use this knowledge to write accurate, type-safe code that references existing entities.\n");

        // ── 1. ENTITIES ───────────────────────────────────────────────────────
        var entityArtifacts = artifacts.Where(a => a.Type == ArtifactType.Entity).ToList();
        if (entityArtifacts.Any())
        {
            sb.AppendLine("## Entities & Fields");
            sb.AppendLine("These are the data models available in this project. Use exact field names.\n");

            foreach (var artifact in entityArtifacts)
            {
                try
                {
                    var entity = JsonSerializer.Deserialize<EntityMetadata>(artifact.Content, _jsonOpts);
                    if (entity == null) continue;

                    sb.AppendLine($"### Entity: `{entity.Name}` (namespace: {entity.Namespace})");
                    sb.AppendLine("| Field | C# Type | Required | MaxLength |");
                    sb.AppendLine("|---|---|---|---|");

                    foreach (var field in entity.Fields)
                    {
                        sb.AppendLine(
                            $"| {field.Name} | {field.CsharpType} | {field.IsRequired} | " +
                            $"{(field.MaxLength > 0 ? field.MaxLength.ToString() : "-")} |");
                    }

                    if (entity.Relations.Any())
                    {
                        sb.AppendLine("\n**Relations:**");
                        foreach (var rel in entity.Relations)
                        {
                            sb.AppendLine(
                                $"- `{entity.Name}` **{rel.Type}** `{rel.TargetEntity}` " +
                                $"via `{rel.NavPropName}` (FK: `{rel.ForeignKeyName}`)");
                        }
                    }

                    sb.AppendLine();
                }
                catch
                {
                    sb.AppendLine($"  ⚠ Could not parse entity: {artifact.Name}\n");
                }
            }
        }

        // ── 2. ENUMS ──────────────────────────────────────────────────────────
        var enumArtifacts = artifacts.Where(a => a.Type == ArtifactType.Enum).ToList();
        if (enumArtifacts.Any())
        {
            sb.AppendLine("## Enums");
            foreach (var artifact in enumArtifacts)
            {
                try
                {
                    var enumMeta = JsonSerializer.Deserialize<EnumMetadata>(artifact.Content, _jsonOpts);
                    if (enumMeta == null) continue;

                    var values = string.Join(", ", enumMeta.Values.Select(v => $"{v.Name}={v.Value}"));
                    sb.AppendLine($"- `{enumMeta.Name}`: [{values}]");
                }
                catch { /* skip malformed */ }
            }
            sb.AppendLine();
        }

        // ── 3. EXISTING CONNECTORS ────────────────────────────────────────────
        var connectorArtifacts = artifacts.Where(a => a.Type == ArtifactType.Connector).ToList();
        if (connectorArtifacts.Any())
        {
            sb.AppendLine("## Available Connectors (IConnector implementations)");
            sb.AppendLine("These are reusable logic units already built. Reference them in workflow suggestions.\n");

            foreach (var artifact in connectorArtifacts)
            {
                try
                {
                    var conn = JsonSerializer.Deserialize<ConnectorMetadata>(artifact.Content, _jsonOpts);
                    if (conn == null) continue;

                    sb.AppendLine($"### Connector: `{conn.Name}`");
                    sb.AppendLine($"Description: {conn.Description}");

                    if (conn.Inputs.Any())
                        sb.AppendLine($"Inputs: {string.Join(", ", conn.Inputs.Select(i => $"`{i.Name}:{i.Type}`"))}");
                    if (conn.Outputs.Any())
                        sb.AppendLine($"Outputs: {string.Join(", ", conn.Outputs.Select(o => $"`{o.Name}:{o.Type}`"))}");

                    sb.AppendLine();
                }
                catch { /* skip malformed */ }
            }
        }

        // ── 4. BUSINESS RULES ─────────────────────────────────────────────────
        // BusinessRule is stored as an Entity artifact with a known naming convention
        // Future: ArtifactType.BusinessRule. For now scan entities content for BusinessRuleMetadata shape.
        var ruleArtifacts = artifacts
            .Where(a => a.Type == ArtifactType.Entity && a.Name.EndsWith("Rule"))
            .ToList();

        if (ruleArtifacts.Any())
        {
            sb.AppendLine("## Existing Business Rules (Do NOT generate conflicting logic)");
            foreach (var artifact in ruleArtifacts)
            {
                try
                {
                    var rule = JsonSerializer.Deserialize<BusinessRuleMetadata>(artifact.Content, _jsonOpts);
                    if (rule == null) continue;
                    sb.AppendLine(
                        $"- **{rule.Name}** on `{rule.TargetEntity}` [{rule.Trigger}]: " +
                        $"IF `{rule.Condition}` THEN `{rule.Action}`");
                }
                catch { /* skip */ }
            }
            sb.AppendLine();
        }

        // ── 5. FORMS ──────────────────────────────────────────────────────────
        var formArtifacts = artifacts.Where(a => a.Type == ArtifactType.Form).ToList();
        if (formArtifacts.Any())
        {
            sb.AppendLine("## Forms");
            foreach (var artifact in formArtifacts)
                sb.AppendLine($"- `{artifact.Name}`");
            sb.AppendLine();
        }

        // ── 6. WORKFLOWS ──────────────────────────────────────────────────────
        var workflowArtifacts = artifacts.Where(a => a.Type == ArtifactType.Workflow).ToList();
        if (workflowArtifacts.Any())
        {
            sb.AppendLine("## Existing Workflows");
            foreach (var artifact in workflowArtifacts)
                sb.AppendLine($"- `{artifact.Name}` (Elsa 3.x workflow)");
            sb.AppendLine();
        }

        sb.AppendLine("=== END PLATFORM KNOWLEDGE DOCUMENT ===\n");

        return sb.ToString();
    }
}
