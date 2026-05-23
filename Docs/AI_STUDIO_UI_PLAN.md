# Platform Studio — AI Feature UI Plan

**Date**: 2026-04-29  
**Status**: PLANNING  
**Framework**: Angular (existing `platform-studio`)  
**Approach**: Minimal invasive — AI is an enhancement layer on existing pages, not a replacement.

---

## 1. What UI Changes Are Needed

The AI backend is fully implemented. The UI needs **three types of changes**:

| Type | Priority | Effort |
|---|---|---|
| **A. Inline AI Buttons** — "✨ Generate" added to existing designer pages | High | Low |
| **B. New Settings Page** — "AI Providers" for BYOK key management | High | Medium |
| **C. New Pages** — Connector Studio, AI Playground, Skill Library | Medium | High |

---

## 2. New API Service Methods (Required First)

Before any UI component, add these methods to the existing `ApiService` (`services/api.ts`):

```typescript
// ── AI Generation ──────────────────────────────────────────────────────
generateSchema(prompt: string, projectId?: string): Observable<any[]> {
  return this.http.post<any[]>(`${this.apiUrl}/ai/generate-schema`, { prompt, projectId });
}

generateConnector(prompt: string, projectId: string): Observable<any> {
  return this.http.post<any>(`${this.apiUrl}/ai/generate-connector`, { prompt, projectId });
}

generateRule(prompt: string, projectId: string): Observable<any> {
  return this.http.post<any>(`${this.apiUrl}/ai/generate-rule`, { prompt, projectId });
}

explainLogic(codeSnippet: string, projectId: string): Observable<string> {
  return this.http.post<string>(`${this.apiUrl}/ai/explain-logic`,
    { prompt: codeSnippet, projectId });
}

getAiSkills(): Observable<AiSkillSummary[]> {
  return this.http.get<AiSkillSummary[]>(`${this.apiUrl}/ai/skills`);
}

// ── Provider Management (BYOK) ─────────────────────────────────────────
getAiProviders(): Observable<AiProvider[]> {
  return this.http.get<AiProvider[]>(`${this.apiUrl}/ai/providers`);
}

addAiProvider(provider: AddProviderRequest): Observable<any> {
  return this.http.post<any>(`${this.apiUrl}/ai/providers`, provider);
}

deleteAiProvider(id: string): Observable<void> {
  return this.http.delete<void>(`${this.apiUrl}/ai/providers/${id}`);
}

testAiProvider(req: TestProviderRequest): Observable<TestResult> {
  return this.http.post<TestResult>(`${this.apiUrl}/ai/providers/test`, req);
}

// ── Streaming (Playground) ─────────────────────────────────────────────
streamCompletion(systemPrompt: string, userPrompt: string): EventSource {
  // Uses native EventSource for SSE
  return new EventSource(`${this.apiUrl}/ai/complete`); // POST via fetch for body
}
```

---

## 3. Type A — Inline AI Buttons on Existing Pages (High Priority, Low Effort)

These require minimal changes to existing pages. An "✨ AI Generate" button is added as a secondary action. No new routes needed.

---

### 3.1 Entity Designer — "✨ AI Generate Entities"
**Existing page**: `pages/entity-designer/entity-designer.ts`

**Current state**: User manually creates entities and fields.  
**Change**: Add a button that opens a modal, user types a description, AI returns entities pre-populated in the designer.

```
┌─────────────────────────────────────────────────────┐
│  Entity Designer                                    │
│  [+ Add Entity]  [✨ Generate with AI]              │ ← NEW button
├─────────────────────────────────────────────────────┤
│  ...existing entity canvas...                       │
└─────────────────────────────────────────────────────┘
```

**AI Generate Modal:**
```
┌──────────────────────────────────────────────────────┐
│  ✨ Generate Entities with AI                    [✕] │
├──────────────────────────────────────────────────────┤
│  Describe your domain:                               │
│  ┌──────────────────────────────────────────────┐    │
│  │ An e-commerce system with products,          │    │
│  │ categories, orders, customers and reviews    │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  ☑ Use existing project entities as context          │ ← passes projectId
│                                                      │
│                    [Cancel]  [✨ Generate]            │
├──────────────────────────────────────────────────────┤
│  Preview (editable before accepting):                │
│  ┌──────────────────────────────────────────────┐    │
│  │  ✅ Product   (5 fields)                     │    │
│  │  ✅ Category  (3 fields)                     │    │
│  │  ✅ Order     (7 fields, → Customer)         │    │
│  │  ✅ Customer  (4 fields)                     │    │
│  │  ✅ Review    (4 fields, → Product)          │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│                [Re-generate]  [Accept All]           │
└──────────────────────────────────────────────────────┘
```

