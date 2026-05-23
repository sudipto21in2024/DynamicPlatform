# AI Engine Implementation — Complete Change Log

**Date**: 2026-04-29  
**Status**: Implemented & Build-Verified (0 errors)  
**Scope**: Full AI subsystem added to DynamicPlatform — provider-agnostic, BYOK, schema-aware.

---

## 1. Summary of All Changes

| # | File | Action | Purpose |
|---|---|---|---|
| 1 | `Platform.Core/Interfaces/IAiProvider.cs` | **New** | Core interface + request models |
| 2 | `Platform.Core/Interfaces/ITenantAiProviderRepository.cs` | **New** | Repository contract for BYOK keys |
| 3 | `Platform.Core/Domain/Entities/TenantAiProvider.cs` | **New** | DB entity for tenant AI keys |
| 4 | `Platform.Core/Domain/Entities/Artifact.cs` | **Modified** | Added `AiSkill=12`, `AiSession=13` to enum |
| 5 | `Platform.Engine/Models/AiSkillMetadata.cs` | **New** | Skill definition model |
| 6 | `Platform.Engine/Services/Ai/OpenAiCompatibleProvider.cs` | **New** | Single provider impl (NVIDIA/OpenAI/Groq/etc.) |
| 7 | `Platform.Engine/Services/Ai/TenantAiProviderResolver.cs` | **New** | BYOK runtime resolver |
| 8 | `Platform.Engine/Services/Ai/SchemaContextExtractor.cs` | **New** | DB → AI context document builder |
| 9 | `Platform.Engine/Services/Ai/AiSkillLibrary.cs` | **New** | Skill catalog + built-in skill definitions |
| 10 | `Platform.Infrastructure/Data/PlatformDbContext.cs` | **Modified** | Added `TenantAiProviders` DbSet + EF config |
| 11 | `Platform.Infrastructure/Data/Repositories/TenantAiProviderRepository.cs` | **New** | EF Core repository implementation |
| 12 | `Platform.API/Controllers/AiController.cs` | **Rewritten** | Full pipeline controller |
| 13 | `Platform.API/appsettings.json` | **Modified** | Added `AiSettings` block (NVIDIA dev fallback) |
| 14 | `Platform.API/Program.cs` | **Modified** | DI registrations for all AI services |

---

## 2. File-by-File Detail

---

### 2.1 `IAiProvider.cs` — NEW
**Location**: `src/Platform.Core/Interfaces/`

The foundational interface that all provider implementations must satisfy. Defines both a synchronous completion and an async streaming method, matching the Python OpenAI SDK's `stream=True/False` parameter exactly.

```
AiRequest        → Carries: SystemPrompt, UserPrompt, History[], Temperature, TopP, MaxTokens, Model?
AiMessage        → { Role: "system|user|assistant", Content: string }
IAiProvider      → CompleteAsync()         ← non-streaming, returns full string
                 → CompleteStreamAsync()   ← streaming, yields tokens via IAsyncEnumerable<string>
```

**Key design decision**: The interface is in `Platform.Core` — the lowest layer — so it has no dependency on any HTTP client, config, or DB. Any layer can inject `IAiProvider` without circular references.

---

### 2.2 `ITenantAiProviderRepository.cs` — NEW
**Location**: `src/Platform.Core/Interfaces/`

Repository contract for the `TenantAiProvider` entity. Methods:

| Method | Description |
|---|---|
| `GetByTenantAsync(tenantId)` | All providers for a tenant, ordered Default first |
| `GetByNameAsync(tenantId, name)` | Lookup by tenant-chosen label (used by skill routing) |
| `GetDefaultAsync(tenantId)` | The IsDefault=true provider for a tenant |
| `AddAsync / UpdateAsync / DeleteAsync` | Standard CRUD |
| `ClearDefaultAsync(tenantId)` | Sets IsDefault=false on all providers before setting a new default |

---

### 2.3 `TenantAiProvider.cs` — NEW
**Location**: `src/Platform.Core/Domain/Entities/`

A platform-level DB entity (same tier as `Tenant`, `Project`). **Not an Artifact** — it is infrastructure, not user design data.

Key fields:

| Field | Type | Description |
|---|---|---|
| `TenantId` | Guid | FK to Tenant — strictly tenant-scoped |
| `Name` | string | Tenant-chosen label e.g., "My OpenAI", "NVIDIA Fast" |
| `BaseUrl` | string | OpenAI-compatible endpoint URL |
| `ApiKeyEncrypted` | string | Phase 1: stored as-is. Phase 2: AES-256-GCM encrypted |
| `ApiKeyPreview` | string | Masked display: `"nvapi-...m2K9"` — never updated after creation |
| `DefaultModel` | string | e.g., `"minimaxai/minimax-m2.7"`, `"gpt-4o"` |
| `IsDefault` | bool | Used when no skill specifies a preferred provider |
| `LastTestStatus` | string | `"Untested"` → `"OK"` or `"Error"` after test endpoint |

