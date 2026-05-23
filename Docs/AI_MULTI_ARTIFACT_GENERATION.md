# Multi-Artifact AI Generation — Architecture & Integration Plan

> **The Core Problem**: A single business requirement like *"Build an order processing feature with discount logic, stock validation, and email confirmation"* requires 6-8 interdependent artifacts. The AI must decompose, sequence, generate, and integrate them in one cohesive session.

---

## 1. The Problem in Detail

### What a "Feature Request" Actually Needs

When a user says: *"Implement loyalty-based discounts on orders with email notification"*, the platform needs:

```
Business Requirement
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│  Artifacts Required                                             │
│                                                                 │
│  1. Entity: Order          ← may already exist (context)        │
│     └─ Add field: FinalAmount (decimal)                         │
│                                                                 │
│  2. Connector: CalculateDiscount                                │
│     └─ Uses Order.TotalAmount + Customer.LoyaltyPoints          │
│     └─ Returns: FinalAmount, DiscountApplied                    │
│                                                                 │
│  3. Connector: SendOrderConfirmation                            │
│     └─ Uses Customer.Email, Order.OrderNumber, FinalAmount      │
│     └─ Calls SendEmailConnector internally                      │
│                                                                 │
│  4. BusinessRule: SetFinalAmountOnSave                          │
│     └─ Trigger: Order.BeforeSave                                │
│     └─ Condition: FinalAmount == 0                              │
│     └─ Action: Run CalculateDiscount connector                  │
│                                                                 │
│  5. Workflow: OrderConfirmationWorkflow                          │
│     └─ Trigger: Order.OnCreate                                  │
│     └─ Step 1: Execute CalculateDiscount                        │
│     └─ Step 2: Update Order.FinalAmount                         │
│     └─ Step 3: Execute SendOrderConfirmation                    │
│                                                                 │
│  6. AiSession: (history of this generation session)             │
└─────────────────────────────────────────────────────────────────┘
```

A single AI call produces ONE artifact. A business feature produces MANY.

---

## 2. The Solution: AI Generation Plan

Introduce a two-phase approach:

```
Phase 1 — PLAN:   AI decomposes requirement → GenerationPlan (list of artifact specs)
Phase 2 — EXECUTE: Orchestrator executes each spec in dependency order, each feeding context to the next
```

---

## 3. New Concepts & Models

### 3.1 AiGenerationPlan (Stored as ArtifactType.AiSession)

```csharp
namespace Platform.Engine.Models;

/// <summary>
/// Represents an AI-generated plan to satisfy a business requirement.
/// Contains an ordered list of artifacts to create/modify, with dependencies.
/// Stored as ArtifactType.AiSession so it persists and can be resumed.
/// </summary>
public class AiGenerationPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string OriginalRequirement { get; set; } = string.Empty;
    public string PlanSummary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ordered list of artifact generation steps.
    /// Order matters: Entities must be created before Connectors that reference them.
    /// </summary>
    public List<AiGenerationStep> Steps { get; set; } = new();

    /// <summary>Overall status of the plan execution.</summary>
    public string Status { get; set; } = "Draft";
    // Draft | InProgress | PartiallyAccepted | Completed | Abandoned
}

/// <summary>One artifact to be generated or modified within a plan.</summary>
public class AiGenerationStep
{
    public int Order { get; set; }               // Execution sequence (1, 2, 3...)
    public string StepId { get; set; } = Guid.NewGuid().ToString();
    public ArtifactType ArtifactType { get; set; }
    public string ArtifactName { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;  // Which AI skill generates this

    /// <summary>
    /// Dependencies: StepIds that must complete before this step can execute.
    /// Ensures entities exist before connectors that reference them.
    /// </summary>
    public List<string> DependsOn { get; set; } = new();

    /// <summary>
    /// The specific prompt for this step (pre-computed by the planner).
    /// More precise than the original requirement.
    /// </summary>
    public string StepPrompt { get; set; } = string.Empty;

    /// <summary>The AI-generated artifact content (JSON).</summary>
    public string? GeneratedContent { get; set; }

    /// <summary>ID of the saved Artifact if accepted.</summary>
    public Guid? AcceptedArtifactId { get; set; }

    public string Status { get; set; } = "Pending";
    // Pending | Generating | Generated | Accepted | Rejected | Modified
}
```

### 3.2 New Skill: "DecomposeRequirement"

A new built-in skill whose ONLY job is to analyze a business requirement and produce a `AiGenerationPlan` JSON:

