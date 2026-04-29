# AI Integration Architecture — DynamicPlatform

> **Scope**: This document defines the architecture for integrating any OpenAI-compatible AI provider into DynamicPlatform to generate complex business logic, enrich CRUD applications, and create intelligent runtime activities.

---

## 1. Core Design Principle: Provider Agnosticism

The AI subsystem must be **provider-agnostic**. Any LLM offering an OpenAI-compatible API endpoint (NVIDIA NIM, OpenAI, Azure OpenAI, Groq, Mistral, Ollama, Anthropic via proxy) must work without code changes.

The platform uses a single abstraction: `IAiProvider`.

```
Platform Studio (UI)
       │
       ▼
 AiController (API Layer)
       │
       ▼
 IAiProvider (Abstraction)
   ├── OpenAiCompatibleProvider  ← NVIDIA, OpenAI, Groq, Mistral, etc.
   ├── AzureOpenAiProvider       ← Azure-specific auth/endpoint format
   └── [Future providers]
       │
       ▼
 ISchemaContextExtractor         ← Reads DB artifacts → builds system prompt
       │
       ▼
 IPromptSkillLibrary             ← Loads skill/rules/templates from DB
       │
       ▼
 IAiResponseParser               ← Validates + normalizes output
```

---

## 2. Core Interfaces & Models

### 2.1 IAiProvider

```csharp
/// <summary>
/// Abstraction for any OpenAI-compatible AI provider.
/// Implementations handle provider-specific auth and endpoint differences.
/// </summary>
public interface IAiProvider
{
    string ProviderName { get; }
    Task<string> CompleteAsync(AiRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> CompleteStreamAsync(AiRequest request, CancellationToken ct = default);
}

public class AiRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public List<AiMessage> History { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.95;
    public int MaxTokens { get; set; } = 8192;
    public string? Model { get; set; } // Override default model per request
}

public class AiMessage
{
    public string Role { get; set; } = "user"; // "user" | "assistant" | "system"
    public string Content { get; set; } = string.Empty;
}
```

### 2.2 OpenAI-Compatible Provider Implementation

```csharp
public class OpenAiCompatibleProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly AiProviderSettings _settings;
    private readonly ILogger<OpenAiCompatibleProvider> _logger;

    public string ProviderName => _settings.ProviderName;

    public OpenAiCompatibleProvider(
        IHttpClientFactory factory,
        IOptions<AiProviderSettings> settings,
        ILogger<OpenAiCompatibleProvider> logger)
    {
        _http = factory.CreateClient("AiProvider");
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(AiRequest request, CancellationToken ct = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt }
        };
        messages.AddRange(request.History.Select(h => new { role = h.Role, content = h.Content }));
        messages.Add(new { role = "user", content = request.UserPrompt });

        var body = new
        {
            model = request.Model ?? _settings.DefaultModel,
            messages,
            temperature = request.Temperature,
            top_p = request.TopP,
            max_tokens = request.MaxTokens,
            stream = false
        };

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await _http.PostAsJsonAsync(
            $"{_settings.BaseUrl}/chat/completions", body, ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
        return result!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> CompleteStreamAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // SSE stream implementation (stream: true)
        // Parses 'data: {...}' chunks and yields delta.content
        // Implementation follows the same pattern as above with stream=true
        yield return await Task.FromResult(string.Empty); // placeholder
    }
}
```

### 2.3 Configuration

```json
// appsettings.json
"AiSettings": {
  "ProviderName": "NVIDIA",
  "BaseUrl": "https://integrate.api.nvidia.com/v1",
  "ApiKey": "nvapi-...",
  "DefaultModel": "minimaxai/minimax-m2.7",
  "MaxTokens": 8192,
  "TimeoutSeconds": 120
}
```

```csharp
// To switch to OpenAI:
"AiSettings": {
  "ProviderName": "OpenAI",
  "BaseUrl": "https://api.openai.com/v1",
  "ApiKey": "sk-...",
  "DefaultModel": "gpt-4o",
  "MaxTokens": 8192
}

// To switch to Groq:
"AiSettings": {
  "ProviderName": "Groq",
  "BaseUrl": "https://api.groq.com/openai/v1",
  "ApiKey": "gsk_...",
  "DefaultModel": "llama-3.3-70b-versatile",
  "MaxTokens": 8192
}
```

No code changes are required to swap providers. Only config changes.

---

## 3. Supported Artifact Types for AI Generation