**DB constraints** (configured in `PlatformDbContext`):
- FK to `Tenants` with cascade delete
- Unique index on `(TenantId, Name)` — one label per tenant

---

### 2.4 `Artifact.cs` — MODIFIED
**Location**: `src/Platform.Core/Domain/Entities/`

Two new enum values added to `ArtifactType`:

```csharp
AiSkill  = 12  // Prompt templates, rules, system prompts (the Skill Library)
AiSession = 13  // Conversation history for a specific AI generation session
```

`AiSkill` artifacts store `AiSkillMetadata` JSON. They are project-level, meaning tenants can customize AI behavior per project without touching platform code. `AiSession` stores conversation history for audit and replay.

---

### 2.5 `AiSkillMetadata.cs` — NEW
**Location**: `src/Platform.Engine/Models/`

The schema for a skill stored as `ArtifactType.AiSkill`. A skill packages everything needed for one specific AI task:

| Field | Purpose |
|---|---|
| `SkillName` | Unique identifier e.g., `"GenerateConnectorLogic"` |
| `SystemPrompt` | Authoritative role + format contract for the AI |
| `Rules` | Numbered hard constraints injected into the system prompt |
| `GuidePrompt` | Primer injected as first `"assistant"` turn — locks format compliance |
| `FewShotExamples` | `List<AiSkillExample>` injected as alternating `user/assistant` history turns |
| `PreferredProvider` | Optional: routes to a specific `TenantAiProvider.Name` |
| `PreferredModel` | Optional: model override e.g., `"gpt-4o"` for code, fast model for rules |
| `ForbiddenOutputPatterns` | Safety list checked by output validator |
| `ExpectedOutputFormat` | `"JSON"` \| `"CSharp"` \| `"Markdown"` — drives output cleaning |

---

### 2.6 `OpenAiCompatibleProvider.cs` — NEW
**Location**: `src/Platform.Engine/Services/Ai/`

**The single HTTP implementation** for ALL OpenAI-compatible providers. Works with:
- NVIDIA NIM (`https://integrate.api.nvidia.com/v1`)
- OpenAI (`https://api.openai.com/v1`)
- Groq (`https://api.groq.com/openai/v1`)
- Mistral (`https://api.mistral.ai/v1`)
- Azure OpenAI (`https://<resource>.openai.azure.com/openai/deployments/<model>`)
- Ollama local (`http://localhost:11434/v1`)

**Non-streaming** (`CompleteAsync`):
```
POST {BaseUrl}/chat/completions
  body: { model, messages, temperature, top_p, max_tokens, stream: false }
  Authorization: Bearer {ApiKey}
→ Parse: choices[0].message.content
```

**Streaming** (`CompleteStreamAsync`):
```
POST {BaseUrl}/chat/completions
  body: { ..., stream: true }
  HttpCompletionOption.ResponseHeadersRead  ← starts reading before full body buffered
→ Read SSE line by line: "data: {...}"
→ Parse: choices[0].delta.content
→ Guard: skip if choices is empty (metadata chunks from some providers)
→ Yield: each non-null content token
→ Stop: when "data: [DONE]" is received
```

The Python SDK pattern maps exactly:
```python
# Python (reference)                    # C# (implementation)
for chunk in completion:                await foreach (var token in provider.CompleteStreamAsync(...))
  if chunk.choices[0].delta.content:      if (!string.IsNullOrEmpty(token))
    print(content, end="")                  yield return token;
```

Also defines three exception types used by the controller:
- `AiProviderException` — HTTP/parse errors from the provider
- `AiProviderNotConfiguredException` — no provider configured for the tenant
- `AiProviderSettings` — the runtime settings bag passed in by the resolver

---

### 2.7 `TenantAiProviderResolver.cs` — NEW
**Location**: `src/Platform.Engine/Services/Ai/`

Resolves which `IAiProvider` instance and model to use at runtime. Priority chain:

```
Priority 1: skill.PreferredProvider (e.g., "NVIDIA Fast")
            → GetByNameAsync(tenantId, "NVIDIA Fast")
            
Priority 2: Tenant's IsDefault provider
            → GetDefaultAsync(tenantId)
            
Priority 3: appsettings["AiSettings"] block
            → Used in development when no DB record exists
            → Logs a WARNING (indicates missing tenant setup)
            
Priority 4 (none): AiProviderNotConfiguredException
            → Controller returns HTTP 402 with settingsUrl
```