```json
// User asks: "Add loyalty discounts on orders with email confirmation"
// AI responds with:
{
  "planSummary": "Creates discount calculation logic, updates Order entity, adds workflow trigger and email notification.",
  "steps": [
    {
      "order": 1,
      "artifactType": "Entity",
      "artifactName": "Order",
      "skillName": "ModifyEntitySchema",
      "dependsOn": [],
      "stepPrompt": "Add a 'FinalAmount' decimal field to the existing Order entity. This stores the discounted total."
    },
    {
      "order": 2,
      "artifactType": "Connector",
      "artifactName": "CalculateDiscount",
      "skillName": "GenerateConnectorLogic",
      "dependsOn": ["step-1"],
      "stepPrompt": "Generate a connector that takes Order.TotalAmount (decimal) and Customer.LoyaltyPoints (int) as inputs. Apply 10% discount if LoyaltyPoints > 500. Apply additional 5% if TotalAmount > 10000. Return FinalAmount and DiscountApplied."
    },
    {
      "order": 3,
      "artifactType": "Connector",
      "artifactName": "SendOrderConfirmation",
      "skillName": "GenerateConnectorLogic",
      "dependsOn": [],
      "stepPrompt": "Generate a connector that takes CustomerEmail (string), OrderNumber (string), FinalAmount (decimal). Calls SendEmailConnector with a formatted confirmation message."
    },
    {
      "order": 4,
      "artifactType": "Workflow",
      "artifactName": "OrderConfirmationWorkflow",
      "skillName": "GenerateWorkflow",
      "dependsOn": ["step-2", "step-3"],
      "stepPrompt": "Generate an Elsa workflow triggered by Order.OnCreate. Step 1: Execute CalculateDiscount with Order.TotalAmount and Customer.LoyaltyPoints. Step 2: Update Order.FinalAmount with the result. Step 3: Execute SendOrderConfirmation."
    }
  ]
}
```

---

## 4. AiPlanOrchestrator — The Execution Engine

