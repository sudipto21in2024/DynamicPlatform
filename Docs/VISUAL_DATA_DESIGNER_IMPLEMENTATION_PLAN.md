# Visual Data Designer Implementation Plan

## Overview

This plan outlines the implementation of the **Visual Data Designer** system, which provides a low-code interface for building complex data queries, transformations, and reports. The system includes a generic wrapper API that supports multiple data providers (Entity, API, Workflow, Static) and can execute both quick synchronous jobs and long-running asynchronous jobs.

**Long-Running Jobs**: This implementation uses **Elsa Workflow** (already integrated in the platform) for orchestrating long-running data operations. This provides visual monitoring, built-in state management, retry logic, and scheduling capabilities without requiring additional infrastructure like Hangfire.

> **📖 Developer Guide**: For detailed implementation instructions on Elsa Workflow integration, see [Elsa Workflow Integration Guide](file:///C:/Sudipto/Antigravity/DynamicPlatform/Docs/ELSA_WORKFLOW_INTEGRATION_GUIDE.md)

---

## User Review Required

> [!IMPORTANT]
> **Breaking Changes & Design Decisions**
> 
> 1. **New Database Tables**: This implementation requires new tables (`ReportDefinition`, `JobInstance`) in the platform database.
> 2. **Background Job System**: We will use **Elsa Workflow** (already integrated in the platform) for long-running job orchestration. This provides visual monitoring, built-in state management, retry logic, and scheduling capabilities.
> 3. **Output Libraries**: 
>    - Excel: **EPPlus** (requires commercial license for commercial use) or **ClosedXML** (free, MIT license)
>    - PDF: **QuestPDF** (Community License, free for non-commercial) or **DinkToPdf**
> 4. **API Design**: Single unified endpoint `/api/data/execute` handles all data operations. This differs from traditional REST patterns but provides better flexibility.
> 5. **Security Model**: Row-Level Security (RLS) filters will be **automatically injected** into all queries. Users cannot bypass this through metadata manipulation.
> 6. **Workflow Integration**: Long-running jobs will execute as Elsa workflows, allowing visual monitoring in Elsa Studio and leveraging existing workflow infrastructure.

> [!WARNING]
> **Performance Considerations**
> 
> - Quick Jobs have a hard limit of **10,000 rows** and **30-second timeout**
> - Long-Running Jobs will use **chunked processing** (1000 rows per chunk) via Elsa workflow activities
> - Large result sets will be streamed to blob storage (requires Azure Blob Storage, AWS S3, or MinIO setup)
> - Workflow state is automatically persisted by Elsa, enabling pause/resume and failure recovery

---

## Proposed Changes

### Backend Infrastructure

#### ✅ [COMPLETED] [DataOperationMetadata.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Models/DataOperationMetadata.cs)

Define the core metadata model for data operations:
- `OperationType` enum (Query, Aggregate, Union, Pivot, Execute)
- `RootEntity`, `Fields`, `Joins`, `Filters` properties
- `Aggregations`, `GroupBy`, `OrderBy` properties
- `CalculatedFields`, `WindowFunctions`, `ExternalSources` properties

#### ✅ [COMPLETED] [FilterDefinitions.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Models/FilterDefinitions.cs)

Define filter-related models:
- `FilterGroup` class with `Operator` (AND/OR) and nested `Conditions`
- `FilterCondition` class with `Field`, `Operator`, `Value`
- Support for subquery filters

#### ✅ [COMPLETED] [IDataProvider.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Interfaces/IDataProvider.cs)

Define the provider interface:
```csharp
public interface IDataProvider
{
    string ProviderType { get; }
    Task<DataResult> ExecuteAsync(DataOperationMetadata metadata, Dictionary<string, object> parameters, ExecutionContext context, CancellationToken cancellationToken);
    Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata);
    Task<long> EstimateRowCountAsync(DataOperationMetadata metadata);
}
```

---

#### ✅ [COMPLETED] [EntityDataProvider.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/DataProviders/EntityDataProvider.cs)

Implement the Entity data provider:
- Parse metadata to LINQ expressions
- Apply joins using `Include()` or manual joins
- Apply filters recursively (handle AND/OR groups)
- Apply RLS filters automatically
- Execute with timeout and cancellation support

#### ⏳ [PENDING] [ApiDataProvider.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/DataProviders/ApiDataProvider.cs)

Implement the API data provider:
- Build HTTP requests from metadata
- Apply authentication profiles
- Execute with retry policy (Polly)
- Parse and map JSON responses

#### ⏳ [PENDING] [WorkflowDataProvider.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/DataProviders/WorkflowDataProvider.cs)

Implement the Workflow data provider:
- Resolve workflow by name
- Execute workflow with parameters
- Return workflow output as data result

#### ✅ [COMPLETED] [StaticDataProvider.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/DataProviders/StaticDataProvider.cs)

Implement the Static data provider:
- Simply return parameters as data (for mocking/testing)

---

#### ✅ [COMPLETED] [DynamicQueryBuilder.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/DynamicQueryBuilder.cs)

Implement the query builder:
- `BuildQuery()` method to translate metadata to `IQueryable`
- `ApplyJoin()` method for relationship navigation
- `ApplyFilters()` method with recursive AND/OR handling
- `ApplyAggregations()` method for GroupBy and aggregate functions
- `ApplyFieldSelection()` method for dynamic projection
- `ApplyOrdering()` and `ApplyPagination()` methods

#### ✅ [COMPLETED] [DataExecutionEngine.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/DataExecutionEngine.cs)

Implement the execution orchestrator:
- Provider selection based on request
- Metadata validation
- Quick job execution with 30s timeout
- Long-running job queueing
- Output format generation

---

#### ✅ [COMPLETED] [ReportDefinition.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Models/ReportDefinition.cs)

Entity model for storing report definitions:
- `Id`, `Name`, `Description`
- `Type` (Visual or Code)
- `ExecutionMode` (Quick or LongRunning)
- `MetadataJson` (serialized `DataOperationMetadata`)
- `OutputFormat` (JSON, Excel, PDF, CSV)
- `Parameters` (JSON array of parameter definitions)

#### ✅ [COMPLETED] [JobInstance.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Models/JobInstance.cs)

Entity model for tracking job execution:
- `JobId`, `ReportDefinitionId`, `UserId`
- `Status` (Queued, Running, Completed, Failed)
- `Progress`, `RowsProcessed`, `TotalRows`
- `StartedAt`, `CompletedAt`, `EstimatedCompletion`
- `DownloadUrl`, `ErrorMessage`

---

#### ⏳ [PENDING] [JobTrackingService.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/JobTrackingService.cs)

Implement job tracking service:
- `CreateJobAsync()` to create job record and link to Elsa workflow instance
- `GetJobAsync()` to retrieve job status from Elsa workflow state
- `UpdateProgressAsync()` to update job progress
- `CompleteJobAsync()` and `FailJobAsync()` for job completion

---

### Elsa Workflow Activities

#### ⏳ [PENDING] [ExecuteDataQueryActivity.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Workflows/Activities/ExecuteDataQueryActivity.cs)

Elsa activity for executing data queries:
- Chunked data processing (1000 rows per chunk)
- Progress tracking via workflow variables
- Cancellation token support
- Automatic retry on transient failures

#### ⏳ [PENDING] [GenerateReportOutputActivity.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Workflows/Activities/GenerateReportOutputActivity.cs)

Elsa activity for generating report output:
- Support for Excel, PDF, CSV formats
- Uses IOutputGenerator implementations
- Returns file stream for upload

#### ⏳ [PENDING] [UploadToStorageActivity.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Workflows/Activities/UploadToStorageActivity.cs)

Elsa activity for uploading to blob storage:
- Upload generated file to configured storage
- Return download URL
- Cleanup temporary files

#### ⏳ [PENDING] [NotifyUserActivity.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Workflows/Activities/NotifyUserActivity.cs)

Elsa activity for user notification:
- Send email notification
- Send SignalR real-time notification
- Include download link and job status

---

### Workflow Definitions

#### ⏳ [PENDING] [LongRunningReportWorkflow.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Workflows/Definitions/LongRunningReportWorkflow.cs)

Elsa workflow definition for long-running reports:
- Orchestrates: ExecuteDataQuery → GenerateOutput → Upload → Notify
- Built-in error handling and compensation
- Progress tracking at each step
- Automatic state persistence

---

### Output Generators

#### ✅ [COMPLETED] [IOutputGenerator.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Interfaces/IOutputGenerator.cs)

Define output generator interface:
```csharp
public interface IOutputGenerator
{
    string Format { get; }
    Task<Stream> GenerateAsync(IEnumerable<object> data, OutputOptions options);
}
```

#### ⏳ [PENDING] [ExcelOutputGenerator.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/OutputGenerators/ExcelOutputGenerator.cs)

Implement Excel generation using EPPlus or ClosedXML:
- Write headers from data properties
- Write data rows
- Apply basic styling (header bold, borders)
- Support for multiple sheets (future)

#### ⏳ [PENDING] [PdfOutputGenerator.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/OutputGenerators/PdfOutputGenerator.cs)

Implement PDF generation using QuestPDF:
- Table layout with headers
- Pagination support
- Header/footer with page numbers
- Logo embedding (optional)

#### ✅ [COMPLETED] [CsvOutputGenerator.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Services/OutputGenerators/CsvOutputGenerator.cs)

Implement CSV generation:
- Simple comma-separated format
- Proper escaping of quotes and commas
- UTF-8 encoding with BOM

---

### API Controllers

#### ⏳ [PENDING] [DataExecutionController.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.API/Controllers/DataExecutionController.cs)

Implement the main API endpoint:
- `POST /api/data/execute` - Execute data operation
- `GET /api/data/jobs/{jobId}/status` - Get job status
- `GET /api/data/jobs/{jobId}/download` - Download completed job result
- `DELETE /api/data/jobs/{jobId}` - Cancel running job

Request/response models:
- `DataExecutionRequest`
- `DataExecutionResponse`
- `JobStatusResponse`

---

### Frontend Components

#### ⏳ [PENDING] [visual-data-designer.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/visual-data-designer.component.ts)

Main designer component:
- Orchestrates all sub-components
- Manages metadata state
- Provides preview functionality
- Handles save/load operations

#### ⏳ [PENDING] [entity-picker.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/entity-picker.component.ts)

Entity selection component:
- Dropdown list of available entities
- Search/filter capability
- Display entity descriptions

#### ⏳ [PENDING] [field-selector.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/field-selector.component.ts)

Field selection component:
- Tree view of entity fields
- Support for related entity navigation
- Checkbox multi-select
- Drag-to-reorder

#### ⏳ [PENDING] [filter-builder.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/filter-builder.component.ts)

Filter builder component:
- Visual condition builder
- AND/OR group support
- Dynamic operator dropdown based on field type
- Value input with validation

#### ⏳ [PENDING] [join-mapper.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/join-mapper.component.ts)

Join configuration component:
- Visual relationship tree
- Automatic join suggestions
- Join type selector
- Custom join condition editor

#### ⏳ [PENDING] [aggregation-panel.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/aggregation-panel.component.ts)

Aggregation configuration component:
- Function selector (Sum, Count, Avg, etc.)
- Field selector
- Group By configuration
- Having clause builder

#### ⏳ [PENDING] [preview-panel.component.ts](file:///c:/Sudipto/Antigravity/DynamicPlatform/platform-studio/src/app/components/visual-data-designer/preview-panel.component.ts)

Data preview component:
- Live data preview (10 rows max)
- Column headers with types
- Execution time display
- Refresh button

---

### Database Migrations

#### ⏳ [PENDING] [AddReportDefinitionTable.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Migrations/AddReportDefinitionTable.cs)

Create `ReportDefinition` table with columns:
- `Id`, `Name`, `Description`, `Type`, `ExecutionMode`
- `MetadataJson`, `OutputFormat`, `ParametersJson`
- `CreatedBy`, `CreatedAt`, `UpdatedAt`

> **Note**: Requires DbContext configuration before migration can be created

#### ⏳ [PENDING] [AddJobInstanceTable.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Migrations/AddJobInstanceTable.cs)

Create `JobInstance` table with columns:
- `JobId`, `ReportDefinitionId`, `UserId`, `Status`
- `Progress`, `RowsProcessed`, `TotalRows`
- `StartedAt`, `CompletedAt`, `EstimatedCompletion`
- `DownloadUrl`, `ErrorMessage`

> **Note**: Requires DbContext configuration before migration can be created

---

## Verification Plan

### Automated Tests

#### Unit Tests

**Test File**: [DataExecutionEngineTests.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/tests/Platform.Engine.Tests/Services/DataExecutionEngineTests.cs)

Tests to create:
1. `ExecuteAsync_WithEntityProvider_ReturnsData()` - Verify entity query execution
2. `ExecuteAsync_WithQuickJob_EnforcesTimeout()` - Verify 30s timeout
3. `ExecuteAsync_WithLongRunningJob_QueuesJob()` - Verify job queueing
4. `ExecuteAsync_WithInvalidMetadata_ThrowsValidationException()` - Verify validation

**Test File**: [DynamicQueryBuilderTests.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/tests/Platform.Engine.Tests/Services/DynamicQueryBuilderTests.cs)

Tests to create:
1. `BuildQuery_SimpleFilter_GeneratesCorrectLinq()` - Verify filter translation
2. `BuildQuery_WithJoin_IncludesRelatedEntity()` - Verify join handling
3. `BuildQuery_WithAggregation_GeneratesGroupBy()` - Verify aggregation
4. `BuildQuery_WithNestedAndOr_GeneratesCorrectExpression()` - Verify complex filters

**Test File**: [EntityDataProviderTests.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/tests/Platform.Engine.Tests/Services/DataProviders/EntityDataProviderTests.cs)

Tests to create:
1. `ExecuteAsync_AppliesRlsFilters()` - Verify RLS injection
2. `ExecuteAsync_WithCircularJoin_ThrowsException()` - Verify circular join detection
3. `EstimateRowCountAsync_ReturnsAccurateEstimate()` - Verify row count estimation

**Run Command**:
```bash
dotnet test tests/Platform.Engine.Tests/Platform.Engine.Tests.csproj --filter "FullyQualifiedName~DataExecution|DynamicQueryBuilder|EntityDataProvider"
```

---

#### Integration Tests

**Test File**: [DataExecutionIntegrationTests.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/tests/Platform.Integration.Tests/DataExecutionIntegrationTests.cs)

Tests to create:
1. `Execute_SimplePatientQuery_ReturnsResults()` - End-to-end entity query
2. `Execute_AggregationQuery_ReturnsGroupedData()` - End-to-end aggregation
3. `Execute_LongRunningJob_CompletesSuccessfully()` - End-to-end async job
4. `Execute_ExcelOutput_GeneratesValidFile()` - Verify Excel generation
5. `Execute_PdfOutput_GeneratesValidFile()` - Verify PDF generation

**Run Command**:
```bash
dotnet test tests/Platform.Integration.Tests/Platform.Integration.Tests.csproj --filter "FullyQualifiedName~DataExecution"
```

---

### Manual Verification

#### Test Scenario 1: Simple Entity Query via Visual Designer

**Steps**:
1. Navigate to Visual Data Designer in Platform Studio
2. Select "Patient" entity from Entity Picker
3. Select fields: FirstName, LastName, Email
4. Add filter: Status == "Active"
5. Click "Preview" button
6. Verify: Preview panel shows up to 10 patient records
7. Click "Execute" button with "Quick Job" mode
8. Verify: Full result set returned (up to 10,000 rows)

**Expected Result**: Patient data displayed with correct filtering

---

#### Test Scenario 2: Aggregation Report with Excel Export

**Steps**:
1. Open Visual Data Designer
2. Select "SalesOrder" entity
3. Add aggregation: Sum(TotalAmount) as Revenue
4. Add aggregation: Count(*) as OrderCount
5. Group by: OrderDate.Month
6. Add filter: OrderDate >= "2024-01-01"
7. Select output format: Excel
8. Click "Execute" as Quick Job
9. Verify: Excel file downloads automatically
10. Open Excel file
11. Verify: Contains columns "Month", "Revenue", "OrderCount" with correct data

**Expected Result**: Valid Excel file with aggregated sales data

---

#### Test Scenario 3: Long-Running Job with Status Tracking

**Steps**:
1. Open Visual Data Designer
2. Create a query that will take > 30 seconds (e.g., large date range)
3. Select "Long-Running" execution mode
4. Select output format: PDF
5. Click "Execute"
6. Verify: Job queued message appears with Job ID
7. Navigate to "Job Status" panel
8. Verify: Job appears with status "Queued" or "Running"
9. Verify: Progress bar updates periodically
10. Wait for job completion
11. Verify: Status changes to "Completed"
12. Click "Download" button
13. Verify: PDF file downloads with correct data

**Expected Result**: Job executes asynchronously with proper status tracking

---

#### Test Scenario 4: Join with Related Entities

**Steps**:
1. Open Visual Data Designer
2. Select "Appointment" entity
3. Add join: Appointment → Patient (Inner Join)
4. Add join: Appointment → Doctor (Inner Join)
5. Select fields: AppointmentDate, Status, Patient.FirstName, Patient.LastName, Doctor.Name
6. Click "Preview"
7. Verify: Preview shows appointments with patient and doctor names
8. Verify: No duplicate columns or missing data

**Expected Result**: Joined data displays correctly with related entity fields

---

#### Test Scenario 5: Filter Builder with AND/OR Groups

**Steps**:
1. Open Visual Data Designer
2. Select "SalesOrder" entity
3. Open Filter Builder
4. Create filter group with OR operator:
   - Region == "North"
   - Region == "South"
5. Add AND condition to root:
   - TotalAmount > 1000
6. Click "Preview"
7. Verify: Only orders from North or South with amount > 1000 appear

**Expected Result**: Complex nested filters work correctly

---

## Implementation Phases

### Phase 1: Backend Foundation (Week 1-2) ✅ CORE COMPLETE
- ✅ Create metadata models and interfaces
- ✅ Implement `EntityDataProvider` with basic query support
- ✅ Implement `DynamicQueryBuilder`
- ⏳ Create database migrations (requires DbContext setup)
- ⏳ Write unit tests for query builder

### Phase 2: Execution Engine (Week 3) ✅ COMPLETE
- ✅ Implement `DataExecutionEngine`
- ✅ Add Quick Job execution with timeout
- ✅ Implement validation logic
- ⏳ Write unit tests for execution engine

### Phase 3: Frontend Visual Designer (Week 4-5) ⏳ PENDING
- ⏳ Build Entity Picker component
- ⏳ Build Field Selector component
- ⏳ Build Filter Builder component
- ⏳ Implement metadata generation from UI
- ⏳ Add preview functionality

### Phase 4: Advanced Query Features (Week 6) ⏳ PENDING
- ⏳ Add Join Mapper component
- ⏳ Implement aggregation support
- ⏳ Add calculated fields support
- ⏳ Implement Union operations

### Phase 5: Output Generators (Week 7) 🔄 PARTIAL
- ⏳ Implement Excel generator
- ⏳ Implement PDF generator
- ✅ Implement CSV generator
- ⏳ Add output format selection to UI

### Phase 6: Long-Running Jobs with Elsa Workflow (Week 8) ⏳ PENDING
- ⏳ Create custom Elsa activities (ExecuteDataQuery, GenerateReportOutput, UploadToStorage, NotifyUser)
- ⏳ Implement `LongRunningReportWorkflow` definition
- ⏳ Implement `JobTrackingService` with Elsa integration
- ⏳ Update `DataExecutionEngine` to start Elsa workflows
- ⏳ Add job status tracking UI with Elsa workflow instance monitoring
- ⏳ Configure Elsa Studio for workflow monitoring

> **Reference**: See [Elsa Workflow Integration Guide](file:///C:/Users/sudip/.gemini/antigravity/brain/3ae47175-87f6-46fe-afca-4770733702d6/elsa_workflow_integration_guide.md) for detailed implementation instructions

### Phase 7: Additional Providers (Week 9) 🔄 PARTIAL
- ⏳ Implement `ApiDataProvider`
- ⏳ Implement `WorkflowDataProvider`
- ✅ Implement `StaticDataProvider`
- ⏳ Add provider selection to UI

### Phase 8: Edge Cases & Polish (Week 10) 🔄 PARTIAL
- ✅ Implement circular join detection
- ⏳ Add schema validation
- ✅ Implement RLS auto-injection
- ⏳ Performance testing and optimization
- ⏳ Write integration tests

---

## Questions for User

1. **Excel Library**: Do you have a commercial license for **EPPlus**, or should we use **ClosedXML** (free, MIT license)?

2. **Blob Storage**: For long-running job outputs, which storage provider should we use: **Azure Blob Storage**, **AWS S3**, or **MinIO** (self-hosted)?

3. **Authentication for External APIs**: Should we create a separate "Auth Profile" management UI, or is it acceptable to configure API keys in appsettings.json for now?

4. **Testing Data**: Do you have sample data in your development database (Patients, Appointments, Orders) that we can use for testing, or should we create seed data?

5. **Elsa Workflow Configuration**: Are there any specific Elsa workflow settings or persistence configurations we should be aware of for long-running jobs?

