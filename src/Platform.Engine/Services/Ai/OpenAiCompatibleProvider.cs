using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Platform.Core.Interfaces;

namespace Platform.Engine.Services.Ai;

/// <summary>
/// OpenAI-compatible provider implementation.
/// Works with NVIDIA NIM, OpenAI, Groq, Mistral, Azure OpenAI (v1 endpoint), Ollama, and any other
/// service implementing the /chat/completions API.
/// Instantiated dynamically per-request by TenantAiProviderResolver — NOT registered as a singleton.
/// </summary>
public class OpenAiCompatibleProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly AiProviderSettings _settings;
    private readonly ILogger _logger;

    public string ProviderName => _settings.ProviderName;

    public OpenAiCompatibleProvider(
        IHttpClientFactory httpFactory,
        AiProviderSettings settings,
        ILogger logger)
    {
        _http = httpFactory.CreateClient("AiProvider");
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Sends a non-streaming completion request and returns the full response text.
    /// Matches the Python pattern:
    ///   client.chat.completions.create(model=..., messages=[...], stream=False)
    /// </summary>
    public async Task<string> CompleteAsync(AiRequest request, CancellationToken ct = default)
    {
        var body = BuildRequestBody(request, stream: false);
        using var httpRequest = BuildHttpRequest(body);

        _logger.LogDebug("AI [{Provider}] → model={Model}, tokens={Tokens}",
            ProviderName, request.Model ?? _settings.DefaultModel, request.MaxTokens);

        using var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("AI [{Provider}] error {Status}: {Body}", ProviderName, response.StatusCode, errorBody);
            throw new AiProviderException($"AI provider '{ProviderName}' returned {(int)response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI [{Provider}] failed to parse response: {Json}", ProviderName, json);
            throw new AiProviderException($"Failed to parse response from '{ProviderName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a streaming completion request and yields tokens as they arrive.
    /// Matches the Python pattern:
    ///   completion = client.chat.completions.create(stream=True)
    ///   for chunk in completion:
    ///     if chunk.choices[0].delta.content is not None:
    ///       print(chunk.choices[0].delta.content, end="")
    /// </summary>
    public async IAsyncEnumerable<string> CompleteStreamAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = BuildRequestBody(request, stream: true);
        using var httpRequest = BuildHttpRequest(body);

        // Must use ResponseHeadersRead to start streaming before full response is buffered
        using var response = await _http.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // SSE lines are prefixed with "data: "
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..].Trim();

            // Stream termination signal
            if (data == "[DONE]") break;

            string? token = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var choices = doc.RootElement.GetProperty("choices");

                // Guard: skip if no choices (some providers send metadata chunks)
                if (choices.GetArrayLength() == 0) continue;

                var delta = choices[0].GetProperty("delta");

                // Guard: delta.content may be absent in the last chunk
                if (delta.TryGetProperty("content", out var contentEl) &&
                    contentEl.ValueKind != JsonValueKind.Null)
                {
                    token = contentEl.GetString();
                }
            }
            catch (JsonException)
            {
                // Malformed chunk — skip silently
                continue;
            }

            if (!string.IsNullOrEmpty(token))
                yield return token;
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private object BuildRequestBody(AiRequest request, bool stream)
    {
        var messages = new List<object>();

        // System message
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });

        // Conversation history
        foreach (var msg in request.History)
            messages.Add(new { role = msg.Role, content = msg.Content });

        // Current user message
        messages.Add(new { role = "user", content = request.UserPrompt });

        return new
        {
            model = request.Model ?? _settings.DefaultModel,
            messages,
            temperature = request.Temperature,
            top_p = request.TopP,
            max_tokens = request.MaxTokens,
            stream
        };
    }

    private HttpRequestMessage BuildHttpRequest(object body)
    {
        var json = JsonSerializer.Serialize(body);
        var msg = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        msg.Headers.Add("User-Agent", "DynamicPlatform/1.0");
        return msg;
    }
}

/// <summary>
/// Settings for a single AI provider instance.
/// Populated by TenantAiProviderResolver from the TenantAiProvider DB record.
/// </summary>
public class AiProviderSettings
{
    public string ProviderName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 8192;
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>Thrown when the AI provider returns an error or cannot be reached.</summary>
public class AiProviderException : Exception
{
    public AiProviderException(string message) : base(message) { }
    public AiProviderException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when no AI provider is configured for the current tenant.</summary>
public class AiProviderNotConfiguredException : Exception
{
    public AiProviderNotConfiguredException(string message) : base(message) { }
}
