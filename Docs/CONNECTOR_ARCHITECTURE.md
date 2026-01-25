# Custom Connector Framework Architecture

The **Custom Connector Framework** is a key extensibility feature of DynamicPlatform. It allows professional developers to create reusable units of business logic (Connectors) that low-code users can consume in workflows or UI interactions.

---

## 1. Architectural Philosophy

A Connector in DynamicPlatform is a **Single Unit of Code** that combines:
1.  **Metadata**: Defines the structure (Inputs, Outputs, and Configuration).
2.  **Implementation**: The actual C# logic (transpiled from a snippet).
3.  **Registration**: Automated Dependency Injection (DI) wiring.

Connectors are designed to be "stateless" execution units that implement a standard interface, making them compatible with both the **API layer** and the **Workflow Engine**.

---

## 2. Technical Stack

- **Interface**: `IConnector` (defined in `Platform.Core`).
- **Templating**: Scriban-based code generation.
- **Bootstrapping**: Automatic `AddScoped<T>` registration in the exported `Program.cs`.
- **Serialization**: JSON-based storage in the `Artifacts` table (`Type: Connector`).

---

## 3. Connector Components

### 3.1. Metadata Definition
```json
{
  "name": "SmtpEmailSender",
  "description": "Sends transactional emails via SMTP",
  "configProperties": [
    { "name": "Host", "type": "string", "defaultValue": "smtp.sendgrid.net" },
    { "name": "Port", "type": "int", "defaultValue": "587" }
  ],
  "inputs": [
    { "name": "ToEmail", "type": "string" },
    { "name": "Subject", "type": "string" },
    { "name": "Body", "type": "string" }
  ],
  "businessLogic": "Console.WriteLine($\"Sending email to {ToEmail} via {Host}...\");\n// Custom C# Logic goes here\nreturn true;"
}
```

### 3.2. Core Interface (`IConnector.cs`)
```csharp
public interface IConnector
{
    string Name { get; }
    Task<object?> ExecuteAsync(IDictionary<string, object?> inputs);
}
```

---

## 4. Generated Code Example

When the "SmtpEmailSender" metadata is processed by the **ConnectorGenerator**, the following C# class is produced in the `Connectors/` directory:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Core.Interfaces;

namespace GeneratedApp.Connectors;

public class SmtpEmailSenderConnector : IConnector
{
    public string Name => "SmtpEmailSender";

    // Configuration Properties (from Metadata)
    public string Host { get; set; } = "smtp.sendgrid.net";
    public int Port { get; set; } = 587;

    public async Task<object?> ExecuteAsync(IDictionary<string, object?> inputs)
    {
        // 1. Automatic Input Mapping
        var ToEmail = inputs.ContainsKey("ToEmail") ? (string)inputs["ToEmail"]! : default;
        var Subject = inputs.ContainsKey("Subject") ? (string)inputs["Subject"]! : default;
        var Body = inputs.ContainsKey("Body") ? (string)inputs["Body"]! : default;

        try 
        {
            // 2. User-Defined Business Logic Block
            Console.WriteLine($"Sending email to {ToEmail} via {Host}...");
            // Simulated SMTP Logic
            await Task.Delay(100); 
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error executing connector SmtpEmailSender: {ex.Message}", ex);
        }
    }
}
```

---

## 5. Deployment & Consumption

### 5.1. Dependency Injection
In the exported `Program.cs`, the platform automatically adds:
```csharp
// Auto-generated during build
builder.Services.AddScoped<SmtpEmailSenderConnector>();
```

### 5.2. Workflow Integration
Since the connector implements a standard interface and is registered in the DI container, a Workflow Engine can dynamically invoke it:
1.  **Discover**: Scan for classes implementing `IConnector`.
2.  **Configure**: Map workflow variables to the `inputs` dictionary.
3.  **Execute**: Call `ExecuteAsync`.

### 5.3. Custom Logic "Hole"
Connectors act as the primary way to escape the boundaries of the low-code platform. By providing a "Business Logic" textarea in the Studio, developers can perform:
- Custom API Calls (using `HttpClient`).
- Complex Data Processing.
- Integration with Legacy Systems.