**Angular changes**:
- Add `generateWithAI()` method to `EntityDesigner` component
- Create shared `AiGenerateModalComponent` (reusable across pages)
- On "Accept All": call existing `createEntity()` loop

---

### 3.2 Connector Studio — "✨ AI Generate Logic"
**Existing page**: `pages/connector-studio/` (currently empty — needs to be built)

The Connector Studio is a new page, but the AI button is the core feature that justifies it. This is covered in Section 4.1.

---

### 3.3 Business Rule Designer — Inline "✨ AI Suggest"
**Where**: Within any existing designer that has a rules/validation panel.

**Change**: Beside any "Add Rule" button, add "✨ AI Suggest Rule" that opens a single-input modal:

```
┌─────────────────────────────────────────────────────┐
│  ✨ Suggest a Business Rule                     [✕] │
├─────────────────────────────────────────────────────┤
│  Describe the rule in plain English:                │
│  [If order total > ₹5000, flag as high-value order] │
│                              [Cancel]  [Generate]   │
└─────────────────────────────────────────────────────┘
```

---

### 3.4 Dashboard — AI Status Widget
**Existing page**: `components/dashboard/dashboard`

Add a small status card that shows if an AI provider is configured:

```
┌──────────────────────────────────┐
│  🤖 AI Engine                   │
│  ✅ NVIDIA-Dev (connected)       │
│  Model: minimax-m2.7             │
│  [Configure →]                   │
└──────────────────────────────────┘
```

If no provider is configured:
```
┌──────────────────────────────────┐
│  🤖 AI Engine                   │
│  ⚠️  Not configured              │
│  Add your API key to enable AI  │
│  features.                       │
│  [Set Up Now →]                  │
└──────────────────────────────────┘
```

---

## 4. Type B — New Settings Page: AI Providers (High Priority, Medium Effort)

**New route**: `/settings/ai-providers`  
**New files**:
- `pages/settings/ai-providers/ai-providers.ts`
- `pages/settings/ai-providers/ai-providers.html`
- `pages/settings/ai-providers/ai-providers.css`

This is the BYOK management page. Tenant admin manages their API keys here.

