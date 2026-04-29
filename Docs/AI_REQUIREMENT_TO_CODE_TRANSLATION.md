# Business Requirement to Code Translation — How It Works

> **Scope**: A detailed walkthrough of the exact pipeline that converts a natural-language business requirement into valid, compiled C# code within the DynamicPlatform, using the existing metadata model and Scriban template system.

---

## 1. The Translation Pipeline (Step by Step)

```
[User Types Business Requirement]
            │
            ▼
  Step 1: SchemaContextExtractor
  Reads ALL Artifacts for the project from DB
  → Produces: Structured "Platform Knowledge Document"
            │
            ▼
  Step 2: PromptSkillLibrary
  Loads the matching AiSkill (rules + system prompt + guide prompt)
  → Produces: Fully assembled System Message for the LLM
            │
            ▼
  Step 3: IAiProvider
  Sends (System + Context + User Requirement) to LLM
  → Produces: Raw LLM Output (JSON or code snippet)
            │
            ▼
  Step 4: IAiResponseParser
  Validates, cleans, and normalizes LLM output
  → Produces: Typed object (ConnectorMetadata, EntityMetadata, etc.)
            │
            ▼
  Step 5: Artifact Persistence
  Saves result as a new/updated Artifact.Content in the DB
            │
            ▼
  Step 6: ProjectGenerator (Existing Engine)
  At build time, uses Scriban templates (Connector.scriban etc.)
  to render final .cs files
            │
            ▼
  Step 7: dotnet build
  Compiles the generated project → Deployable API
```

---

## 2. Step 1 — SchemaContextExtractor: Making Entities Visible to AI

The AI cannot "see" your database. The `SchemaContextExtractor` reads all `Artifact` records for a project and produces a structured markdown block injected into the system prompt.

### 2.1 What it reads

```csharp
public class SchemaContextExtractor : ISchemaContextExtractor
{
    private readonly IArtifactRepository _repo;

    public async Task<string> ExtractAsync(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var sb = new StringBuilder();

        // --- ENTITIES ---
        sb.AppendLine("## Available Entities");
        foreach (var artifact in artifacts.Where(a => a.Type == ArtifactType.Entity))
        {
            var entity = JsonSerializer.Deserialize<EntityMetadata>(artifact.Content)!;
            sb.AppendLine($"\n### Entity: {entity.Name}");
            sb.AppendLine("| Field | CSharp Type | Required | Max Length |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var field in entity.Fields)
            {
                sb.AppendLine($"| {field.Name} | {field.CsharpType} | {field.IsRequired} | {field.MaxLength} |");
            }
            // Relations
            if (entity.Relations.Any())
            {
                sb.AppendLine("\n**Relations:**");
                foreach (var rel in entity.Relations)
                    sb.AppendLine($"- {rel.Type}: {entity.Name} → {rel.TargetEntity} via `{rel.NavPropName}`");
            }
        }

        // --- EXISTING CONNECTORS ---
        sb.AppendLine("\n## Available Connectors (IConnector implementations)");
        foreach (var artifact in artifacts.Where(a => a.Type == ArtifactType.Connector))
        {
            var conn = JsonSerializer.Deserialize<ConnectorMetadata>(artifact.Content)!;
            sb.AppendLine($"- **{conn.Name}**: {conn.Description}");
            sb.AppendLine($"  Inputs: {string.Join(", ", conn.Inputs.Select(i => $"{i.Name}:{i.Type}"))}");
        }

        // --- BUSINESS RULES ---
        sb.AppendLine("\n## Existing Business Rules (Do NOT duplicate)");
        // ... similar pattern for BusinessRuleMetadata artifacts

        return sb.ToString();
    }
}
```

### 2.2 Example output (injected into system prompt)

```
## Available Entities

### Entity: Order
| Field         | CSharp Type | Required | Max Length |
|---------------|-------------|----------|------------|
| OrderNumber   | string      | True     | 50         |
| TotalAmount   | decimal     | True     | 0          |
| Status        | OrderStatus | False    | 0          |
| CustomerId    | Guid        | True     | 0          |

**Relations:**
- ManyToOne: Order → Customer via `Customer`
- OneToMany: Order → OrderItem via `OrderItems`

### Entity: Customer
| Field         | CSharp Type | Required | Max Length |
|---------------|-------------|----------|------------|
| Name          | string      | True     | 100        |
| Email         | string      | True     | 200        |
| LoyaltyPoints | int         | False    | 0          |

## Available Connectors
- **SendEmailConnector**: Sends transactional email via SMTP
  Inputs: ToEmail:string, Subject:string, Body:string

## Existing Business Rules (Do NOT duplicate)
- Rule: "ApplyVAT" on Order.BeforeSave — Adds 18% tax if Order.Country == "IN"
```

---

## 3. Step 2 — Full Example: Business Requirement → Connector Code

### 3.1 User's Business Requirement

