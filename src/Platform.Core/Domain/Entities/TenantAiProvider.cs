using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Core.Domain.Entities;

/// <summary>
/// Represents an AI provider configured by a tenant — BYOK (Bring Your Own Key) model.
/// A tenant can configure multiple providers and designate one as default.
/// API keys are stored encrypted; the raw key is never persisted or returned via API.
/// </summary>
public class TenantAiProvider
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // --- Ownership ---
    [Required]
    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Tenant-chosen label. e.g., "My OpenAI", "NVIDIA Fast", "Groq Dev"
    /// Used by AiSkill.PreferredProvider to look up this record at runtime.
    /// </summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI-compatible base URL (no trailing slash).
    /// Examples:
    ///   https://integrate.api.nvidia.com/v1
    ///   https://api.openai.com/v1
    ///   https://api.groq.com/openai/v1
    ///   https://api.mistral.ai/v1
    ///   http://localhost:11434/v1  (Ollama)
    /// </summary>
    [Required, MaxLength(500)]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// AES-256 encrypted API key. Decrypted only at the moment of an AI call.
    /// Never returned in any API response.
    /// </summary>
    [Required]
    public string ApiKeyEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Masked preview shown in Studio UI. e.g., "nvapi-...m2K9"
    /// Set once on creation from the raw key, never updated.
    /// </summary>
    [MaxLength(30)]
    public string ApiKeyPreview { get; set; } = string.Empty;

    /// <summary>
    /// Default model identifier for this provider.
    /// e.g., "minimaxai/minimax-m2.7", "gpt-4o", "llama-3.3-70b-versatile"
    /// </summary>
    [Required, MaxLength(200)]
    public string DefaultModel { get; set; } = string.Empty;

    public int MaxTokens { get; set; } = 8192;

    public double DefaultTemperature { get; set; } = 0.7;

    public double DefaultTopP { get; set; } = 0.95;

    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// When true, this provider is used when no skill specifies a PreferredProvider.
    /// Only one TenantAiProvider per tenant should have IsDefault=true (enforced by service layer).
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastTestedAt { get; set; }

    /// <summary>Last connectivity test result: "Untested" | "OK" | "AuthError" | "Timeout" | "Error"</summary>
    [MaxLength(50)]
    public string LastTestStatus { get; set; } = "Untested";
}
