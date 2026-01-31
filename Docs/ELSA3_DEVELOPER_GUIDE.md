# Elsa 3 Developer Guidance & Troubleshooting Guide

This document provides technical guidance for developers working with **Elsa 3.x** integration within the **DynamicPlatform**. It covers implementation patterns, common pitfalls, and debugging strategies identified during the migration from Elsa 2.

---

## 1. Core Concepts

In Elsa 3, the architecture shifted from a "centralized" approach to a more "decentralized" and modular one. Key namespaces and classes have changed significantly.

### 1.1 Activity Structure
All custom activities should now inherit from `CodeActivity` (or `Activity` if they are composite).

```csharp
public class MyActivity : CodeActivity
{
    [Input(Description = "Source data")]
    public Input<string> Source { get; set; } = default!;

    [Output]
    public Output<bool> Result { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Use .Get(context) extension method from Elsa.Extensions
        var source = Source.Get(context);
        
        // Resolve services from context
        var myService = context.GetRequiredService<IMyService>();
        
        // Set outputs
        Result.Set(context, true);
    }
}
```

### 1.2 Workflow Definitions (C#)
Workflows are defined by inheriting from `WorkflowBase`.

```csharp
public class MyWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var myVar = new Variable<string>();

        builder.Root = new Sequence
        {
            Variables = { myVar },
            Activities = 
            {
                new Inline(ctx => {
                    // Map workflow input to variable
                    myVar.Set(ctx, ctx.GetWorkflowInput<string>("MyInput"));
                }),
                new MyActivity { Source = new Input<string>(myVar) }
            }
        };
    }
}
```

---

## 2. Common Implementation Patterns

### 2.1 Starting a Workflow Programmatically
Use `IWorkflowRuntime` instead of the legacy `IWorkflowRunner`.

**Correct Pattern:**
```csharp
var result = await _workflowRuntime.StartWorkflowAsync(
    "WorkflowDefinitionId",
    new StartWorkflowRuntimeParams
    {
        Input = new Dictionary<string, object> { ["Key"] = "Value" }
    });
```

**Namespace:** `Elsa.Workflows.Runtime.Parameters`

### 2.2 Registering Activities
In Elsa 3, activities are registered as standard services in the DI container.

```csharp
services.AddTransient<MyActivity>();
// If using Elsa options:
services.AddElsa(elsa => elsa.AddActivitiesFrom<MyActivity>());
```

---

## 3. Troubleshooting & Common Issues

| Issue | Root Cause | Fix |
| :--- | :--- | :--- |
| **`Input<T>.Get()` not found** | Missing `Elsa.Extensions` namespace. | Add `using Elsa.Extensions;` to your file. |
| **`Operator '.' cannot be applied to void`** | Chaining styling methods in QuestPDF/Fluent APIs incorrectly. | Move styling inside the text block or apply to the container *before* calling the terminal method. |
| **Workflow Inputs are `null`** | C# workflows don't automatically map inputs to variables. | Use an `Inline` activity at the start of the `Sequence` to call `ctx.GetWorkflowInput<T>("Key")` and set variables. |
| **`IServiceCollection` missing** | Missing NuGet package in library project. | Add `Microsoft.Extensions.DependencyInjection.Abstractions`. |
| **`WorkflowInstanceFilter` not found** | Incorrect namespace for filters. | Use `using Elsa.Workflows.Management.Filters;`. |
| **`StartWorkflowRuntimeOptions` missing** | Renamed to `StartWorkflowRuntimeParams` in Elsa 3.2. | Use `StartWorkflowRuntimeParams` from `Elsa.Workflows.Runtime.Parameters`. |

---

## 4. Debugging Tips

1.  **Check Workflow Instance Store**: Query the `WorkflowInstances` table or use `IWorkflowInstanceStore` to inspect the `Status`, `SubStatus`, and `Fault` properties.
2.  **Inspect Variable Storage**: If an activity seems to be receiving null inputs, verify the variable initialization in the `Build` method. C# Workflow variables must be explicitly added to the `Variables` collection of a `Sequence` or `Workflow`.
3.  **Logging**: Use `ActivityExecutionContext.GetRequiredService<ILogger<T>>()` inside `ExecuteAsync` to log specific execution details of your custom activity.
4.  **Activity Discovery**: If your custom activity doesn't appear in the designer or fails to resolve, ensure it is registered as `Transient` or `Scoped` in `ServiceCollection`.

---

## 5. Migration Checklist (Elsa 2 -> 3)
- [ ] Change `[ActivityInput]` to `[Input]`.
- [ ] Change `[ActivityOutput]` to `[Output]`.
- [ ] Change `BuildAsync` or `OnExecuteAsync` to `ExecuteAsync`.
- [ ] Update `IWorkflowInstanceStore` calls to use new filter models.
- [ ] Move any Activity-specific DI resolution to `context.GetRequiredService<T>()`.
- [ ] Ensure `IWorkflowRuntime` is used for triggering executions.