> *"When an order is placed, calculate the discount. If the customer has more than 500 loyalty points, apply a 10% discount on TotalAmount. If TotalAmount exceeds ₹10,000, apply an additional 5%. Update the Order.FinalAmount with the result. Then trigger the SendEmailConnector to notify the customer."*

### 3.2 AiOrchestrator builds this full prompt

**SYSTEM MESSAGE** (assembled from AiSkill + SchemaContext):

```
You are an expert C# developer for DynamicPlatform — a low-code application builder.
Your ONLY task is to generate the `BusinessLogic` string content for a ConnectorMetadata object.

=== HARD RULES ===
1. Output ONLY a valid JSON object conforming to ConnectorMetadata schema. No markdown, no explanation.
2. The BusinessLogic field must contain compilable C# code only.
3. You MUST use the exact field names as they appear in the Entity definitions below.
4. You MUST NOT instantiate Entity classes (e.g., `new Order()`). Use the input dictionary only.
5. You MAY reference existing connectors by name in comments, but do NOT call them directly in BusinessLogic.
6. Wrap all logic in try/catch. Use `var logger = _logger;` for logging.
7. Always return a typed value from ExecuteAsync. Never return void.

=== PLATFORM CONTEXT ===
## Available Entities

### Entity: Order
| Field        | CSharp Type | Required |
|--------------|-------------|----------|
| OrderNumber  | string      | True     |
| TotalAmount  | decimal     | True     |
| FinalAmount  | decimal     | False    |
| CustomerId   | Guid        | True     |
| Status       | string      | False    |

**Relations:**
- ManyToOne: Order → Customer via `Customer`

### Entity: Customer
| Field         | CSharp Type | Required |
|---------------|-------------|----------|
| LoyaltyPoints | int         | False    |
| Email         | string      | True     |
| Name          | string      | True     |

## Available Connectors
- **SendEmailConnector**: Sends transactional email
  Inputs: ToEmail:string, Subject:string, Body:string

=== OUTPUT FORMAT ===
{
  "name": "...",
  "description": "...",
  "inputs": [ { "name": "...", "type": "..." } ],
  "outputs": [ { "name": "...", "type": "..." } ],
  "businessLogic": "// C# code here"
}
```

**USER MESSAGE**:
```
When an order is placed, calculate the discount. If the customer has more than 500 
loyalty points, apply a 10% discount on TotalAmount. If TotalAmount exceeds 10000, 
apply an additional 5%. Update Order.FinalAmount. Then notify customer via SendEmailConnector.
```

### 3.3 LLM Returns (Raw JSON)

```json
{
  "name": "ApplyOrderDiscount",
  "description": "Calculates loyalty and volume discounts for an order and sets FinalAmount.",
  "inputs": [
    { "name": "TotalAmount", "type": "decimal" },
    { "name": "LoyaltyPoints", "type": "int" },
    { "name": "CustomerEmail", "type": "string" },
    { "name": "CustomerName", "type": "string" },
    { "name": "OrderNumber", "type": "string" }
  ],
  "outputs": [
    { "name": "FinalAmount", "type": "decimal" },
    { "name": "DiscountApplied", "type": "decimal" }
  ],
  "businessLogic": "var discount = 0m;\nif (LoyaltyPoints > 500)\n    discount += TotalAmount * 0.10m;\nif (TotalAmount > 10000)\n    discount += TotalAmount * 0.05m;\nvar FinalAmount = TotalAmount - discount;\nlogger.LogInformation($\"Order {OrderNumber}: Discount={discount}, Final={FinalAmount}\");\n// NOTE: Call SendEmailConnector separately via Workflow with inputs ToEmail=CustomerEmail, Subject=...\nreturn new Dictionary<string, object?> { [\"FinalAmount\"] = FinalAmount, [\"DiscountApplied\"] = discount };"
}
```

### 3.4 IAiResponseParser Validates

```csharp
public ConnectorMetadata ParseConnectorResponse(string rawJson)
{
    // Strip markdown fences if model ignored instructions
    rawJson = Regex.Replace(rawJson, @"```json|```", "").Trim();

    var result = JsonSerializer.Deserialize<ConnectorMetadata>(rawJson)
        ?? throw new AiParseException("Failed to parse connector metadata.");

    // Validate required fields
    if (string.IsNullOrWhiteSpace(result.Name))
        throw new AiParseException("AI output is missing 'name'.");
    if (string.IsNullOrWhiteSpace(result.BusinessLogic))
        throw new AiParseException("AI output is missing 'businessLogic'.");

    // Basic C# sanity check: must not contain banned patterns
    var bannedPatterns = new[] { "new Order(", "new Customer(", "DROP TABLE", "File.Delete" };
    foreach (var banned in bannedPatterns)
        if (result.BusinessLogic.Contains(banned))
            throw new AiSafetyException($"Generated code contains banned pattern: '{banned}'.");

    return result;
}
```

### 3.5 Scriban Renders to Final C# File

The validated `ConnectorMetadata` is saved to the DB. At build time, `Connector.scriban` renders this into the actual `.cs` file:

```csharp
// OUTPUT FILE: Connectors/ApplyOrderDiscountConnector.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Platform.Core.Interfaces;

namespace GeneratedApp.Connectors;

/// <summary>
/// Custom Connector: ApplyOrderDiscount
/// Calculates loyalty and volume discounts for an order and sets FinalAmount.
/// </summary>
public class ApplyOrderDiscountConnector : IConnector
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApplyOrderDiscountConnector> _logger;

    public string Name => "ApplyOrderDiscount";

    public ApplyOrderDiscountConnector(IHttpClientFactory httpClientFactory,
        ILogger<ApplyOrderDiscountConnector> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<object?> ExecuteAsync(IDictionary<string, object?> inputs)
    {
        var http = _httpClientFactory.CreateClient("ApplyOrderDiscount");
        var logger = _logger;

        // Extracting Inputs
        var TotalAmount = inputs.ContainsKey("TotalAmount") ? (decimal)inputs["TotalAmount"]! : default;
        var LoyaltyPoints = inputs.ContainsKey("LoyaltyPoints") ? (int)inputs["LoyaltyPoints"]! : default;
        var CustomerEmail = inputs.ContainsKey("CustomerEmail") ? (string)inputs["CustomerEmail"]! : default;
        var CustomerName = inputs.ContainsKey("CustomerName") ? (string)inputs["CustomerName"]! : default;
        var OrderNumber = inputs.ContainsKey("OrderNumber") ? (string)inputs["OrderNumber"]! : default;

        try
        {
            // ---- AI-GENERATED BUSINESS LOGIC ----
            var discount = 0m;
            if (LoyaltyPoints > 500)
                discount += TotalAmount * 0.10m;
            if (TotalAmount > 10000)
                discount += TotalAmount * 0.05m;
            var FinalAmount = TotalAmount - discount;
            logger.LogInformation($"Order {OrderNumber}: Discount={discount}, Final={FinalAmount}");
            return new Dictionary<string, object?> 
            { 
                ["FinalAmount"] = FinalAmount, 
                ["DiscountApplied"] = discount 
            };
            // ---- END AI-GENERATED LOGIC ----
        }
        catch (Exception ex)
        {
            throw new Exception($"Error executing connector ApplyOrderDiscount: {ex.Message}", ex);
        }
    }
}
```

This file is then compiled via `dotnet build` and becomes a real, deployed API endpoint.

---

## 4. Composite Code — Chaining Multiple Generated Artifacts

Complex scenarios require **composition**: multiple AI-generated connectors wired together in an AI-generated Workflow.

### 4.1 Example: Full Order Processing Pipeline

**Requirement**: *"When an order is created: validate stock, apply discount, process payment, send confirmation email, and generate a PDF invoice."*

**AI generates these as separate artifacts:**

| Artifact | Type | AI Generates |
|---|---|---|
| `ValidateStockConnector` | Connector | Checks stock via API |
| `ApplyOrderDiscountConnector` | Connector | Calculates discount |
| `ProcessPaymentConnector` | Connector | Calls payment gateway |
| `SendOrderConfirmationConnector` | Connector | Sends email |
| `GenerateInvoicePdfConnector` | Connector | Calls PDF generator |
| `OrderProcessingWorkflow` | Workflow | Wires all above in sequence |

### 4.2 AI-Generated Workflow Definition

The AI generates a workflow JSON that references the connectors by name:

```json
{
  "name": "OrderProcessingWorkflow",
  "trigger": { "type": "EntityEvent", "entity": "Order", "event": "OnCreate" },
  "activities": [
    {
      "type": "ExecuteConnector",
      "connectorName": "ValidateStockConnector",
      "inputs": { "OrderId": "{{Trigger.OrderId}}" },
      "output": "StockValidationResult"
    },
    {
      "type": "ConditionalBranch",
      "condition": "{{StockValidationResult.IsValid}} == true",
      "trueBranch": [
        {
          "type": "ExecuteConnector",
          "connectorName": "ApplyOrderDiscountConnector",
          "inputs": {
            "TotalAmount": "{{Trigger.TotalAmount}}",
            "LoyaltyPoints": "{{Customer.LoyaltyPoints}}",
            "OrderNumber": "{{Trigger.OrderNumber}}"
          },
          "output": "DiscountResult"
        },
        {
          "type": "ExecuteConnector",
          "connectorName": "ProcessPaymentConnector",
          "inputs": { "Amount": "{{DiscountResult.FinalAmount}}" },
          "output": "PaymentResult"
        },
        {
          "type": "ExecuteConnector",
          "connectorName": "SendOrderConfirmationConnector",
          "inputs": { "Email": "{{Customer.Email}}", "OrderNumber": "{{Trigger.OrderNumber}}" }
        }
      ],
      "falseBranch": [
        {
          "type": "SetEntityField",
          "entity": "Order",
          "field": "Status",
          "value": "StockUnavailable"
        }
      ]
    }
  ]
}
```

This JSON is saved as an `Artifact` of type `Workflow` and is consumed by the existing Elsa workflow integration at build time.