The resolver instantiates `OpenAiCompatibleProvider` **dynamically** per-request from DB-loaded settings. This means the provider is never a singleton — each request gets a fresh instance configured with the correct tenant's API key.

---

### 2.8 `SchemaContextExtractor.cs` — NEW
**Location**: `src/Platform.Engine/Services/Ai/`

**This is what makes the AI "see" your project.** It reads all `Artifact` records for a `projectId` and formats them into a structured markdown "Platform Knowledge Document" appended to the system prompt.

**What it reads and formats:**

| Artifact Type | How it's formatted |
|---|---|
| `Entity` | Markdown table: `\| Field \| C# Type \| Required \| MaxLength \|` + Relations list |
| `Enum` | Inline: `OrderStatus: [Pending=0, Active=1, Closed=2]` |
| `Connector` | Summary: name, description, inputs, outputs |
| `Workflow` | Name list only |
| `Form` | Name list only |
| Business Rules | `IF condition THEN action` format |

**Effect**: When a user asks the AI to *"calculate loyalty discount on Order using Customer.LoyaltyPoints"*, the AI already knows from the context that:
- `Order.TotalAmount` is `decimal`
- `Customer.LoyaltyPoints` is `int`
- `Order` has a `ManyToOne` relation to `Customer` via `Order.CustomerId`

Without the context, the AI would guess — and likely use wrong field names, causing the generated code to fail compilation.

---

### 2.9 `AiSkillLibrary.cs` — NEW
**Location**: `src/Platform.Engine/Services/Ai/`

Loads skill definitions with a two-tier lookup:

```
1. Check: Artifact[Type=AiSkill, Name=skillName] for this projectId
          → Tenants can override any built-in skill per project
          
2. Fallback: BuiltInSkills static catalog (compiled into the DLL)
```

**Built-in skills:**

| Skill Name | Category | Temp | Purpose |
|---|---|---|---|
| `GenerateConnectorLogic` | CodeGeneration | 0.3 | NL → ConnectorMetadata + C# businessLogic |
| `GenerateEntitySchema` | SchemaDesign | 0.2 | NL → EntityMetadata[] |
| `GenerateBusinessRule` | CodeGeneration | 0.2 | NL → BusinessRuleMetadata |
| `ExplainLogic` | Documentation | 0.5 | C# snippet → plain English explanation |

Each built-in skill includes:
- Full system prompt with role, rules, and output format contract
- `GuidePrompt` assistant primer for format-enforcing
- `FewShotExamples` (especially on `GenerateConnectorLogic`) for consistency
- `ForbiddenOutputPatterns` list for safety validation

---

### 2.10 `PlatformDbContext.cs` — MODIFIED
**Location**: `src/Platform.Infrastructure/Data/`

Two additions:

```csharp
// New DbSet
public DbSet<TenantAiProvider> TenantAiProviders { get; set; }

// New OnModelCreating config
modelBuilder.Entity<TenantAiProvider>()
    .HasOne(p => p.Tenant)
    .WithMany()
    .HasForeignKey(p => p.TenantId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<TenantAiProvider>()
    .HasIndex(p => new { p.TenantId, p.Name })
    .IsUnique();
```

The `TenantAiProviders` table is created automatically at startup via `db.Database.EnsureCreated()` (existing pattern).

---

### 2.11 `TenantAiProviderRepository.cs` — NEW
**Location**: `src/Platform.Infrastructure/Data/Repositories/`

Standard EF Core repository. Notable implementation detail in `ClearDefaultAsync`:

```csharp
// Called before adding a new default provider to prevent multiple IsDefault=true records
public async Task ClearDefaultAsync(Guid tenantId, CancellationToken ct)
{
    var providers = await _db.TenantAiProviders
        .Where(p => p.TenantId == tenantId && p.IsDefault)
        .ToListAsync(ct);
    foreach (var p in providers) p.IsDefault = false;
    await _db.SaveChangesAsync(ct);
}
```

---

### 2.12 `AiController.cs` — REWRITTEN
**Location**: `src/Platform.API/Controllers/`

Complete replacement of the old Gemini-only controller. All endpoints now use the full pipeline:
`AiSkillLibrary → SchemaContextExtractor → TenantAiProviderResolver → OpenAiCompatibleProvider`

**New Endpoints:**