```csharp
namespace Platform.Engine.Services.Ai;

public class AiPlanOrchestrator
{
    private readonly AiSkillLibrary _skillLibrary;
    private readonly SchemaContextExtractor _contextExtractor;
    private readonly TenantAiProviderResolver _resolver;
    private readonly IArtifactRepository _artifactRepo;
    private readonly ILogger<AiPlanOrchestrator> _logger;

    /// <summary>
    /// Phase 1: Analyze the requirement and produce a GenerationPlan.
    /// Does NOT execute yet — returns the plan for user review.
    /// </summary>
    public async Task<AiGenerationPlan> CreatePlanAsync(
        Guid projectId, Guid tenantId, string requirement, CancellationToken ct)
    {
        var skill = await _skillLibrary.GetSkillAsync("DecomposeRequirement", projectId, ct);
        var schemaContext = await _contextExtractor.ExtractAsync(projectId, ct);

        var (provider, model) = await _resolver.ResolveAsync(tenantId, ct: ct);

        var rawPlan = await provider.CompleteAsync(new AiRequest
        {
            SystemPrompt = skill.SystemPrompt + schemaContext,
            UserPrompt   = requirement,
            Temperature  = 0.3,
            Model        = model
        }, ct);

        var plan = JsonSerializer.Deserialize<AiGenerationPlan>(CleanJson(rawPlan))!;
        plan.ProjectId = projectId;
        plan.OriginalRequirement = requirement;
        return plan;
    }

    /// <summary>
    /// Phase 2: Execute one step of the plan.
    /// Each step uses its specific skill and is aware of steps already completed.
    /// </summary>
    public async Task<AiGenerationStep> ExecuteStepAsync(
        AiGenerationPlan plan,
        AiGenerationStep step,
        Guid tenantId,
        CancellationToken ct)
    {
        step.Status = "Generating";

        // Build accumulated context: project schema + already-accepted steps
        var schemaContext = await _contextExtractor.ExtractAsync(plan.ProjectId, ct);
        var completedContext = BuildCompletedStepsContext(plan, step);

        var skill = await _skillLibrary.GetSkillAsync(step.SkillName, plan.ProjectId, ct);
        var (provider, model) = await _resolver.ResolveAsync(tenantId,
            skill.PreferredProvider, skill.PreferredModel, ct);

        var result = await provider.CompleteAsync(new AiRequest
        {
            SystemPrompt = skill.SystemPrompt + schemaContext + completedContext,
            UserPrompt   = step.StepPrompt,
            History      = BuildFewShot(skill),
            Temperature  = skill.DefaultTemperature,
            Model        = model
        }, ct);

        step.GeneratedContent = CleanJson(result);
        step.Status = "Generated";
        return step;
    }

    /// <summary>
    /// Accept a step: save the generated content as a real Artifact in the DB.
    /// The artifact immediately becomes available as context for subsequent steps.
    /// </summary>
    public async Task<Guid> AcceptStepAsync(
        AiGenerationPlan plan,
        AiGenerationStep step,
        CancellationToken ct)
    {
        var artifact = new Artifact
        {
            Id        = Guid.NewGuid(),
            ProjectId = plan.ProjectId,
            Type      = step.ArtifactType,
            Name      = step.ArtifactName,
            Content   = step.GeneratedContent ?? "{}",
            LastModified = DateTime.UtcNow
        };

        await _artifactRepo.AddAsync(artifact);

        step.AcceptedArtifactId = artifact.Id;
        step.Status = "Accepted";

        _logger.LogInformation(
            "Plan {PlanId} Step {StepId}: Accepted as Artifact {ArtifactId} [{Type}:{Name}]",
            plan.PlanId, step.StepId, artifact.Id, step.ArtifactType, step.ArtifactName);

        return artifact.Id;
    }

    /// <summary>
    /// Builds a context summary of all steps already completed in this plan.
    /// Injected into subsequent step prompts so the AI knows what already exists.
    /// </summary>
    private static string BuildCompletedStepsContext(AiGenerationPlan plan, AiGenerationStep currentStep)
    {
        var accepted = plan.Steps
            .Where(s => s.Status == "Accepted" && s.Order < currentStep.Order)
            .ToList();

        if (!accepted.Any()) return string.Empty;

        var sb = new StringBuilder("\n\n=== ALREADY GENERATED IN THIS SESSION ===\n");
        sb.AppendLine("These artifacts were just created and are available for reference:\n");

        foreach (var s in accepted)
        {
            sb.AppendLine($"✅ {s.ArtifactType}: `{s.ArtifactName}`");
            // Inline the content summary so the AI knows exact field names generated
            if (!string.IsNullOrEmpty(s.GeneratedContent))
            {
                sb.AppendLine($"   Content preview: {s.GeneratedContent[..Math.Min(300, s.GeneratedContent.Length)]}...");
            }
        }

        sb.AppendLine("=== END SESSION ARTIFACTS ===\n");
        return sb.ToString();
    }
}
```

---

## 5. How Generated Artifacts Integrate into the API

Once artifacts are **accepted** (saved to the DB), they integrate automatically at **build time** through the existing generation pipeline:

```
Accepted Artifacts in DB
         │
         ▼  (existing BuildController.cs)
BuildController.BuildProject(projectId)
         │
         ▼
MetadataLoader.LoadAll(projectId)
  ├── entities   ← from Artifact[Type=Entity]
  ├── connectors ← from Artifact[Type=Connector]  ← AI-generated ones included
  ├── workflows  ← from Artifact[Type=Workflow]   ← AI-generated ones included
  └── rules      ← from Artifact[Type=Entity/Rule]
         │
         ▼
Code Generators (Scriban templates)
  ├── EntityGenerator   → Entity.cs files
  ├── ConnectorGenerator → Connector.cs files  ← businessLogic injected here
  ├── RepositoryGenerator → Repository.cs files
  ├── ControllerGenerator → Controller.cs files
  └── WorkflowGenerator  → Elsa JSON definitions
         │
         ▼
ProjectGenerator → .csproj + Program.cs (with all DI registrations)
         │
         ▼
dotnet build → ZIP → Deployable ASP.NET Core API
```

**The key insight**: AI artifacts are **identical** to manually designed artifacts in the DB. The build engine cannot tell the difference. There is zero special handling required at build time.

---

## 6. API Endpoints for Multi-Artifact Generation

Three new endpoints to add to `AiController`:

### 6.1 `POST /api/ai/plan` — Create a Generation Plan

```http
POST /api/ai/plan
{
  "projectId": "abc-123",
  "requirement": "Add loyalty discounts on orders with email confirmation"
}

Response 200:
{
  "planId": "plan-xyz",
  "planSummary": "Creates discount calculation logic...",
  "steps": [
    { "order": 1, "artifactType": "Entity",    "artifactName": "Order",         "status": "Pending" },
    { "order": 2, "artifactType": "Connector", "artifactName": "CalculateDiscount", "status": "Pending" },
    { "order": 3, "artifactType": "Connector", "artifactName": "SendOrderConfirmation", "status": "Pending" },
    { "order": 4, "artifactType": "Workflow",  "artifactName": "OrderConfirmationWorkflow", "status": "Pending" }
  ]
}
```

