# AI Prompt Engineering — Skills, Rules & System Prompts

> **Scope**: This document defines the prompt engineering strategy for DynamicPlatform. Since different AI models produce different quality outputs, the platform uses a **Skill Library** stored in the database to standardize behavior across all providers.

---

## 1. The Three-Layer Prompt Architecture

Every AI call in the platform is composed from three layers:

```
┌─────────────────────────────────────────────────────┐
│  LAYER 1: SYSTEM PROMPT (AiSkill.SystemPrompt)      │
│  • Who the AI is (role + expertise)                 │
│  • Hard rules and constraints (numbered)            │
│  • Safety guardrails                                │
│  • Output format contract                           │
├─────────────────────────────────────────────────────┤
│  LAYER 2: CONTEXT INJECTION (SchemaContextExtractor)│
│  • Live entity schema from DB                       │
│  • Available connectors catalog                     │
│  • Existing business rules (avoid conflicts)        │
│  • Platform conventions & patterns                  │
├─────────────────────────────────────────────────────┤
│  LAYER 3: USER PROMPT (Runtime Input)               │
│  • The actual business requirement                  │
│  • Optional: target entity name hint                │
│  • Optional: existing code to refactor              │
└─────────────────────────────────────────────────────┘
```

---

## 2. Skill Library — Catalog of Built-In Skills

### 2.1 Skill: GenerateConnectorLogic

**Purpose**: Translates a business requirement into a `ConnectorMetadata` JSON with a compilable `BusinessLogic` C# string.

**System Prompt**:
```
You are an expert C# developer specializing in the DynamicPlatform low-code framework.
Your task is to generate business logic for a Connector — a stateless execution unit that 
implements the IConnector interface.

=== ROLE ===
You write safe, production-quality, idiomatic C# 12 / .NET 8 code. 
You are familiar with async/await, LINQ, and dependency injection patterns.

=== HARD RULES (follow all, in order) ===
1. Output ONLY a single, valid JSON object. No markdown. No prose. No extra keys.
2. The JSON must conform exactly to ConnectorMetadata schema (see Output Format below).
3. The `businessLogic` string must contain only C# statements — no class/method declarations.
4. Use ONLY input variable names defined in your `inputs` array. Never invent field names.
5. Do NOT instantiate Entity classes. Do NOT use `new Order()`, `new Customer()` etc.
6. Do NOT include any database context (DbContext, EF queries). Connectors are stateless.
7. Always wrap the full logic in proper error handling (it's handled by the outer try/catch template).
8. Always return an explicit typed value. Never use `return null` without justification.
9. Log important decision points using: `logger.LogInformation("...")`
10. If the requirement is unclear, generate the most reasonable conservative implementation.

=== FORBIDDEN PATTERNS ===
- File system access: File.Read, File.Write, Directory.*
- Process execution: Process.Start, cmd.exe, bash
- Reflection: Assembly.Load, Type.GetMethod
- Raw SQL: SqlCommand, ExecuteNonQuery
- Environment variables access: Environment.GetEnvironmentVariable (use injected config instead)

=== OUTPUT FORMAT ===
{
  "name": "PascalCaseConnectorName",
  "namespace": "GeneratedApp.Connectors",
  "description": "One sentence describing what this connector does.",
  "inputs": [ { "name": "FieldName", "type": "csharptype" } ],
  "outputs": [ { "name": "ResultField", "type": "csharptype" } ],
  "configProperties": [],
  "businessLogic": "// C# logic here. Multi-line with \\n escaping."
}
```

**Guide Prompt** (injected as the first "assistant" turn to prime format):
```
Understood. I will output a single valid JSON object conforming to ConnectorMetadata.
The businessLogic will contain only executable C# statements using the declared input variables.
I will not output markdown, explanations, or any text outside the JSON object.
Ready for the business requirement.
```

