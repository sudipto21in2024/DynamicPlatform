# AI Engine Architecture Decision Record (ADR) — v2

## Decision: BYOK (Bring Your Own Key) — Tenant-Scoped AI Provider Model

**Status**: APPROVED  
**Date**: 2026-04-29  
**Revision**: v2 — Updated from platform-managed keys to tenant-managed BYOK model

---

## 1. The Fundamental Business Model

DynamicPlatform is a **multi-tenant low-code platform**. Tenants are businesses that use the platform to build their own applications. In this model:

```
DynamicPlatform (Platform Operator)
        │
        ├── Tenant A (Company ABC) ← brings their OWN OpenAI key
        │       └── Projects: CRM App, Inventory App
        │
        ├── Tenant B (Company XYZ) ← brings their OWN NVIDIA key
        │       └── Projects: HR Portal
        │
        └── Tenant C (Startup PQR) ← brings their OWN Groq key
                └── Projects: Customer Portal
```

**Consequence**: The platform **never holds a platform-wide API key**. Every AI call is made using the **tenant's own credentials**. This is called BYOK — Bring Your Own Key.

---

## 2. Why This Changes Everything

| Concern | Previous Thinking | Correct BYOK Model |
|---|---|---|
| Who owns the API key? | Platform operator | Each tenant |
| Where are keys stored? | `appsettings.json` | DB, encrypted per tenant |
| Who chooses the provider? | Platform config | Tenant, in Studio Settings |
| Who pays the AI bill? | Platform operator | Tenant (costs go to their key) |
| Platform fallback key? | Yes, required | No, or only for free-trial demo accounts |
| Per-tenant isolation | Nice-to-have | **Mandatory** |

---

## 3. Revised Architecture

### 3.1 `TenantAiProvider` — The Core Entity

This entity lives in `Platform.Core.Domain.Entities`. Each tenant can configure **multiple providers** (one for each AI service they use) with different models for different tasks.

```csharp
namespace Platform.Core.Domain.Entities;

/// <summary>
/// Represents an AI provider configured by a tenant (BYOK model).
/// A tenant can configure multiple providers (e.g., OpenAI for code, Groq for quick tasks).
/// </summary>
public class TenantAiProvider
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // --- Tenant Ownership ---
    [Required]
    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    // --- Provider Identity ---
    /// <summary>
    /// Logical name chosen by the tenant. e.g., "My OpenAI", "NVIDIA Fast", "Groq Dev"
    /// </summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI-compatible base URL.
    /// e.g., "https://api.openai.com/v1"
    ///         "https://integrate.api.nvidia.com/v1"
    ///         "https://api.groq.com/openai/v1"
    ///         "http://localhost:11434/v1"  (Ollama local)
    /// </summary>
    [Required, MaxLength(500)]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The tenant's API key, encrypted with AES-256 using a per-tenant encryption key.
    /// The raw key is NEVER stored. Only shown to tenant once on entry (masked thereafter).
    /// </summary>
    [Required]
    public string ApiKeyEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// A masked preview shown in the UI: "sk-...xK9f"
    /// </summary>
    [MaxLength(20)]
    public string ApiKeyPreview { get; set; } = string.Empty;

    /// <summary>
    /// The default model to use. e.g., "gpt-4o", "minimaxai/minimax-m2.7"
    /// </summary>
    [MaxLength(200)]
    public string DefaultModel { get; set; } = string.Empty;

    public int MaxTokens { get; set; } = 8192;
    public double DefaultTemperature { get; set; } = 0.7;
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// If true, this provider is used when no skill specifies a preference.
    /// Only one provider per tenant can be the default.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Result of the last connectivity test: "OK", "AuthError", "Timeout", etc.
    /// </summary>
    [MaxLength(50)]
    public string LastTestStatus { get; set; } = "Untested";
}
```

---

### 3.2 Resolution Logic: Tenant Key Is Always the Source of Truth

The `IAiProviderResolver` has a clear, simple priority chain:

```
Priority 1: AiSkill.PreferredProvider
            → Look up TenantAiProvider where TenantId = currentTenant AND Name = preferredProvider
            
Priority 2: Tenant's Default Provider
            → Look up TenantAiProvider where TenantId = currentTenant AND IsDefault = true
            
Priority 3: Error — No AI provider configured
            → Return clear error: "Please configure an AI provider in Settings → AI Providers"

[NO platform-wide fallback key — by design]
```

