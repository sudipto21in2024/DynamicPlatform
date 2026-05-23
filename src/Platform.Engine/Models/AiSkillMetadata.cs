using System.Collections.Generic;

namespace Platform.Engine.Models;

/// <summary>
/// Defines a reusable AI generation skill stored as ArtifactType.AiSkill.
/// Each skill packages a system prompt, rules, and model preferences for one specific AI task.
/// Skills are project-level artifacts — they travel with the project and can be customized.
/// </summary>
public class AiSkillMetadata
{
    /// <summary>Unique identifier. e.g., "GenerateConnectorLogic", "GenerateEntitySchema"</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>Human-readable description shown in Studio skill picker.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping in the Studio UI.
    /// Values: "CodeGeneration" | "SchemaDesign" | "Documentation" | "DataAnalysis" | "Workflow"
    /// </summary>
    public string Category { get; set; } = "CodeGeneration";

    /// <summary>
    /// The authoritative system-level instruction block.
    /// Defines the AI's role, output format contract, and domain expertise.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of hard rules injected into the system prompt.
    /// Each rule is a numbered constraint the AI must follow.
    /// </summary>
    public List<string> Rules { get; set; } = new();

    /// <summary>
    /// Optional primer injected as an initial "assistant" turn to lock in format compliance.
    /// Effective for models that tend to ignore output format instructions.
    /// </summary>
    public string GuidePrompt { get; set; } = string.Empty;

    /// <summary>
    /// Input→Output examples for few-shot prompting.
    /// Dramatically improves consistency across different model providers.
    /// </summary>
    public List<AiSkillExample> FewShotExamples { get; set; } = new();

    /// <summary>
    /// Optional: route this skill to a specific named TenantAiProvider.
    /// Empty = use the tenant's default provider.
    /// </summary>
    public string PreferredProvider { get; set; } = string.Empty;

    /// <summary>
    /// Optional: override the provider's default model for this specific skill.
    /// e.g., "gpt-4o" for code generation, "llama-3.3-70b-versatile" for quick tasks.
    /// </summary>
    public string PreferredModel { get; set; } = string.Empty;

    /// <summary>Low temperature (0.1-0.4) for deterministic output; higher for creative tasks.</summary>
    public double DefaultTemperature { get; set; } = 0.3;

    public double DefaultTopP { get; set; } = 0.95;

    public int MaxTokens { get; set; } = 8192;

    /// <summary>
    /// Patterns that must NOT appear in the AI output.
    /// Used by IAiResponseParser for safety validation before accepting a result.
    /// </summary>
    public List<string> ForbiddenOutputPatterns { get; set; } = new()
    {
        "new Order(",
        "new Customer(",
        "File.Delete",
        "Process.Start",
        "Assembly.Load",
        "DROP TABLE",
        "SqlCommand"
    };

    /// <summary>Expected output format for parser routing: "JSON" | "CSharp" | "Markdown" | "Text"</summary>
    public string ExpectedOutputFormat { get; set; } = "JSON";
}

/// <summary>A single input→output example for few-shot prompting.</summary>
public class AiSkillExample
{
    public string UserInput { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
}
