# Gap Analysis: Workflow & Connector Implementation

After analyzing the current architecture and comparing it against the "Real-Life Scenarios" described in the documentation, I have identified several key technical gaps that need to be addressed to ensure the system is fully operational.

---

## 1. Trigger Gap: Entity Event Dispatching
**Status**: ❌ Gaps Detected
**Issue**: Our generated `Repository.cs` files perform standard EF Core saves but do not "emit" any events. 
**Impact**: The Elsa `EntityCreatedTrigger` or `OnCreated<T>` nodes will never fire because the workflow engine doesn't know a record was saved to the project-isolated database.
**Requirement**: We need to update the `Repository.scriban` template to inject Elsa's `IWorkflowRuntime` and trigger a "Signal" or "Event" after `SaveChangesAsync()`.

---

## 2. Infrastructure Gap: Workflow Hot-Loading
**Status**: ❌ Gaps Detected
**Issue**: The `BuildController` correctly places `.json` workflow definitions into the ZIP, but our `Program.cs` template only initializes the Elsa engine; it doesn't tell Elsa to "Watch" or "Load" the files in the `/Workflows` folder.
**Impact**: The exported app will run, but no workflows will be active or registered in the runtime.
**Requirement**: Update `Program.cs` template to use Elsa's `AddWorkflowsFromDirectory` or a custom loader that reads our exported JSON files.

---

## 3. Connector Gap: Utility & Service Access
**Status**: ❌ Gaps Detected
**Issue**: The current `Connector.scriban` creates a class with properties, but the `ExecuteAsync` block is purely logical. It lacks access to standard services like `HttpClient` (for API calls) or `ILogger` (for debugging).
**Impact**: Scenarios like "Data Enrichment Pipeline" (calling Clearbit/Apollo APIs) cannot be implemented because there is no way to make an outbound HTTP call from the connector snippet.
**Requirement**: Update the `ConnectorGenerator` to support Dependency Injection in generated classes.

---

## 4. UI Gap: Task & Signal Management
**Status**: ❌ Gaps Detected
**Issue**: Scenarios like "Manager Approval" require a human to "Signal" the workflow to continue.
**Impact**: There is currently no generated UI or API endpoint in the standalone app for a user to "Approve" a task.
**Requirement**: We need a standard `WorkflowController` in the export template that exposes Elsa's "Signal" or "Trigger" endpoints to the frontend.

---

## 5. Metadata Gap: Entity Context in Workflows
**Status**: ⚠️ Partial Gap
**Issue**: Elsa nodes need to "Know" about our generated `GeneratedDbContext` and `Repositories` to perform "Update Entity" or "Query History" actions.
**Impact**: Workflows can't easily perform complex queries (like the "Burn Rate" calculation) without manual SQL or complex configuration.
**Requirement**: We must ensure our `GeneratedDbContext` is registered in Elsa's Service Provider context.

---

## Summary of Planned Fixes:
1.  **Repository Trigger**: Add event emission to the Repository template.
2.  **Workflow Loader**: Update the bootstrapper to auto-load JSON definitions.
3.  **Connector DI**: Allow connectors to use `HttpClient` and `ILogger`.
4.  **Workflow API**: Add a system controller to the exported app to handle signals/approvals.
