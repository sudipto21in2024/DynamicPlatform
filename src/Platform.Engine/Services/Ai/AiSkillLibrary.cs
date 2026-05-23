using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Models;
using System.IO;

namespace Platform.Engine.Services.Ai;

/// <summary>
/// Loads AiSkill definitions from the Artifact table (ArtifactType.AiSkill).
/// Falls back to platform-built-in skills if none are stored in the project.
/// This allows tenants to customize skills without code changes.
/// </summary>
public class AiSkillLibrary
{
    private readonly IArtifactRepository _repo;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiSkillLibrary(IArtifactRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Gets a skill by name for the given project.
    /// Lookup order: Project-specific AiSkill artifact → Built-in platform defaults.
    /// </summary>
    public async Task<AiSkillMetadata> GetSkillAsync(
        string skillName,
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        // Try project-specific skill first
        if (projectId.HasValue && projectId != Guid.Empty)
        {
            var artifacts = await _repo.GetByProjectIdAsync(projectId.Value);
            foreach (var artifact in artifacts)
            {
                if (artifact.Type == ArtifactType.AiSkill && artifact.Name == skillName)
                {
                    try
                    {
                        var skill = JsonSerializer.Deserialize<AiSkillMetadata>(artifact.Content, _jsonOpts);
                        if (skill != null) return skill;
                    }
                    catch { /* fall through to built-in */ }
                }
            }
        }

        // Return built-in platform skill
        return GetBuiltInSkill(skillName);
    }

    // ── Built-in Platform Skills ──────────────────────────────────────────────

    private static AiSkillMetadata GetBuiltInSkill(string skillName) => skillName switch
    {
        "GenerateConnectorLogic" => BuiltInSkills.GenerateConnectorLogic,
        "GenerateEntitySchema"   => BuiltInSkills.GenerateEntitySchema,
        "GenerateBusinessRule"   => BuiltInSkills.GenerateBusinessRule,
        "ExplainLogic"           => BuiltInSkills.ExplainLogic,
        "DesignEntity"           => BuiltInSkills.DesignEntity,
        _                        => BuiltInSkills.GenerateConnectorLogic // safe default
    };

    public static List<AiSkillMetadata> GetAllBuiltInSkills() => new()
    {
        BuiltInSkills.GenerateConnectorLogic,
        BuiltInSkills.GenerateEntitySchema,
        BuiltInSkills.GenerateBusinessRule,
        BuiltInSkills.ExplainLogic,
        BuiltInSkills.DesignEntity
    };
}

/// <summary>
/// Catalog of platform built-in skills. These are the defaults used when no
/// project-level skill artifact overrides them.
/// </summary>
internal static class BuiltInSkills
{
    private static string LoadPromptResource(string promptFileName)
    {
        var assembly = typeof(BuiltInSkills).Assembly;
        var resourceName = $"Platform.Engine.Services.Ai.Prompts.{promptFileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new System.IO.FileNotFoundException($"Embedded resource {resourceName} not found.");
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
    public static readonly AiSkillMetadata GenerateConnectorLogic = new()
    {
        SkillName = "GenerateConnectorLogic",
        Description = "Converts a business requirement into a ConnectorMetadata JSON with a C# businessLogic snippet.",
        Category = "CodeGeneration",
        DefaultTemperature = 0.3,
        MaxTokens = 8192,
        ExpectedOutputFormat = "JSON",
        SystemPrompt = LoadPromptResource("GenerateConnectorLogic.md"),
        GuidePrompt =
            "Understood. I will output only a valid JSON object. " +
            "The businessLogic will contain only executable C# statements. Ready.",
        FewShotExamples = new()
        {
            new AiSkillExample
            {
                UserInput = "Calculate 18% GST on a net amount and return the gross total.",
                ExpectedOutput = """
                    {
                      "name": "CalculateGst",
                      "namespace": "GeneratedApp.Connectors",
                      "description": "Calculates 18% GST on a net amount.",
                      "inputs": [ { "name": "NetAmount", "type": "decimal" } ],
                      "outputs": [ { "name": "GrossAmount", "type": "decimal" }, { "name": "GstAmount", "type": "decimal" } ],
                      "configProperties": [],
                      "businessLogic": "var GstAmount = NetAmount * 0.18m;\nvar GrossAmount = NetAmount + GstAmount;\nlogger.LogInformation($\"GST: Net={NetAmount}, GST={GstAmount}, Gross={GrossAmount}\");\nreturn new Dictionary<string, object?> { [\"GrossAmount\"] = GrossAmount, [\"GstAmount\"] = GstAmount };"
                    }
                    """
            }
        },
        ForbiddenOutputPatterns = new() { "new Order(", "new Customer(", "File.Delete", "Process.Start", "DROP TABLE" }
    };

    public static readonly AiSkillMetadata DesignEntity = new()
    {
        SkillName = "DesignEntity",
        Description = "Modifies/extends the current selected entity and/or suggests additional related entities based on a natural language prompt.",
        Category = "SchemaDesign",
        DefaultTemperature = 0.2,
        MaxTokens = 4096,
        ExpectedOutputFormat = "JSON",
        SystemPrompt = LoadPromptResource("DesignEntity.md")
    };

    public static readonly AiSkillMetadata GenerateEntitySchema = new()
    {
        SkillName = "GenerateEntitySchema",
        Description = "Converts a natural-language domain description into an EntityMetadata JSON array.",
        Category = "SchemaDesign",
        DefaultTemperature = 0.2,
        MaxTokens = 4096,
        ExpectedOutputFormat = "JSON",
        SystemPrompt = LoadPromptResource("GenerateEntitySchema.md")
    };

    public static readonly AiSkillMetadata GenerateBusinessRule = new()
    {
        SkillName = "GenerateBusinessRule",
        Description = "Creates a declarative BusinessRuleMetadata object from a condition statement.",
        Category = "CodeGeneration",
        DefaultTemperature = 0.2,
        MaxTokens = 2048,
        ExpectedOutputFormat = "JSON",
        SystemPrompt = LoadPromptResource("GenerateBusinessRule.md")
    };

    public static readonly AiSkillMetadata ExplainLogic = new()
    {
        SkillName = "ExplainLogic",
        Description = "Reverse-engineers a C# businessLogic snippet and explains it in plain English.",
        Category = "Documentation",
        DefaultTemperature = 0.5,
        MaxTokens = 2048,
        ExpectedOutputFormat = "Markdown",
        SystemPrompt = LoadPromptResource("ExplainLogic.md")
    };
}
