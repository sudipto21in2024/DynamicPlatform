# Workflow Automation with Elsa 3.1

DynamicPlatform integrates **Elsa Workflows 3.x** to provide a high-performance, distributed workflow engine for automating business processes.

---

## 1. Architectural Integration

The workflow engine is integrated into the platform across three layers:

### 1.1. Persistence Layer
Workflows and their runtime state (instances, bookmarks) are persisted in **PostgreSQL**.
- **Management DB**: Stores workflow definitions.
- **Runtime DB**: Stores active and completed workflow instances.
- **Shared Connection**: Uses the same project-isolated connection string as the entities, ensuring data locality.

### 1.2. Metadata Layer
Within the Platform Studio, Workflows are stored as `Artifacts` with `Type: Workflow`.
- **Content**: Stores the raw Elsa JSON definition.
- **Version Control**: Every save creates a new version of the process.

### 1.3. Runtime Layer
The `Platform.API` acts as a multi-tenant workflow host.
- **API Endpoints**: Exposed at `/workflows/api` for management.
- **HTTP Activities**: Workflows can listen for incoming HTTP requests at `/workflows/{project_id}/{trigger_path}`.

---

## 2. Standalone Code Generation

When "Output as ZIP" is triggered, the engine performs the following:

- **Dependency Injection**: Detects workflows and adds `Elsa` packages to the `.csproj`.
- **Registration**: Injects `builder.Services.AddElsa()` into `Program.cs`.
- **Definition Embedding**: Copies all project workflows into the `/Workflows` folder as JSON.
- **Hot-loading**: The standalone app is configured to find and register these JSON definitions on startup.

---

## 3. Sample Workflows

Below are two sample workflow definitions (expressed conceptually) that demonstrate the power of the platform.

### Sample A: Automated Order Fulfillment
**Goal**: Process a new order, update stock, and notify the customer.

1.  **HTTP Trigger**: `POST /order-webhook`
2.  **Logic Node**: Check if `StockCount > 0`.
3.  **Entity Action**: Create `Order` record in Database.
4.  **Custom Connector**: Call `SmtpEmailSender` (Custom Connector we built earlier).
5.  **Output**: Response `200 OK` to the caller.

### Sample B: Weekly Inventory Report
**Goal**: Run a scheduled task to identify low-stock items.

1.  **Timer Trigger**: `Cron: 0 0 * * MON` (Every Monday at midnight).
2.  **Entity Action**: Query `Product` entity where `Quantity < ReorderLevel`.
3.  **Loop Action**: For each low-stock item...
4.  **Integration**: POST to Slack Webhook (using an Integration Connector).

---

## 4. Consuming Custom Connectors in Workflows

One of the unique features of our implementation is the **Unified Activity Library**. 

Because your **Custom Connectors** (like `EmailSender`) are registered in the DI container of the exported app, they can be directly referenced inside the workflow logic using Elsa's **Scripting** nodes:

```javascript
// Inside an Elsa JavaScript Node
var mailer = resolve('EmailSenderConnector');
await mailer.ExecuteAsync({ "To": "admin@local.com", "Subject": "Alert" });
```

---

## 5. Deployment Considerations

- **Isolation**: Workflows run within the context of the project's isolated environment.
- **Scalability**: Elsa 3.x supports distributed execution, allowing your published apps to scale across multiple instances while sharing the same workflow state in the database.