```csharp
public class TenantAiProviderResolver : IAiProviderResolver
{
    private readonly ITenantAiProviderRepository _repo;
    private readonly IEncryptionService _encryption;
    private readonly ICurrentTenantService _tenantContext;
    private readonly IHttpClientFactory _httpFactory;

    public async Task<(IAiProvider Provider, string Model)> ResolveAsync(
        string? preferredProviderName,
        string? preferredModel,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("No tenant context. Cannot resolve AI provider.");

        TenantAiProvider? config = null;

        // Priority 1: Skill-specified provider name
        if (!string.IsNullOrEmpty(preferredProviderName))
        {
            config = await _repo.GetByNameAsync(tenantId, preferredProviderName, ct);
        }

        // Priority 2: Tenant's default provider
        config ??= await _repo.GetDefaultAsync(tenantId, ct);

        // Priority 3: Fail clearly
        if (config == null || !config.IsEnabled)
        {
            throw new AiProviderNotConfiguredException(
                "No AI provider is configured for your account. " +
                "Please add your API key in Settings → AI Providers.");
        }

        var decryptedKey = _encryption.Decrypt(config.ApiKeyEncrypted, tenantId.ToString());

        var settings = new AiProviderSettings
        {
            ProviderName = config.Name,
            BaseUrl = config.BaseUrl,
            ApiKey = decryptedKey,
            DefaultModel = config.DefaultModel,
            MaxTokens = config.MaxTokens,
            TimeoutSeconds = config.TimeoutSeconds
        };

        var provider = new OpenAiCompatibleProvider(_httpFactory, settings);
        var model = !string.IsNullOrEmpty(preferredModel) ? preferredModel : config.DefaultModel;

        return (provider, model);
    }
}
```

---

### 3.3 Encryption Strategy

Since these are tenant-owned API keys, security is paramount:

```csharp
public class TenantKeyEncryptionService : IEncryptionService
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Encrypts using AES-256-GCM.
    /// The encryption key is derived from: MasterSecret + TenantId (HKDF).
    /// This means even if the DB is leaked, keys from one tenant cannot decrypt another's.
    /// </summary>
    public string Encrypt(string plaintext, string tenantId)
    {
        var derivedKey = DeriveKey(_config["Encryption:MasterSecret"]!, tenantId);
        // AES-256-GCM encryption...
        return Convert.ToBase64String(ciphertext);
    }

    public string Decrypt(string ciphertext, string tenantId)
    {
        var derivedKey = DeriveKey(_config["Encryption:MasterSecret"]!, tenantId);
        // AES-256-GCM decryption...
        return plaintext;
    }

    private byte[] DeriveKey(string masterSecret, string tenantId)
    {
        // HKDF: master secret + tenantId salt → 32-byte key
        return HKDF.DeriveKey(HashAlgorithmName.SHA256,
            Encoding.UTF8.GetBytes(masterSecret),
            32,
            salt: Encoding.UTF8.GetBytes(tenantId));
    }
}
```

**Key Handling Rules:**
- The raw API key is **never logged**.
- The raw API key is **never returned** in any API response after creation.
- Only `ApiKeyPreview` (e.g., `sk-...xK9f`) is shown in the UI.
- The `MasterSecret` lives in Azure Key Vault / environment variables, never in the DB.

---

## 4. Tenant-Facing UI: "AI Providers" Settings Page

This is a new settings section in Platform Studio, accessible only to the tenant admin:

```
Settings → AI Providers

┌──────────────────────────────────────────────────────────────┐
│  My AI Providers                                  [+ Add New]│
├──────────────────────────────────────────────────────────────┤
│  ✅ DEFAULT  My OpenAI Account                               │
│             Provider: OpenAI-compatible                      │
│             Base URL: https://api.openai.com/v1              │
│             API Key:  sk-...xK9f                             │
│             Model:    gpt-4o                                 │
│             Status:   ✅ Tested OK (2026-04-29)              │
│             [Set as Default] [Test] [Edit] [Delete]          │
├──────────────────────────────────────────────────────────────┤
│             NVIDIA Fast (for quick tasks)                    │
│             Provider: OpenAI-compatible                      │
│             Base URL: https://integrate.api.nvidia.com/v1   │
│             API Key:  nvapi-...m2K9                          │
│             Model:    minimaxai/minimax-m2.7                 │
│             Status:   🕐 Untested                            │
│             [Set as Default] [Test] [Edit] [Delete]          │
└──────────────────────────────────────────────────────────────┘

Add Provider Dialog:
┌─────────────────────────────────────────┐
│  Provider Name (your label):            │
│  [My Groq Account              ]        │
│                                         │
│  Quick Setup:                           │
│  [OpenAI ▼] [NVIDIA ▼] [Groq ▼]        │
│  [Azure OpenAI] [Mistral] [Custom...]   │
│                                         │
│  Base URL:                              │
│  [https://api.groq.com/openai/v1 ]      │
│                                         │
│  API Key:                               │
│  [gsk_•••••••••••••••••••••      ]      │
│                                         │
│  Default Model:                         │
│  [llama-3.3-70b-versatile       ]       │
│                                         │
│  Max Tokens:  [8192]                    │
│  Timeout (s): [120 ]                    │
│                                         │
│  ☑ Set as default provider              │
│                                         │
│          [Test Connection]  [Save]       │
└─────────────────────────────────────────┘
```