**Few-Shot Examples**:
```
INPUT: Calculate 18% GST on an amount and return the gross total.
OUTPUT:
{
  "name": "CalculateGst",
  "description": "Calculates 18% GST on a net amount and returns gross total.",
  "inputs": [ { "name": "NetAmount", "type": "decimal" } ],
  "outputs": [ { "name": "GrossAmount", "type": "decimal" }, { "name": "GstAmount", "type": "decimal" } ],
  "businessLogic": "var GstAmount = NetAmount * 0.18m;\nvar GrossAmount = NetAmount + GstAmount;\nlogger.LogInformation($\"GST calc: Net={NetAmount}, GST={GstAmount}, Gross={GrossAmount}\");\nreturn new Dictionary<string, object?> { [\"GrossAmount\"] = GrossAmount, [\"GstAmount\"] = GstAmount };"
}
```

---

### 2.2 Skill: GenerateEntitySchema

**Purpose**: Converts a natural-language domain description into an `EntityMetadata[]` JSON array.

**System Prompt**:
```
You are a Software Architect designing a data model for a business application.
Convert the user's domain description into a JSON array of EntityMetadata objects.

=== HARD RULES ===
1. Output ONLY a valid JSON array. No markdown. No explanations.
2. Do NOT include `Id`, `CreatedAt`, `UpdatedAt` fields — they are auto-generated.
3. Field types must be one of: string, int, decimal, bool, datetime, guid.
4. For references to other entities, use type=guid and name it EntityNameId (e.g., CustomerId).
5. For enum-like fields (Status, Category), use type=string with a comment in the name.
6. Relations must use RelationType: OneToMany, ManyToOne, or ManyToMany.
7. NavPropName must be PascalCase and match the target entity name (singular or plural).
8. Infer reasonable IsRequired and MaxLength values from business context.
9. Namespace must always be "GeneratedApp.Entities".

=== OUTPUT FORMAT ===
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "name": "EntityName",
    "namespace": "GeneratedApp.Entities",
    "fields": [
      { "name": "FieldName", "type": "string", "isRequired": true, "maxLength": 100, "rules": [] }
    ],
    "relations": [
      { "targetEntity": "OtherEntity", "type": "ManyToOne", "navPropName": "OtherEntity", "foreignKeyName": "OtherEntityId" }
    ]
  }
]
```

**Recommended Model**: `gpt-4o` or `claude-3-5-sonnet` (complex structural reasoning)
**Temperature**: 0.2 (deterministic schema output)

---

### 2.3 Skill: GenerateBusinessRule

**Purpose**: Creates a `BusinessRuleMetadata` object from a condition statement.

**System Prompt**:
```
You are a business rules analyst for DynamicPlatform. Convert user requirements 
into BusinessRuleMetadata JSON objects.

=== HARD RULES ===
1. Output ONLY a valid JSON object conforming to BusinessRuleMetadata schema.
2. Trigger must be one of: BeforeSave, AfterSave, OnDelete.
3. Condition must be a simple boolean expression using field names from the target entity.
4. Action must describe a simple mutation (Set FieldName = Value).
5. Do NOT generate C# code — only declarative rule expressions.

=== OUTPUT FORMAT ===
{
  "name": "RuleName",
  "description": "...",
  "targetEntity": "EntityName",
  "trigger": "BeforeSave",
  "condition": "FieldName > Value",
  "action": "Set OtherField = NewValue"
}
```

---

### 2.4 Skill: GenerateFormLayout

**Purpose**: Creates a `FormMetadata` JSON from an entity schema.

**System Prompt**:
```
You are a UX designer for a low-code platform. Given an entity schema, generate a 
logical form layout as a FormMetadata JSON object. Group related fields into sections.
Order fields by user-facing priority (name before ID, status at the bottom).
```

---

### 2.5 Skill: ExplainExistingLogic (AI Code Reader)

**Purpose**: Reverse-engineers an existing `BusinessLogic` string and explains it in plain English.

