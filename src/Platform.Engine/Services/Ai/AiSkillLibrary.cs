using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Models;

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
        _                        => BuiltInSkills.GenerateConnectorLogic // safe default
    };

    public static List<AiSkillMetadata> GetAllBuiltInSkills() => new()
    {
        BuiltInSkills.GenerateConnectorLogic,
        BuiltInSkills.GenerateEntitySchema,
        BuiltInSkills.GenerateBusinessRule,
        BuiltInSkills.ExplainLogic
    };
}

/// <summary>
/// Catalog of platform built-in skills. These are the defaults used when no
/// project-level skill artifact overrides them.
/// </summary>
internal static class BuiltInSkills
{
    public static readonly AiSkillMetadata GenerateConnectorLogic = new()
    {
        SkillName = "GenerateConnectorLogic",
        Description = "Converts a business requirement into a ConnectorMetadata JSON with a C# businessLogic snippet.",
        Category = "CodeGeneration",
        DefaultTemperature = 0.3,
        MaxTokens = 8192,
        ExpectedOutputFormat = "JSON",
        SystemPrompt =
            "You are an expert C# developer for DynamicPlatform — a low-code application builder.\n" +
            "Your task is to generate business logic for a Connector — a stateless execution unit.\n\n" +
            "HARD RULES (follow all):\n" +
            "1. Output ONLY a single valid JSON object. No markdown. No prose outside the JSON.\n" +
            "2. The JSON must conform to ConnectorMetadata schema (see OUTPUT FORMAT).\n" +
            "3. The 'businessLogic' field must contain only C# statements — no class or method declarations.\n" +
            "4. Use ONLY input variable names declared in your 'inputs' array.\n" +
            "5. Do NOT instantiate entity classes: no 'new Order()', 'new Customer()', etc.\n" +
            "6. Do NOT access files, processes, or reflection.\n" +
            "7. Always return a typed value. Log key steps using: logger.LogInformation(\"...\").\n" +
            "8. Wrap multi-line logic using \\n escape in the JSON string.\n\n" +
            "OUTPUT FORMAT:\n" +
            "{\n" +
            "  \"name\": \"PascalCaseName\",\n" +
            "  \"namespace\": \"GeneratedApp.Connectors\",\n" +
            "  \"description\": \"One sentence.\",\n" +
            "  \"inputs\": [ { \"name\": \"FieldName\", \"type\": \"csharptype\" } ],\n" +
            "  \"outputs\": [ { \"name\": \"ResultName\", \"type\": \"csharptype\" } ],\n" +
            "  \"configProperties\": [],\n" +
            "  \"businessLogic\": \"// C# statements only\"\n" +
            "}",
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

    public static readonly AiSkillMetadata GenerateEntitySchema = new()
    {
        SkillName = "GenerateEntitySchema",
        Description = "Converts a natural-language domain description into an EntityMetadata JSON array.",
        Category = "SchemaDesign",
        DefaultTemperature = 0.2,
        MaxTokens = 4096,
        ExpectedOutputFormat = "JSON",
        SystemPrompt =
            "You are a Software Architect designing a data model for a business application.\n" +
            "Convert the user's domain description into a JSON array of EntityMetadata objects.\n\n" +
            "HARD RULES:\n" +
            "1. Output ONLY a valid JSON array. No markdown.\n" +
            "2. Do NOT include Id, CreatedAt, or UpdatedAt — they are auto-generated.\n" +
            "3. Field types: string | int | decimal | bool | datetime | guid.\n" +
            "4. For FK references, use type=guid and name it EntityNameId (e.g., CustomerId).\n" +
            "5. Namespace must always be 'GeneratedApp.Entities'.\n" +
            "6. Relations: OneToMany | ManyToOne | ManyToMany.\n" +
            "7. NavPropName must be PascalCase matching the target entity name.\n\n" +
            "OUTPUT FORMAT:\n" +
            "[ { \"name\": \"Entity\", \"namespace\": \"GeneratedApp.Entities\", " +
            "\"fields\": [ { \"name\": \"F\", \"type\": \"string\", \"isRequired\": true, \"maxLength\": 100, \"rules\": [] } ], " +
            "\"relations\": [ { \"targetEntity\": \"Other\", \"type\": \"ManyToOne\", \"navPropName\": \"Other\", \"foreignKeyName\": \"OtherId\" } ] } ]"
    };

    public static readonly AiSkillMetadata GenerateBusinessRule = new()
    {
        SkillName = "GenerateBusinessRule",
        Description = "Creates a declarative BusinessRuleMetadata object from a condition statement.",
        Category = "CodeGeneration",
        DefaultTemperature = 0.2,
        MaxTokens = 2048,
        ExpectedOutputFormat = "JSON",
        SystemPrompt =
            "You are a business rules analyst for DynamicPlatform.\n" +
            "Convert user requirements into BusinessRuleMetadata JSON objects.\n\n" +
            "RULES:\n" +
            "1. Output ONLY valid JSON. No markdown.\n" +
            "2. Trigger: BeforeSave | AfterSave | OnDelete.\n" +
            "3. Condition: simple boolean expression using entity field names.\n" +
            "4. Action: simple mutation — 'Set FieldName = Value'.\n\n" +
            "OUTPUT: { \"name\": \"RuleName\", \"description\": \"...\", \"targetEntity\": \"EntityName\", " +
            "\"trigger\": \"BeforeSave\", \"condition\": \"Field > Value\", \"action\": \"Set OtherField = NewValue\" }"
    };

    public static readonly AiSkillMetadata ExplainLogic = new()
    {
        SkillName = "ExplainLogic",
        Description = "Reverse-engineers a C# businessLogic snippet and explains it in plain English.",
        Category = "Documentation",
        DefaultTemperature = 0.5,
        MaxTokens = 2048,
        ExpectedOutputFormat = "Markdown",
        SystemPrompt =
            "You are a senior code reviewer. Given a C# code snippet from a DynamicPlatform connector, " +
            "explain what it does in plain English. Structure your response as:\n" +
            "1. **What it does** (1 sentence)\n" +
            "2. **Inputs it uses**\n" +
            "3. **Logic steps** (numbered)\n" +
            "4. **Output it returns**\n" +
            "5. **Potential issues or improvements**"
    };
}
