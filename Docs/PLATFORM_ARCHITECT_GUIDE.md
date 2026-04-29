# DynamicPlatform: Architect's Guide & Feature Overview

## Executive Summary

**DynamicPlatform** is an enterprise-grade Low-Code Application Platform (LCAP) designed for professional developers and architects. Unlike traditional low-code tools that lock you into proprietary runtimes, DynamicPlatform acts as a **smart compiler**: it takes high-level metadata (Data Models, UI Layouts, Workflows) and generates **production-ready, human-readable .NET 9 source code**.

It focuses on generating clean, maintainable **Monolithic** architectures that are robust, scalable, and easy to deploy.

---

## 1. Data Modeling & Entity Designer
**The Foundation of Your Application**

The Entity Designer is a powerful database schema builder and modeling tool. It allows you to define the "What" of your system—Entities and Relationships—without worrying about the underlying SQL or Entity Framework configurations.

### Key Capabilities
*   **Visual Modeling**: Drag-and-drop entities and define relationships (`1:1`, `1:N`).
*   **Rich Data Types**: Support for primitives, enums, and complex types.
*   **Database Agnostic**: Generates EF Core mappings compatible with SQL Server, PostgreSQL, and Oracle.
*   **Code Generation**: Automatically generates C# Entities (POCOs) and EF Core DbContext.

### Architect's Example: "E-Commerce Order System"
**Requirement**: Create an Order system where an Order has multiple Line Items.

**Platform Implementation**:
1.  Create `Order` entity.
2.  Create `OrderItem` entity.
3.  Draw a 1:N relationship from `Order` to `OrderItem`.
4.  Define properties like `OrderDate`, `TotalAmount` on Order, and `ProductId`, `Quantity` on OrderItem.

**Generated Code**:
```csharp
// The platform generates clean EF Core entities
public class Order : Entity<Guid>
{
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Navigation Property
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
```

---

## 2. Low-Code UI Construction
**Page, Widget, and Form Designers**

DynamicPlatform separates UI concerns into three specialized designers, ensuring reusability and consistency. The output is a modern **Angular (SPA)** application.

### Key Capabilities
*   **Page Designer**: Layouts, Application Shell, Navigation, and Dashboard composition.
*   **Connector-Driven**: Widgets connect to APIs or Workflows via abstract data sources.
*   **Form Architect**: Specialized for high-fidelity data entry with validation rules, conditional logic, and field grouping.
*   **Widget Studio**: Create reusable UI components (e.g., "Product Card", "KPI Metric") that can be dropped onto any page.

### Architect's Example: "Patient Intake Dashboard"
**Requirement**: A dashboard showing a list of recent patients and a modal form to register a new patient.

**Platform Implementation**:
1.  **Widget Studio**: Build a `PatientCard` widget displaying Name, Age, and Status.
2.  **Form Architect**: Design `RegistrationForm` bound to the `Patient` entity. Add conditional logic: *Show "Guardian Name" only if Age < 18*.
3.  **Page Designer**:
    *   Drag a `Grid` layout.
    *   Drop a `Repeater` control bound to the `GetRecentPatients` API.
    *   Inside the Repeater, drop the `PatientCard` widget.
    *   Add a logic flow: *On "Add Patient" button click -> Open Modal(RegistrationForm)*.

---

## 3. Business Automation & Workflow Engine
**Elsa 3.1 Integration**

Workflows in DynamicPlatform are the "Brain" of your application. They orchestrate logic across entities, external services, and users. Unlike simple "If-This-Then-That" tools, this is a full-state orchestration engine capable of long-running processes (Sagas).

### Key Capabilities
*   **Event-Driven Triggers**: Start flows on `EntityCreated`, `EntityUpdated`, `Timer`, or `Webhook`.
*   **Visual Flowchart**: Drag-and-drop logic nodes (If/Else, Switch, Loop, Parallel).
*   **Human-in-the-Loop**: "User Task" nodes that wait for human approval before proceeding.
*   **Versioning**: Workflows are versioned; running instances continue on V1 while new requests start on V2.

### Architect's Example: "Loan Approval Process"
**Requirement**: When a Loan Application is submitted, check credit score. If > 700, auto-approve. If 600-700, require Manager Approval. If < 600, auto-reject.

**Platform Implementation**:
1.  **Trigger**: `LoanApplication Created`.
2.  ** Action**: Call `CreditBureauConnector` (Input: SSN, Output: Score).
3.  **Switch Node**:
    *   **Case (> 700)**: Update Status = "Approved", Send Email.
    *   **Case (600-700)**: logic flow -> `UserTask(Assignee: "Manager")` -> Wait for Signal -> Update Status based on outcome.
    *   **Case (< 600)**: Update Status = "Rejected".

---

## 4. Connectivity Hub & Integration
**Connecting to the World**

The Connectivity Hub allows you to wrap external logic and APIs into reusable "Connectors". These connectors become native tools in the Workflow Engine and UI Designer.

### Key Capabilities
*   **Connector SDK**: Write custom C# logic to wrap complex libraries (e.g., SAP SDK, AWS S3).
*   **Rest API Wrapper**: Import OpenAPI/Swagger definitions to auto-generate connectors for external REST services.
*   **Configuration Management**: Connectors define their own config (API Keys, URLs) which are managed securely per environment.

### Architect's Example: "SAP ERP Sync"
**Requirement**: Every time a new Customer is created in the Platform, sync it to a legacy SAP instance.

**Platform Implementation**:
1.  **Developer**: Writings a C# implementation of `IConnector` called `SapCustomerSync` that uses the NCo 3.0 library.
2.  **Metadata**: Defines inputs (`CustomerData`) and configuration (`SAP Connection String`).
3.  **Workflow**: Use the `EntityCreated<Customer>` trigger -> Add `SapCustomerSync` node -> Map fields.
4.  **Result**: The platform handles dependency injection, logging, and error handling automatically.

---

## 5. Deployment & Infrastructure
**Production-Ready Monolith**

The platform generates a standard, modularized **Monolithic** architecture that is easy to build, test, and deploy.

### Capabilities
*   **Standard .NET Solution**: Generates a single `.sln` with well-structured projects (API, Core, Infrastructure).
*   **Docker Ready**: Automatically generates a `Dockerfile` for the API and background workers.
*   **CI/CD Friendly**: The output is standard code that can be built by any pipeline (GitHub Actions, Azure DevOps).

### Architect's Example: "Rapid Deployment"
**Scenario**: You need to deploy a new CRM system to Azure App Service.

**Platform Implementation**:
1.  **Publish**: Click the "Publish" button in the Studio.
2.  **Artifacts**: The platform produces a ZIP file containing the source code and a `Dockerfile`.
3.  **Deploy**: Push the code to your Git repository connected to Azure App Service.
4.  **Result**: The application builds and starts running. Database migrations are applied automatically on startup.

---

## Summary of Artifacts Generated

When you click "Publish", the platform dictates a Standard Project Structure:

| Directory | Content |
|-----------|---------|
| `/src/Core` | EF Core Entities, Interface Definitions (Pure C#) |
| `/src/Infrastructure` | DbContext, Repositories, Migrations |
| `/src/API` | ASP.NET Core Controllers (REST), Swagger Docs |
| `/src/Workflows` | Compiled Elsa Workflow Definitions |
| `/frontend` | Angular 17+ Project (Components, Services, Routing) |

DynamicPlatform ensures that **Architects define the 'What' and 'Why', while the Platform handles the 'How'.**