**System Prompt**:
```
You are a senior code reviewer. Given a C# code snippet from a DynamicPlatform connector,
explain what it does in plain English. Structure your response as:
1. What it does (1 sentence)
2. Inputs it uses
3. Logic steps (numbered)
4. Output it returns
5. Potential issues or improvements
```

---

### 2.6 Skill: ImproveExistingLogic (AI Refactoring)

**Purpose**: Takes existing `BusinessLogic` + a complaint/requirement and returns improved code.

**System Prompt**:
```
You are an expert C# developer. You will receive:
- EXISTING CODE: The current businessLogic string of a connector.
- IMPROVEMENT REQUEST: What needs to change.

Your task: Return ONLY the improved businessLogic string (not the full ConnectorMetadata).
Follow all platform rules: no entity instantiation, no file access, proper logging.
Do not explain. Output only the C# code string.
```

---

## 3. Provider-Specific Adjustments

Different models need different handling. The `AiSkill` can store `PreferredProvider` and `PreferredModel` to route specific skills to the best model:

| Skill | Best Model | Notes |
|---|---|---|
| `GenerateConnectorLogic` | `gpt-4o` / `deepseek-coder` | Code-specialized models excel here |
| `GenerateEntitySchema` | `gpt-4o` / `claude-3-5-sonnet` | Complex structural reasoning |
| `GenerateBusinessRule` | `llama-3.3-70b` / `minimax-m2.7` | Simple, fast, declarative |
| `GenerateFormLayout` | Any model | Simple JSON generation |
| `ExplainExistingLogic` | `claude-3-5-sonnet` | Best at verbose explanation |
| `ImproveExistingLogic` | `gpt-4o` / `deepseek-coder` | Refactoring requires code understanding |

### 3.1 Model-Specific Output Cleaning Rules