```
Settings → AI Providers

┌─────────────────────────────────────────────────────────────────────┐
│  🔑 AI Provider Keys                                    [+ Add New] │
├─────────────────────────────────────────────────────────────────────┤
│  Each skill can use a different provider. Add your API keys below.  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  ⭐ DEFAULT   NVIDIA Fast                                   │   │
│  │             https://integrate.api.nvidia.com/v1             │   │
│  │             Key: nvapi-...m2K9  •  Model: minimax-m2.7      │   │
│  │             ✅ Tested OK — 2026-04-29 19:45                 │   │
│  │                        [Test] [Edit] [Delete]               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │             My OpenAI Account                               │   │
│  │             https://api.openai.com/v1                       │   │
│  │             Key: sk-...xK9f  •  Model: gpt-4o               │   │
│  │             🕐 Untested                                     │   │
│  │             [Set Default] [Test] [Edit] [Delete]            │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Add New Provider Dialog:**
```
┌──────────────────────────────────────────────────────────────┐
│  Add AI Provider                                        [✕]  │
├──────────────────────────────────────────────────────────────┤
│  Provider Label (your name for it):                          │
│  [My Groq Account                  ]                         │
│                                                              │
│  Quick Setup:                                                │
│  [OpenAI] [NVIDIA] [Groq] [Mistral] [Azure OpenAI] [Custom] │
│                                                              │
│  Base URL:                                                   │
│  [https://api.groq.com/openai/v1   ]                         │
│                                                              │
│  API Key:                                                    │
│  [gsk_•••••••••••••••••••••        ]  [👁 Show]              │
│                                                              │
│  Default Model:                                              │
│  [llama-3.3-70b-versatile          ]                         │
│                                                              │
│  Max Tokens: [8192 ]   Timeout (s): [120]                    │
│                                                              │
│  ☑ Set as default provider                                   │
│                                                              │
│  [Test Connection]  →  ✅ Connected (response: "OK")          │
│                                                              │
│                          [Cancel]  [Save Provider]           │
└──────────────────────────────────────────────────────────────┘
```

**Quick Setup templates** (pre-fill BaseUrl + model hint):
```typescript
const PROVIDER_PRESETS = {
  'OpenAI':     { baseUrl: 'https://api.openai.com/v1',               model: 'gpt-4o' },
  'NVIDIA':     { baseUrl: 'https://integrate.api.nvidia.com/v1',     model: 'minimaxai/minimax-m2.7' },
  'Groq':       { baseUrl: 'https://api.groq.com/openai/v1',          model: 'llama-3.3-70b-versatile' },
  'Mistral':    { baseUrl: 'https://api.mistral.ai/v1',               model: 'mistral-large-latest' },
  'Azure':      { baseUrl: 'https://<resource>.openai.azure.com/...',  model: 'gpt-4o' },
  'Custom':     { baseUrl: '',                                          model: '' }
};
```

---

## 5. Type C — New Pages (Medium Priority, High Effort)

---

### 5.1 Connector Studio (New Page)
**New route**: `/projects/:projectId/connectors`  
**New files**: `pages/connector-studio/connector-studio.ts/html/css`

This is the primary surface for AI-powered business logic generation.

```
┌──────────────────────────────────────────────────────────────────────┐
│  Connector Studio  [project: CRM App]                                │
│  ─────────────────────────────────────────────────────────────────  │
│  Connectors are reusable logic units called from workflows and APIs. │
├───────────────────────┬──────────────────────────────────────────────┤
│  CONNECTORS           │  Editor                                      │
│  ─────────────────    │  ─────────────────────────────────────────   │
│  CalculateGst    ✅   │  Name: [ApplyLoyaltyDiscount          ]     │
│  SendEmail       ✅   │  Description: [Applies loyalty discount...]  │
│  ValidateStock   ✅   │                                              │
│  ─────────────────    │  Inputs:                           [+ Add]   │
│  [+ New Connector]    │  ┌──────────────┬──────────┐                │
│                       │  │ LoyaltyPoints │ int      │ [✕]           │
│  [✨ AI Generate]     │  │ TotalAmount   │ decimal  │ [✕]           │
│                       │  └──────────────┴──────────┘                │
│                       │                                              │
│                       │  Business Logic:        [✨ AI Generate]     │
│                       │  ┌──────────────────────────────────────┐   │
│                       │  │ var discount = 0m;                   │   │
│                       │  │ if (LoyaltyPoints > 500)             │   │
│                       │  │     discount = TotalAmount * 0.10m;  │   │
│                       │  │ return discount;                     │   │
│                       │  └──────────────────────────────────────┘   │
│                       │                                              │
│                       │  [✨ Explain This Code]  [💾 Save]  [🗑️]    │
└───────────────────────┴──────────────────────────────────────────────┘
```

**"✨ AI Generate" logic modal:**
```
┌────────────────────────────────────────────────────────────┐
│  ✨ Generate Connector Logic with AI                  [✕]  │
├────────────────────────────────────────────────────────────┤
│  Describe what this connector should do:                   │
│  ┌──────────────────────────────────────────────────┐      │
│  │ Apply a 10% discount if loyalty points > 500.    │      │
│  │ Apply additional 5% if order total > ₹10,000.    │      │
│  └──────────────────────────────────────────────────┘      │
│                                                            │
│  ℹ️  AI is aware of your project entities:                 │
│     Order (TotalAmount, CustomerId...) + Customer (...)    │
│                                                            │
│                         [Cancel]  [✨ Generate]            │
├────────────────────────────────────────────────────────────┤
│  Generated:                                                │
│  ┌──────────────────────────────────────────────────┐      │
│  │  var discount = 0m;                              │      │
│  │  if (LoyaltyPoints > 500)                        │      │
│  │      discount += TotalAmount * 0.10m;            │      │
│  │  if (TotalAmount > 10000)                        │      │
│  │      discount += TotalAmount * 0.05m;            │      │
│  │  return TotalAmount - discount;                  │      │
│  └──────────────────────────────────────────────────┘      │
│                                                            │
│           [🔄 Regenerate]  [✅ Use This Logic]              │
└────────────────────────────────────────────────────────────┘
```

---

### 5.2 AI Playground (New Panel/Page)
**New route**: `/projects/:projectId/ai-playground`  
**Purpose**: Free-form prompt testing with streaming output. Used by developers to test skills and prompts.

```
┌──────────────────────────────────────────────────────────────────┐
│  🧪 AI Playground                                                │
├────────────────────────────┬─────────────────────────────────────┤
│  Skill:                    │  OUTPUT (streaming)                 │
│  [GenerateConnectorLogic ▼]│  ┌───────────────────────────────┐ │
│                            │  │ {                             │ │
│  Provider Override:        │  │   "name": "CalculateDiscount",│ │
│  [-- Use Default -- ▼]     │  │   "inputs": [                 │ │
│                            │  │     {"name": "Amount",        │ │
│  Temperature: [0.3 ──●──]  │  │      "type": "decimal"}       │ │
│  Max Tokens:  [8192  ]     │  │   ],                          │ │
│                            │  │   "businessLogic": "..."      │ │
│  Your Prompt:              │  │ }                             │ │
│  ┌──────────────────────┐  │  └───────────────────────────────┘ │
│  │ Calculate 18% GST on │  │                                     │
│  │ a net amount         │  │  ℹ️  Context injected:              │
│  └──────────────────────┘  │  3 entities, 2 connectors           │
│                            │                                     │
│  [▶ Run]  [⏹ Stop]         │  [📋 Copy]  [✅ Save as Connector]  │
└────────────────────────────┴─────────────────────────────────────┘
```

---

### 5.3 Skill Library (New Settings Page)
**New route**: `/settings/ai-skills`  
**Purpose**: View and customize AI skill definitions. Tenants can override built-in skills.

```
Settings → AI Skills

┌──────────────────────────────────────────────────────────────────────┐
│  🧠 AI Skill Library                                                 │
│  Skills define how the AI generates code, schemas, and rules.        │
├──────────────────────────────────────────────────────────────────────┤
│  ▼ Code Generation                                                   │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  GenerateConnectorLogic     [Built-in]      [✏️ Customize]  │    │
│  │  Converts NL descriptions into C# connector logic           │    │
│  │  Temp: 0.3  •  Model: (default)                             │    │
│  └─────────────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  GenerateEntitySchema       [Built-in]      [✏️ Customize]  │    │
│  │  Converts NL descriptions into entity schemas               │    │
│  └─────────────────────────────────────────────────────────────┘    │
│  ▼ Documentation                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  ExplainLogic               [Built-in]      [✏️ Customize]  │    │
│  │  Explains C# connector logic in plain English               │    │
│  └─────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 6. Navigation Changes

**Dashboard sidebar** needs new items:

```
── Projects ──
  • My Projects

── Design ──
  • Entity Designer        (existing)
  • Enum Designer          (existing)
  • Connector Studio       (NEW ← /connectors)
  • Page Designer          (existing)
  • Form Designer          (existing)
  • Workflow Designer      (existing)
  • Widget Designer        (existing)
  • Security               (existing)

── AI Tools ──             (NEW section)
  • AI Playground          (NEW ← /ai-playground)

── Settings ──             (NEW section)
  • 🔑 AI Providers        (NEW ← /settings/ai-providers)
  • 🧠 AI Skills           (NEW ← /settings/ai-skills)
```

---

## 7. Shared Components to Build

These components are used across multiple pages:

| Component | Used By | Description |
|---|---|---|
| `AiGenerateModalComponent` | Entity Designer, Entity Designer | Text input → loading → preview → accept |
| `AiProviderStatusBadge` | Dashboard, Settings | Shows "✅ Connected" or "⚠️ Not configured" |
| `AiStreamingOutputComponent` | Playground, Connector Studio | Renders streaming SSE tokens in real-time |
| `CodeEditorComponent` | Connector Studio | Monaco-based editor for `businessLogic` snippet |
| `AiContextInfoBanner` | Any AI modal | "AI is aware of X entities, Y connectors" |

---

## 8. Implementation Priority Order

| Sprint | Features | New Files |
|---|---|---|
| **Sprint 1** (Now) | API service methods + AI Providers settings page | `pages/settings/ai-providers/...` |
| **Sprint 2** | AI Generate button on Entity Designer + shared modal | `components/ai-generate-modal/...` |
| **Sprint 3** | Connector Studio page (full CRUD + AI generate) | `pages/connector-studio/...` |
| **Sprint 4** | AI Playground streaming page | `pages/ai-playground/...` |
| **Sprint 5** | Dashboard AI status widget + sidebar navigation updates | Modify existing |
| **Sprint 6** | Skill Library settings page | `pages/settings/ai-skills/...` |

---

## 9. New Routes to Add to `app.routes.ts`

```typescript
// In the root Dashboard children array:
{ 
  path: 'projects/:projectId/connectors', 
  loadComponent: () => import('./pages/connector-studio/connector-studio')
    .then(m => m.ConnectorStudioComponent) 
},
{ 
  path: 'projects/:projectId/ai-playground', 
  loadComponent: () => import('./pages/ai-playground/ai-playground')
    .then(m => m.AiPlaygroundComponent) 
},
{ 
  path: 'settings/ai-providers', 
  loadComponent: () => import('./pages/settings/ai-providers/ai-providers')
    .then(m => m.AiProvidersComponent) 
},
{ 
  path: 'settings/ai-skills', 
  loadComponent: () => import('./pages/settings/ai-skills/ai-skills')
    .then(m => m.AiSkillsComponent) 
}
```