The platform's `ArtifactType` enum maps directly to what the AI can generate or enrich:

| ArtifactType | AI Use Case | Output Format |
|---|---|---|
| `Entity (1)` | Generate entity schemas from NL descriptions | `EntityMetadata[]` JSON |
| `Connector (5)` | Generate C# business logic snippets | `ConnectorMetadata.BusinessLogic` string |
| `Workflow (3)` | Generate Elsa workflow definitions | Elsa workflow JSON definition |
| `Form (10)` | Generate form layouts from entity schema | `FormMetadata` JSON |
| `Page (2)` | Suggest widget layouts for a page | `PageMetadata` JSON |
| `SecurityConfig (6)` | Generate role-based rules | `SecurityMetadata` JSON |
| **`AiSkill` (New: 12)** | Store prompt templates, rules, system prompts | `AiSkillMetadata` JSON |
| **`AiSession` (New: 13)** | Persist multi-turn conversations per project | `AiSessionMetadata` JSON |

> **Action Required**: Add `AiSkill = 12` and `AiSession = 13` to the `ArtifactType` enum in `Artifact.cs`.

---

## 4. New Entities Required in the Platform

### 4.1 AiSkillMetadata (stored as Artifact.Content JSON)

```csharp
public class AiSkillMetadata
{
    public string SkillName { get; set; } = string.Empty;  // e.g., "GenerateConnectorLogic"
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "CodeGeneration"; // CodeGeneration | DataAnalysis | DocumentGeneration | SchemaDesign
    public string SystemPrompt { get; set; } = string.Empty; // The authoritative system instructions
    public string GuidePrompt { get; set; } = string.Empty;  // The structure/format guide injected as assistant primer
    public List<string> Rules { get; set; } = new();         // Ordered list of hard rules
    public List<AiExample> FewShotExamples { get; set; } = new(); // Input→Output examples
    public string PreferredProvider { get; set; } = string.Empty;  // e.g., "OpenAI" - blank = use default
    public string PreferredModel { get; set; } = string.Empty;     // e.g., "gpt-4o" - blank = use default
    public double DefaultTemperature { get; set; } = 0.3;
}

public class AiExample
{
    public string UserInput { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
}
```

### 4.2 AiSessionMetadata

```csharp
public class AiSessionMetadata
{
    public string SkillName { get; set; } = string.Empty;  // Which skill drove this session
    public List<AiMessage> History { get; set; } = new(); // Full conversation history
    public string FinalOutput { get; set; } = string.Empty; // The accepted result
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active"; // Active | Accepted | Rejected
}
```

---

## 5. Service Registration

```csharp
// Program.cs / DI Setup
builder.Services.Configure<AiProviderSettings>(
    builder.Configuration.GetSection("AiSettings"));

// Register the provider
builder.Services.AddHttpClient("AiProvider", (sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<AiProviderSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

builder.Services.AddScoped<IAiProvider, OpenAiCompatibleProvider>();
builder.Services.AddScoped<ISchemaContextExtractor, SchemaContextExtractor>();
builder.Services.AddScoped<IPromptSkillLibrary, DbPromptSkillLibrary>();
builder.Services.AddScoped<IAiOrchestrator, AiOrchestrator>();
```

---

## 6. The AiOrchestrator — Putting It Together

```csharp
public class AiOrchestrator : IAiOrchestrator
{
    private readonly IAiProvider _provider;
    private readonly ISchemaContextExtractor _contextExtractor;
    private readonly IPromptSkillLibrary _skillLibrary;

    public async Task<AiGenerationResult> GenerateAsync(
        Guid projectId,
        string skillName,
        string userPrompt,
        CancellationToken ct = default)
    {
        // 1. Load the skill definition from the DB
        var skill = await _skillLibrary.GetSkillAsync(skillName);

        // 2. Extract the schema context for this project
        var schemaContext = await _contextExtractor.ExtractAsync(projectId);

        // 3. Build the full system prompt
        var systemPrompt = BuildSystemPrompt(skill, schemaContext);

        // 4. Call the AI
        var request = new AiRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Temperature = skill.DefaultTemperature,
            Model = string.IsNullOrEmpty(skill.PreferredModel) ? null : skill.PreferredModel
        };

        var rawOutput = await _provider.CompleteAsync(request, ct);

        // 5. Parse and validate
        return ParseAndValidate(rawOutput, skill);
    }
}
```