| Method | Endpoint | Skill | Description |
|---|---|---|---|
| POST | `/api/ai/generate-schema` | `GenerateEntitySchema` | NL → EntityMetadata[] |
| POST | `/api/ai/generate-connector` | `GenerateConnectorLogic` | NL → ConnectorMetadata + C# |
| POST | `/api/ai/generate-rule` | `GenerateBusinessRule` | NL → BusinessRuleMetadata |
| POST | `/api/ai/explain-logic` | `ExplainLogic` | C# → plain English |
| POST | `/api/ai/complete` | (raw) | Streaming SSE for Playground |
| GET  | `/api/ai/skills` | — | Lists built-in skill catalog |
| POST | `/api/ai/providers/test` | — | Tests a key before saving |
| GET  | `/api/ai/providers` | — | Lists tenant's registered keys |
| POST | `/api/ai/providers` | — | Registers a new BYOK key |
| DELETE | `/api/ai/providers/{id}` | — | Removes a key |

**Central `ExecuteSkillAsync` method** — all generation endpoints call this single method which implements the full 8-step pipeline with consistent error handling.

**Output cleaning** — strips markdown fences (` ```json `, ` ``` `) and trims to valid JSON boundaries. Handles all known model quirks where providers add prose before/after the JSON despite instructions.

**Request DTO** — `AiPromptRequest` has an optional `ProjectId` field. When provided, `SchemaContextExtractor` fetches that project's artifacts for context. Without it, the AI operates context-free (useful for initial schema generation when no project exists yet).

---

### 2.13 `appsettings.json` — MODIFIED
**Location**: `src/Platform.API/`

Added `AiSettings` block as the **development fallback** provider. This is used only when:
- The tenant has no `TenantAiProvider` records in the DB, AND
- `TenantAiProviderResolver` has no DB entry to resolve

```json
"AiSettings": {
  "ProviderName": "NVIDIA-Dev",
  "BaseUrl": "https://integrate.api.nvidia.com/v1",
  "ApiKey": "nvapi-...",
  "DefaultModel": "minimaxai/minimax-m2.7",
  "MaxTokens": 8192
}
```

> ⚠️ **Note**: Move the API key to `appsettings.Development.json` and add it to `.gitignore` before committing. In production, use environment variables or Azure Key Vault.

---

### 2.14 `Program.cs` — MODIFIED
**Location**: `src/Platform.API/`

Added registrations in the AI Services block:

```csharp
// Named HttpClient for AI calls — configures timeout from settings
builder.Services.AddHttpClient("AiProvider", (sp, client) => {
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("AiSettings:TimeoutSeconds", 120));
});

// Repository
builder.Services.AddScoped<ITenantAiProviderRepository, TenantAiProviderRepository>();

// AI Engine Services
builder.Services.AddScoped<TenantAiProviderResolver>();
builder.Services.AddScoped<SchemaContextExtractor>();
builder.Services.AddScoped<AiSkillLibrary>();

// Legacy (kept for backwards compatibility)
builder.Services.AddHttpClient<GeminiService>();
```

---

## 3. The Complete Data Flow