### 6.2 `POST /api/ai/plan/{planId}/steps/{stepId}/execute` — Execute One Step

```http
POST /api/ai/plan/plan-xyz/steps/step-2/execute

Response 200:
{
  "stepId": "step-2",
  "status": "Generated",
  "artifactType": "Connector",
  "artifactName": "CalculateDiscount",
  "generatedContent": {
    "name": "CalculateDiscount",
    "inputs": [...],
    "businessLogic": "var discount = 0m;\n..."
  }
}
```

### 6.3 `POST /api/ai/plan/{planId}/steps/{stepId}/accept` — Accept & Save

```http
POST /api/ai/plan/plan-xyz/steps/step-2/accept
// Optionally include modified content if user edited the preview
{ "modifiedContent": "{ ... }" }

Response 200:
{
  "artifactId": "artifact-guid",
  "message": "Saved as Artifact[Connector:CalculateDiscount]"
}
```

### 6.4 `POST /api/ai/plan/{planId}/steps/{stepId}/reject` — Reject a Step

```http
POST /api/ai/plan/plan-xyz/steps/step-2/reject
{ "reason": "Already have this connector" }

Response 200: { "status": "Rejected" }
```

---

## 7. UI Flow for Multi-Artifact Generation

The **"✨ Build Feature"** button (higher-level than the existing "✨ Generate") opens a wizard:

```
┌──────────────────────────────────────────────────────────────────┐
│  ✨ Build a Feature with AI                               Step 1 │
├──────────────────────────────────────────────────────────────────┤
│  Describe what you want to build:                                │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Add loyalty-based discounts on orders. If a customer has   │  │
│  │ more than 500 points, apply 10% off. If order > ₹10,000,   │  │
│  │ apply additional 5%. Send email confirmation on order.     │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│                              [Cancel]  [Analyze Requirement →]   │
└──────────────────────────────────────────────────────────────────┘

                ↓ AI calls POST /api/ai/plan

┌──────────────────────────────────────────────────────────────────┐
│  ✨ Build a Feature with AI                               Step 2 │
│  Review the generation plan before executing                     │
├──────────────────────────────────────────────────────────────────┤
│  AI will create the following artifacts:                         │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │ 1. 📝 MODIFY  Entity: Order                              │    │
│  │    Add FinalAmount (decimal) field                       │    │
│  │    [✓ Include]  [✗ Skip]                                 │    │
│  ├──────────────────────────────────────────────────────────┤    │
│  │ 2. ⚙️ CREATE  Connector: CalculateDiscount               │    │
│  │    Loyalty + volume discount logic                       │    │
│  │    Depends on: step 1                                    │    │
│  │    [✓ Include]  [✗ Skip]                                 │    │
│  ├──────────────────────────────────────────────────────────┤    │
│  │ 3. ⚙️ CREATE  Connector: SendOrderConfirmation           │    │
│  │    Email notification with order summary                 │    │
│  │    [✓ Include]  [✗ Skip]                                 │    │
│  ├──────────────────────────────────────────────────────────┤    │
│  │ 4. 🔄 CREATE  Workflow: OrderConfirmationWorkflow        │    │
│  │    Orchestrates discount + email on Order.OnCreate       │    │
│  │    Depends on: steps 2, 3                                │    │
│  │    [✓ Include]  [✗ Skip]                                 │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│       [← Back]  [Edit Plan]  [▶ Execute All]  [Execute One →]   │
└──────────────────────────────────────────────────────────────────┘

                ↓ User clicks "Execute All" (or step by step)

┌──────────────────────────────────────────────────────────────────┐
│  ✨ Build a Feature with AI                               Step 3 │
│  Review & Accept Each Artifact                                   │
├──────────────────────────────────────────────────────────────────┤
│  ✅ Step 1: Entity — Order   [Accepted]                          │
│                                                                  │
│  ⚙️ Step 2: Connector — CalculateDiscount     [Generated]        │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │ {                                                        │    │
│  │   "name": "CalculateDiscount",                           │    │
│  │   "inputs": [                                            │    │
│  │     {"name": "TotalAmount", "type": "decimal"},          │    │
│  │     {"name": "LoyaltyPoints", "type": "int"}             │    │
│  │   ],                                                     │    │
│  │   "businessLogic": "var d=0m;\nif(LoyaltyPoints>500)..." │    │
│  │ }                                                        │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  [✏️ Edit]  [🔄 Regenerate]  [✗ Reject]  [✅ Accept & Continue]   │
│                                                                  │
│  ⏸ Step 3: Connector — SendOrderConfirmation  [Waiting...]       │
│  ⏸ Step 4: Workflow — OrderConfirmationWorkflow  [Waiting...]     │
└──────────────────────────────────────────────────────────────────┘
```