### 4.1 "Test Connection" API

Before saving, the platform tests the key with a minimal, cheap call:

```csharp
[HttpPost("api/ai-providers/test")]
public async Task<IActionResult> TestConnection([FromBody] TestProviderRequest request)
{
    var testRequest = new AiRequest
    {
        SystemPrompt = "You are a test agent. Reply with only the word: OK",
        UserPrompt = "Confirm you are reachable.",
        MaxTokens = 10,
        Temperature = 0
    };

    var settings = new AiProviderSettings
    {
        BaseUrl = request.BaseUrl,
        ApiKey = request.ApiKey, // raw key, only for this test — never stored raw
        DefaultModel = request.DefaultModel
    };

    try
    {
        var provider = new OpenAiCompatibleProvider(_httpFactory, settings);
        var result = await provider.CompleteAsync(testRequest);
        return Ok(new { status = "OK", response = result });
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        return Ok(new { status = "AuthError", message = "Invalid API key." });
    }
    catch (Exception ex)
    {
        return Ok(new { status = "Error", message = ex.Message });
    }
}
```

---

## 5. AI Skill → Provider Routing in BYOK Context

With BYOK, the `AiSkill.PreferredProvider` becomes a **tenant-relative name**, not a global name:

```json
// AiSkill artifact stored in DB
{
  "skillName": "GenerateConnectorLogic",
  "preferredProvider": "NVIDIA Fast",   // ← Tenant's own label
  "preferredModel": "minimaxai/minimax-m2.7",
  "defaultTemperature": 0.3,
  "systemPrompt": "..."
}
```

When another tenant uses the same skill, `"NVIDIA Fast"` resolves to **their** TenantAiProvider with that name, not the original tenant's. Skills are project-level artifacts; provider configs are tenant-level settings. The two are linked only at runtime by name.

---

## 6. What If a Tenant Has No AI Provider?

The platform degrades gracefully:

```csharp
catch (AiProviderNotConfiguredException ex)
{
    return StatusCode(402, new
    {
        error = "AI_PROVIDER_REQUIRED",
        message = "AI features require an API key. Please configure your AI provider.",
        settingsUrl = "/settings/ai-providers",
        learnMoreUrl = "https://docs.dynamicplatform.io/ai-setup"
    });
}
```

In the Studio UI, any AI-powered button (✨ Generate) shows a prompt:

```
⚠️ No AI Provider Configured
To use AI features, add your API key.
[Go to Settings → AI Providers]
```

---

## 7. Revised "What Goes Where" — Final Answer

| Component | Location | Notes |
|---|---|---|
| `TenantAiProvider` entity | `Platform.Core/Domain/Entities/` | Owned by tenant, not platform |
| `TenantAiProvider` DB table | `Platform.Infrastructure/` | EF migration required |
| `ITenantAiProviderRepository` | `Platform.Core/Interfaces/` | Repo contract |
| `IAiProvider` (code interface) | `Platform.Core/Interfaces/` | Unchanged |
| `OpenAiCompatibleProvider` | `Platform.Engine/Services/Ai/` | One impl for all providers |
| `TenantAiProviderResolver` | `Platform.Engine/Services/Ai/` | BYOK resolver |
| `TenantKeyEncryptionService` | `Platform.Infrastructure/Services/` | AES-256-GCM + HKDF |
| `AiProviderController` | `Platform.API/Controllers/` | Tenant CRUD for their own keys |
| Studio: Settings → AI Providers | `platform-studio` | Tenant admin UI |
| `AiSkill` artifacts | Existing `Artifact` table | `ArtifactType.AiSkill = 12` |

> **There is no platform-wide API key.**  
> The platform operator runs the infrastructure. Tenants pay for their own AI usage directly.  
> This is the correct, scalable, and legally clean model for a multi-tenant SaaS platform.