```
POST /api/ai/generate-connector
  Body: { "prompt": "Calculate loyalty discount on Order", "projectId": "abc-123" }
                            │
              ┌─────────────▼─────────────────────────┐
              │ AiController.ExecuteSkillAsync()        │
              └──────────────────┬────────────────────┘
                                 │
          ┌──────────────────────▼──────────────────────────┐
          │ Step 1: AiSkillLibrary.GetSkillAsync(            │
          │           "GenerateConnectorLogic", projectId)   │
          │  → Check DB: Any Artifact[AiSkill, name=skill]? │
          │  → No → Return BuiltInSkills.GenerateConnector  │
          │     skill = { systemPrompt, rules, fewShot... } │
          └──────────────────────┬──────────────────────────┘
                                 │
          ┌──────────────────────▼──────────────────────────┐
          │ Step 2: BuildSystemPrompt(skill)                │
          │  → skill.SystemPrompt + "\nADDITIONAL RULES:\n" │
          │    + "1. Output ONLY JSON...\n2. No entity..."  │
          └──────────────────────┬──────────────────────────┘
                                 │
          ┌──────────────────────▼──────────────────────────┐
          │ Step 3: SchemaContextExtractor.ExtractAsync(     │
          │           projectId = "abc-123")                 │
          │  → DB: SELECT * FROM Artifacts                   │
          │        WHERE ProjectId = 'abc-123'               │
          │  → Deserialize each by Type:                     │
          │    Entity "Order":                               │
          │      | Field       | C# Type | Required |        │
          │      | OrderNumber | string  | True     |        │
          │      | TotalAmount | decimal | True     |        │
          │      | CustomerId  | Guid    | True     |        │
          │    Entity "Customer":                            │
          │      | Name          | string | True  |          │
          │      | LoyaltyPoints | int    | False |          │
          │    Connector "SendEmailConnector":               │
          │      Inputs: ToEmail:string, Subject:string      │
          └──────────────────────┬──────────────────────────┘
                                 │ Context markdown string
          ┌──────────────────────▼──────────────────────────┐
          │ Step 4: BuildFewShotHistory(skill)              │
          │  → [ {role:"assistant", content: GuidePrompt},  │
          │      {role:"user",      content: example.Input},│
          │      {role:"assistant", content: example.Output}]│
          └──────────────────────┬──────────────────────────┘
                                 │
          ┌──────────────────────▼──────────────────────────┐
          │ Step 5: TenantAiProviderResolver.ResolveAsync() │
          │  → skill.PreferredProvider = ""                 │
          │  → GetDefaultAsync(Guid.Empty) → null (dev)     │
          │  → appsettings fallback:                        │
          │    BaseUrl = "https://integrate.api.nvidia.com" │
          │    ApiKey  = "nvapi-..."                        │
          │    Model   = "minimaxai/minimax-m2.7"           │
          └──────────────────────┬──────────────────────────┘
                                 │ provider, model
          ┌──────────────────────▼──────────────────────────┐
          │ Step 6: AiRequest assembly                       │
          │  SystemPrompt = systemPrompt + schemaContext     │
          │  UserPrompt   = "Calculate loyalty discount..."  │
          │  History      = [guidePrompt, fewShot turns]    │
          │  Model        = "minimaxai/minimax-m2.7"         │
          │  Temperature  = 0.3                             │
          └──────────────────────┬──────────────────────────┘
                                 │
          ┌──────────────────────▼──────────────────────────┐
          │ Step 7: OpenAiCompatibleProvider.CompleteAsync() │
          │  POST https://integrate.api.nvidia.com/v1/       │
          │       chat/completions                           │
          │  Authorization: Bearer nvapi-...                 │
          │  { model, messages, temperature:0.3,             │
          │    top_p:0.95, max_tokens:8192, stream:false }   │
          │                                                  │
          │  → AI sees:                                      │
          │    [system] You are C# expert... Rules... Schema │
          │    [assistant] Understood. Output only JSON...   │
          │    [user] Calculate 18% GST... (few-shot)        │
          │    [assistant] { "name":"CalculateGst"... }      │
          │    [user] Calculate loyalty discount on Order... │
          │                                                  │
          │  ← AI outputs:                                   │
          │    { "name":"ApplyLoyaltyDiscount",              │
          │      "inputs":[{"name":"LoyaltyPoints","type":   │
          │        "int"},{"name":"TotalAmount","type":      │
          │        "decimal"}],                              │
          │      "businessLogic":"var discount=0m;\n         │
          │        if(LoyaltyPoints>500)\n  discount=        │
          │        TotalAmount*0.10m;\nreturn discount;" }   │
          └──────────────────────┬──────────────────────────┘
                                 │ raw JSON string
          ┌──────────────────────▼──────────────────────────┐
          │ Step 8: CleanOutput(raw, "JSON")                │
          │  → Strip ```json...``` fences                    │
          │  → Trim to first { / last }                      │
          └──────────────────────┬──────────────────────────┘
                                 │
                        200 OK: ConnectorMetadata JSON
                        → Saved to Artifact table by Studio
                        → At build: Connector.scriban renders .cs file
                        → dotnet build → deployed API
```

---

## 4. Pending Work (Phase 2)

| Item | Priority | Notes |
|---|---|---|
| Encrypt `ApiKeyEncrypted` with AES-256-GCM | **High** | Phase 1 stores plain-text |
| JWT auth middleware → real `GetCurrentTenantId()` | **High** | Currently returns `Guid.Empty` |
| EF Migration for `TenantAiProvider` table | **High** | EnsureCreated works but proper migration needed |
| `AiResponseParser` with forbidden pattern checker | Medium | Safety validation before saving |
| `POST /api/ai/providers/{id}/set-default` endpoint | Medium | Convenience endpoint |
| `AiSession` artifact persistence | Medium | Save conversation history for audit |
| Studio UI: Settings → AI Providers page | Medium | BYOK key management for tenants |
| Studio UI: AI Playground panel | Low | Uses `/api/ai/complete` SSE endpoint |
| Roslyn C# syntax validation in parser | Low | Optional quality gate |
| Per-skill cost tracking | Low | Token count logging per call |
