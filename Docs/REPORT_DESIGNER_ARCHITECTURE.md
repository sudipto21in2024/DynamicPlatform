# Report Designer Architecture & Business Specification

## 1. Executive Summary
The **Dynamic Platform Report Designer** is a critical subsystem designed to empower users to create, manage, and execute complex data reports. It addresses the need for both real-time operational insights ("Quick Jobs") and heavy historical data processing ("Long-Running Jobs"). The module bridges the gap between low-code ease of use (Visual Data Designer) and pro-code flexibility (Code/Logic Window), ensuring that 100% of reporting requirements can be met within the platform.

## 2. Business Requirements & Capabilities

### 2.1 Core Capabilities
| Capability | Description |
| :--- | :--- |
| **Hybrid Job Model** | Support for **Synchronous (Quick)** and **Asynchronous (Long-Running)** execution modes. |
| **Dual-Mode Designer** | A **Visual Query Builder** for business users and a **Code Editor (C#/LINQ/SQL)** for complex logic. |
| **Multi-Format Output** | Native generation of **Excel (.xlsx)** and **PDF** documents with pixel-perfect control. |
| **Parameterization** | Dynamic parameter inputs (Date Ranges, Entity Selectors, Booleans) auto-generated at runtime. |
| **Scheduling** | (Future) Cron-based scheduling for automated report delivery via Email/Storage. |

### 2.2 User Scenarios
1.  **Operational Dashboard Report**: A user needs a daily sales summary (Quick Job). They drag-and-drop the "Order" entity, summarize "TotalAmount" by "Date", and view it immediately.
2.  **Regulatory Compliance Report**: A user needs a 5-year audit log export (Long-Running Job). They write a complex stored procedure or LINQ query in the Code Window. The system ques the job, calculates millions of rows in the background, and notifies the user via Email/Notification when the PDF is ready.
3.  **Invoice Generation**: A user generates a specifically formatted PDF invoice for a single order (Quick Job, formatted output).

## 3. Technical Architecture

### 3.1 Metadata Architecture
Reports are defined purely as metadata, stored in the `ReportDefinition` entity.

```json
{
  "reportId": "guid",
  "name": "Quarterly Sales",
  "type": "Visual", // or "Code"
  "executionMode": "LongRunning", // or "Quick"
  "dataSource": {
    "entity": "SalesOrder",
    "joins": [...],
    "aggregations": [...]
  },
  "codeLogic": null, // C# script if type == "Code"
  "layout": {
    "type": "Table", // or "FreeForm" for Invoices
    "columns": [
      { "field": "OrderDate", "title": "Date", "format": "yyyy-MM-dd" },
      { "field": "Total", "title": "Amount", "format": "Currency" }
    ]
  },
  "parameters": [
    { "name": "StartDate", "type": "DateTime", "required": true }
  ]
}
```

### 3.2 Execution Engine
The engine splits execution based on `executionMode`.

#### A. Quick Jobs (Synchronous)
*   **Flow**: API Call -> Metadata Interpreter -> DB Query -> In-Memory Document Generation -> HTTP Response (Stream).
*   **Timeout**: Strict 30s timeout.
*   **Use Case**: UI Grids, Simple Exports, Single Record Prints (Invoices).

#### B. Long-Running Jobs (Asynchronous)
*   **Tech Stack**: Background Job System (e.g., Hangfire or Native .NET Worker Service).
*   **Flow**:
    1.  User clicks "Run".
    2.  API creates a `JobInstance` record (Status: Queued).
    3.  Background Worker picks up the job.
    4.  Worker executes logic (Chunked processing to avoid memory overflows).
    5.  Generated file is streamed to Blob Storage (Azure Blob / S3 / MinIO).
    6.  `JobInstance` updated to "Completed" with `DownloadUrl`.
    7.  Notification sent to User (SignalR / Email).

### 3.3 The Visual Data Designer (UI)
A React/Angular-based builder component.
*   **Entity Picker**: Select root entity.
*   **Relation Mapper**: Visual tree to select related fields (e.g., `Order -> Customer -> Name`).
*   **Filter Builder**: Groupable conditions (AND/OR).
*   **Aggregation**: Sum, Count, Average, Min, Max grouping.
*   **Preview**: Live data preview limited to 10 rows.

### 3.4 The Code Writing Window (Pro-Code)
For scenarios where visual builders fail (CTEs, Recursive queries, external API merges).
*   **Language**: C# (Scripting) or raw SQL.
*   **Intellisense**: Basic autocomplete for Entity names.
*   **Security**: Sandbox execution (roslyn-scripting) with read-only DB context.

### 3.5 Output Generators
*   **XLSX Engine**: Uses **OpenXML SDK** or **EPPlus** (if license permits) / **ClosedXML**.
    *   Features: Multi-sheet, Cell styling, Formulas.
*   **PDF Engine**: Uses **QuestPDF** (Community License) or **DinkToPdf**.
    *   Features: Headers/Footers, Pagination, Image embedding (Logos).

## 4. Implementation Plan

### Phase 1: Foundation
1.  Create `ReportDefinition` and `ReportExecutionLog` entities.
2.  Implement `IReportExecutor` interface.
3.  Implement `QuickJobExecutor` using generic repository pattern.

### Phase 2: Visual Designer & Excel
1.  Build the Frontend UI for Visual Builder.
2.  Implement `VisualReportGenerator` service to translate JSON -> LINQ -> OpenXML.
3.  Add "Export to Excel" button on all Dynamic Grids (reuses this logic).

### Phase 3: Long-Running & PDF
1.  Integrate Background Worker (HostedService).
2.  Implement `LongRunningJobManager`.
3.  Implement `PdfGenerator` using QuestPDF.
4.  Add "Job Status" panel in the UI.

### Phase 4: Pro-Code & Advanced
1.  Implement Roslyn-based Code Report execution.
2.  Add Scheduling capabilities.

## 5. Missing Scenarios & Risk Mitigation

### 5.1 Security & Data Access
*   **Risk**: A user runs a report seeing data they shouldn't.
*   **Solution**: The Report Engine must apply **Row-Level Security (RLS)** filters automatically. The generated query must inject `Where(x => x.TenantId == CurrentUser.TenantId)` implicitly.

### 5.2 Performance Overheads
*   **Risk**: A user requests a "Dump All" report with 10 million rows, crashing the server memory.
*   **Solution**:
    *   Enforce **Hard Limits** (e.g., Max 10k rows for Quick Jobs).
    *   Use **Streaming** processing for Long-Running jobs (Read 1000 rows -> Write to Stream -> Discard Iteration). Never load full dataset into list.

### 5.3 Versioning
*   **Scenario**: A report definition changes while a scheduled job is pending.
*   **Solution**: Job Instances should snapshot the *Code/Metadata* at time of queuing, or simply use the latest version with a warning log.

### 5.4 Parameters
*   **Scenario**: Dynamic Dropdowns in Parameter inputs (e.g., "Select Region").
*   **Solution**: Report parameters can be linked to other Entities. The UI renders a Dropdown fetching from that Entity.