---

## 8. Dependency & Sequencing Rules

The `AiPlanOrchestrator` enforces these rules automatically:

| Rule | Reason |
|---|---|
| `Entity` artifacts execute before `Connector` | Connectors reference entity field names; they must exist in schema context |
| `Connector` artifacts execute before `Workflow` | Workflows reference connector names for `ExecuteConnector` steps |
| `Entity` field modifications merge, not replace | Modifying an existing entity adds new fields without deleting existing ones |
| `BusinessRule` executes after the `Entity` it targets | Rule conditions reference entity field names |
| `Form` executes after its target `Entity` | Form fields map to entity fields |

### Dependency Graph (for the example)

```
Step 1: Entity[Order]
    │
    ▼
Step 2: Connector[CalculateDiscount]  ──┐
                                        ├──► Step 4: Workflow[OrderConfirmation]
Step 3: Connector[SendOrderConfirmation] ┘
```

Steps 2 and 3 can execute **in parallel** (no dependency on each other).  
Step 4 must wait for both 2 and 3.

---

## 9. Integration into the Build Pipeline — What Changes

### Nothing changes in the build pipeline.

The accepted artifacts are saved with the exact same `Artifact` schema as manually created ones. The `MetadataLoader` and all Scriban generators remain untouched.

The only addition needed is **handling "modify existing entity"** scenarios where the AI generates a partial entity (e.g., just adding `FinalAmount`) rather than a full replacement:

```csharp
// In AcceptStepAsync — for Entity modifications:
public async Task<Guid> AcceptStepAsync(AiGenerationPlan plan, AiGenerationStep step, ...)
{
    if (step.ArtifactType == ArtifactType.Entity)
    {
        // Check if entity already exists
        var existing = await _artifactRepo.GetByProjectAndNameAsync(plan.ProjectId, step.ArtifactName);

        if (existing != null && step.IsModification)
        {
            // Merge: add new fields, preserve existing ones
            var existingMeta = JsonSerializer.Deserialize<EntityMetadata>(existing.Content);
            var newMeta      = JsonSerializer.Deserialize<EntityMetadata>(step.GeneratedContent!);

            foreach (var newField in newMeta!.Fields)
            {
                if (!existingMeta!.Fields.Any(f => f.Name == newField.Name))
                    existingMeta.Fields.Add(newField);
            }

            existing.Content = JsonSerializer.Serialize(existingMeta);
            existing.LastModified = DateTime.UtcNow;
            await _artifactRepo.UpdateAsync(existing);
            return existing.Id;
        }
    }

    // New artifact — standard add
    var artifact = new Artifact { ... };
    await _artifactRepo.AddAsync(artifact);
    return artifact.Id;
}
```

---

## 10. Summary: Artifact Integration Matrix

| Artifact Type | AI Generates | Saved As | Integrated At | API Surface |
|---|---|---|---|---|
| `Entity` | `EntityMetadata` JSON | `ArtifactType.Entity` | Build → `Entity.cs` | GET/POST `/entities` |
| `Connector` | `ConnectorMetadata` JSON (incl. C# `businessLogic`) | `ArtifactType.Connector` | Build → `Connector.cs` | Auto-registered in `Program.cs` |
| `Workflow` | Elsa workflow JSON | `ArtifactType.Workflow` | Build → Elsa import | Elsa engine |
| `BusinessRule` | `BusinessRuleMetadata` JSON | `ArtifactType.Entity` (rule subtype) | Build → `EvaluateRulesActivity` | Injected into EF save pipeline |
| `Form` | `FormMetadata` JSON | `ArtifactType.Form` | Build → Angular form component | GET `/forms/:id` |
| `AiGenerationPlan` | Plan JSON | `ArtifactType.AiSession` | Not built — reference only | GET `/ai/plans/:id` |
