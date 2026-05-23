using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Core.Interfaces;

/// <summary>
/// Represents a request to an AI provider.
/// </summary>
public class AiRequest
{
    /// <summary>The authoritative system-level instructions for the AI.</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>The user's natural-language input or business requirement.</summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>Optional prior conversation turns for multi-turn sessions.</summary>
    public List<AiMessage> History { get; set; } = new();

    /// <summary>Sampling temperature (0 = deterministic, 1 = creative).</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>Nucleus sampling parameter.</summary>
    public double TopP { get; set; } = 0.95;

    /// <summary>Maximum tokens to generate in the response.</summary>
    public int MaxTokens { get; set; } = 8192;

    /// <summary>Optional model override. If null, provider's DefaultModel is used.</summary>
    public string? Model { get; set; }
}

/// <summary>
/// A single message turn in a conversation history.
/// </summary>
public class AiMessage
{
    /// <summary>"system" | "user" | "assistant"</summary>
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Abstraction for any OpenAI-compatible AI provider.
/// The single implementation (OpenAiCompatibleProvider) works for NVIDIA, OpenAI, Groq, etc.
/// Provider selection and credentials are resolved from TenantAiProvider DB records at runtime.
/// </summary>
public interface IAiProvider
{
    /// <summary>The logical name of this provider instance (e.g., "NVIDIA", "My OpenAI").</summary>
    string ProviderName { get; }

    /// <summary>
    /// Sends a completion request and returns the full response text.
    /// </summary>
    Task<string> CompleteAsync(AiRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a completion request and streams response tokens as they arrive.
    /// </summary>
    IAsyncEnumerable<string> CompleteStreamAsync(AiRequest request, CancellationToken ct = default);
}