```csharp
public class AiResponseCleaner
{
    public string Clean(string raw, string providerName)
    {
        // All providers: strip markdown code fences
        raw = Regex.Replace(raw, @"```json|```csharp|```", "").Trim();

        // Some models (Mistral, Llama) prepend "Sure, here is..." 
        if (raw.StartsWith("{") == false && raw.Contains("{"))
            raw = raw[raw.IndexOf('{')..];

        // Some models append "Hope this helps!" after the JSON
        var lastBrace = raw.LastIndexOf('}');
        if (lastBrace >= 0 && lastBrace < raw.Length - 1)
            raw = raw[..(lastBrace + 1)];

        // NVIDIA Minimax: sometimes double-escapes newlines
        if (providerName == "NVIDIA")
            raw = raw.Replace("\\\\n", "\\n");

        return raw;
    }
}
```

---

## 4. Internal Tooling Requirements

To manage the Skill Library and AI workflow, the following internal tools must be built into Platform Studio:

### 4.1 Skill Editor (Studio UI Panel)

A dedicated panel in Platform Studio for managing `AiSkill` artifacts:

```
┌────────────────────────────────────────────────────┐
│  AI Skill Library                         [+ New]  │
├────────────────────────────────────────────────────┤
│  ▼ Code Generation                                 │
│    • GenerateConnectorLogic      [Edit] [Test]     │
│    • GenerateEntitySchema        [Edit] [Test]     │
│    • ImproveExistingLogic        [Edit] [Test]     │
│  ▼ Documentation                                   │
│    • ExplainExistingLogic        [Edit] [Test]     │
├────────────────────────────────────────────────────┤
│  [Edit Skill: GenerateConnectorLogic]              │
│  Name: ____________  Category: [Code Generation ▼] │
│  Preferred Provider: [NVIDIA ▼]                    │
│  Preferred Model:   [minimaxai/minimax-m2.7]       │
│  Temperature: [0.3]                                │
│                                                    │
│  System Prompt:                                    │
│  ┌──────────────────────────────────────────────┐ │
│  │ You are an expert C# developer...            │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  Rules (one per line):                             │
│  ┌──────────────────────────────────────────────┐ │
│  │ 1. Output ONLY valid JSON...                 │ │
│  │ 2. No entity instantiation...                │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  Few-Shot Examples:  [+ Add Example]               │
│                                                    │
│               [Save Skill]  [Test Skill]           │
└────────────────────────────────────────────────────┘
```

### 4.2 AI Playground (Studio Panel)

A scratchpad for testing any skill against the live project schema:

```
┌────────────────────────────────────────────────────┐
│  AI Playground                                     │
├────────────────────────────────────────────────────┤
│  Skill: [GenerateConnectorLogic ▼]                 │
│                                                    │
│  Your Requirement:                                 │
│  ┌──────────────────────────────────────────────┐ │
│  │ When an order is placed, apply loyalty       │ │
│  │ discount and update FinalAmount...           │ │
│  └──────────────────────────────────────────────┘ │
│                                    [Generate ▶]    │
│                                                    │
│  Generated Output:         [Accept] [Retry] [Edit] │
│  ┌──────────────────────────────────────────────┐ │
│  │ {                                            │ │
│  │   "name": "ApplyOrderDiscount",              │ │
│  │   "businessLogic": "var discount = 0m;\n..." │ │
│  │ }                                            │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  Context Used (collapsed):                         │
│  ▶ Schema injected: 3 entities, 2 connectors      │
└────────────────────────────────────────────────────┘
```

### 4.3 AI Session History (per Project)

All AI interactions are stored as `AiSession` artifacts. Users can:
- Review past generations
- Re-accept a previously rejected output
- Compare outputs from different model runs
- Export a session as documentation

### 4.4 Inline AI Button in Connector Editor

In the existing Connector editor page, add an **"✨ AI Generate"** button that:
1. Opens a modal with a text area for the requirement.
2. Calls `POST /api/ai/generate-connector` with the requirement + `projectId`.
3. Previews the result.
4. On "Accept", saves the `ConnectorMetadata` to DB.

### 4.5 API Endpoints Required

```
POST /api/ai/generate-connector      → AiOrchestrator.GenerateAsync("GenerateConnectorLogic", ...)
POST /api/ai/generate-schema         → AiOrchestrator.GenerateAsync("GenerateEntitySchema", ...)  [ALREADY EXISTS]
POST /api/ai/generate-rule           → AiOrchestrator.GenerateAsync("GenerateBusinessRule", ...)
POST /api/ai/explain-logic           → AiOrchestrator.GenerateAsync("ExplainExistingLogic", ...)
POST /api/ai/improve-logic           → AiOrchestrator.GenerateAsync("ImproveExistingLogic", ...)
GET  /api/ai/skills                  → List all AiSkill artifacts
PUT  /api/ai/skills/{id}             → Update an AiSkill artifact
POST /api/ai/skills/{id}/test        → Test a skill with a given prompt
GET  /api/ai/sessions/{projectId}    → List AI session history for a project
```

---

## 5. Quality Assurance Strategy

Since AI outputs vary, apply these checks before accepting any output:

| Check | Type | Action on Fail |
|---|---|---|
| Valid JSON parse | Hard | Reject + retry |
| Required fields present | Hard | Reject + retry |
| Forbidden pattern scan | Hard | Reject + alert user |
| C# syntax check (Roslyn) | Soft | Warn user, allow override |
| Field name validation vs. schema | Soft | Warn user with diff |
| Output length sanity (< 50 chars) | Soft | Warn user |
| Business rule conflict check | Soft | Warn user |

```csharp
// Optional: Use Roslyn for basic C# syntax validation
using Microsoft.CodeAnalysis.CSharp;

public bool IsValidCsharp(string snippet)
{
    var wrappedCode = $"class T {{ async Task M() {{ {snippet} }} }}";
    var tree = CSharpSyntaxTree.ParseText(wrappedCode);
    return !tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
}
```
