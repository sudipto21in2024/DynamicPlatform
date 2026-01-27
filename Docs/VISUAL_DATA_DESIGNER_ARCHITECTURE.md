# Visual Data Designer: Detailed Architecture & Generic Wrapper API

## 1. Executive Summary

The **Visual Data Designer** is a low-code/no-code interface that enables users to build complex data queries, transformations, and aggregations without writing code. It serves as the foundation for:
- Report generation (Quick & Long-Running jobs)
- Widget data binding
- Dashboard data sources
- Custom data exports

This document provides a comprehensive plan including:
- Core scenarios and use cases
- Edge case handling strategies
- Generic Wrapper API architecture for unified data operations
- Metadata structures and execution engine design

---

## 2. Core Scenarios & Use Cases

### 2.1. Simple Entity Query
**Scenario**: User wants to display all active patients

**Visual Designer Steps**:
1. Select root entity: `Patient`
2. Add filter: `Status == 'Active'`
3. Select fields: `FirstName`, `LastName`, `DateOfBirth`
4. Set limit: 100

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "Patient",
  "fields": ["FirstName", "LastName", "DateOfBirth"],
  "filters": {
    "operator": "AND",
    "conditions": [
      { "field": "Status", "operator": "Equals", "value": "Active" }
    ]
  },
  "limit": 100,
  "offset": 0
}
```

**Expected Output**: List of patient records

---

### 2.2. Join with Related Entities
**Scenario**: User wants appointments with patient and doctor names

**Visual Designer Steps**:
1. Select root entity: `Appointment`
2. Add join: `Appointment.Patient` → Include `Patient.FirstName`, `Patient.LastName`
3. Add join: `Appointment.Doctor` → Include `Doctor.Name`
4. Select fields: `AppointmentDate`, `Status`
5. Add filter: `AppointmentDate >= Today()`

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "Appointment",
  "fields": [
    "AppointmentDate",
    "Status",
    "Patient.FirstName",
    "Patient.LastName",
    "Doctor.Name"
  ],
  "joins": [
    {
      "entity": "Patient",
      "type": "Inner",
      "on": "Appointment.PatientId == Patient.Id"
    },
    {
      "entity": "Doctor",
      "type": "Inner",
      "on": "Appointment.DoctorId == Doctor.Id"
    }
  ],
  "filters": {
    "operator": "AND",
    "conditions": [
      { "field": "AppointmentDate", "operator": "GreaterThanOrEqual", "value": "{{Today()}}" }
    ]
  }
}
```

---

### 2.3. Aggregation & Grouping
**Scenario**: User wants total revenue by month

**Visual Designer Steps**:
1. Select root entity: `SalesOrder`
2. Add aggregation: `Sum(TotalAmount)` as `Revenue`
3. Group by: `OrderDate.Month`, `OrderDate.Year`
4. Add filter: `OrderDate >= '2024-01-01'`
5. Sort by: `OrderDate.Year DESC`, `OrderDate.Month DESC`

**Generated Metadata**:
```json
{
  "operationType": "Aggregate",
  "rootEntity": "SalesOrder",
  "aggregations": [
    {
      "function": "Sum",
      "field": "TotalAmount",
      "alias": "Revenue"
    },
    {
      "function": "Count",
      "field": "*",
      "alias": "OrderCount"
    }
  ],
  "groupBy": [
    { "field": "OrderDate", "datepart": "Year", "alias": "Year" },
    { "field": "OrderDate", "datepart": "Month", "alias": "Month" }
  ],
  "filters": {
    "operator": "AND",
    "conditions": [
      { "field": "OrderDate", "operator": "GreaterThanOrEqual", "value": "2024-01-01" }
    ]
  },
  "orderBy": [
    { "field": "Year", "direction": "DESC" },
    { "field": "Month", "direction": "DESC" }
  ]
}
```

---

### 2.4. Complex Filtering (Nested AND/OR)
**Scenario**: User wants high-value orders from specific regions

**Filter Logic**: `(Region == 'North' OR Region == 'South') AND TotalAmount > 1000`

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "SalesOrder",
  "filters": {
    "operator": "AND",
    "conditions": [
      {
        "operator": "OR",
        "conditions": [
          { "field": "Region", "operator": "Equals", "value": "North" },
          { "field": "Region", "operator": "Equals", "value": "South" }
        ]
      },
      { "field": "TotalAmount", "operator": "GreaterThan", "value": 1000 }
    ]
  }
}
```

---

### 2.5. Calculated Fields
**Scenario**: User wants to calculate profit margin

**Visual Designer Steps**:
1. Select root entity: `Product`
2. Add calculated field: `(SellingPrice - CostPrice) / SellingPrice * 100` as `ProfitMargin`
3. Add filter: `ProfitMargin < 20` (low margin products)

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "Product",
  "fields": ["Name", "SellingPrice", "CostPrice"],
  "calculatedFields": [
    {
      "alias": "ProfitMargin",
      "expression": "((SellingPrice - CostPrice) / SellingPrice) * 100",
      "dataType": "Decimal"
    }
  ],
  "filters": {
    "operator": "AND",
    "conditions": [
      { "field": "ProfitMargin", "operator": "LessThan", "value": 20 }
    ]
  }
}
```

---

### 2.6. Multi-Entity Union
**Scenario**: User wants combined list of all contacts (Patients + Doctors)

**Visual Designer Steps**:
1. Create Query A: Select `Patient` → Fields: `FirstName`, `LastName`, `Email`, `'Patient' as Type`
2. Create Query B: Select `Doctor` → Fields: `Name as FirstName`, `'' as LastName`, `Email`, `'Doctor' as Type`
3. Union: Query A UNION Query B

**Generated Metadata**:
```json
{
  "operationType": "Union",
  "queries": [
    {
      "rootEntity": "Patient",
      "fields": [
        { "field": "FirstName", "alias": "FirstName" },
        { "field": "LastName", "alias": "LastName" },
        { "field": "Email", "alias": "Email" },
        { "value": "Patient", "alias": "Type" }
      ]
    },
    {
      "rootEntity": "Doctor",
      "fields": [
        { "field": "Name", "alias": "FirstName" },
        { "value": "", "alias": "LastName" },
        { "field": "Email", "alias": "Email" },
        { "value": "Doctor", "alias": "Type" }
      ]
    }
  ],
  "unionType": "All"
}
```

---

### 2.7. Subquery / Exists Condition
**Scenario**: User wants patients who have appointments in the last 30 days

**Visual Designer Steps**:
1. Select root entity: `Patient`
2. Add subquery filter: `EXISTS (Appointment WHERE PatientId == Patient.Id AND AppointmentDate >= Today() - 30)`

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "Patient",
  "filters": {
    "operator": "AND",
    "conditions": [
      {
        "type": "Subquery",
        "operator": "Exists",
        "subquery": {
          "rootEntity": "Appointment",
          "filters": {
            "operator": "AND",
            "conditions": [
              { "field": "PatientId", "operator": "Equals", "value": "{{Parent.Id}}" },
              { "field": "AppointmentDate", "operator": "GreaterThanOrEqual", "value": "{{Today(-30)}}" }
            ]
          }
        }
      }
    ]
  }
}
```

---

### 2.8. Pivot / Cross-Tab
**Scenario**: User wants sales by product category and month

**Visual Designer Steps**:
1. Select root entity: `SalesOrder`
2. Pivot: Rows = `Product.Category`, Columns = `OrderDate.Month`, Values = `Sum(TotalAmount)`

**Generated Metadata**:
```json
{
  "operationType": "Pivot",
  "rootEntity": "SalesOrder",
  "rowFields": ["Product.Category"],
  "columnField": {
    "field": "OrderDate",
    "datepart": "Month"
  },
  "valueField": {
    "function": "Sum",
    "field": "TotalAmount"
  }
}
```

---

### 2.9. Window Functions (Ranking)
**Scenario**: User wants top 5 products by sales in each category

**Visual Designer Steps**:
1. Select root entity: `Product`
2. Add window function: `RANK() OVER (PARTITION BY Category ORDER BY TotalSales DESC)` as `Rank`
3. Add filter: `Rank <= 5`

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "Product",
  "windowFunctions": [
    {
      "function": "Rank",
      "partitionBy": ["Category"],
      "orderBy": [{ "field": "TotalSales", "direction": "DESC" }],
      "alias": "Rank"
    }
  ],
  "filters": {
    "operator": "AND",
    "conditions": [
      { "field": "Rank", "operator": "LessThanOrEqual", "value": 5 }
    ]
  }
}
```

---

### 2.10. External API Integration
**Scenario**: User wants to enrich patient data with external insurance verification

**Visual Designer Steps**:
1. Select root entity: `Patient`
2. Add external data source: API call to `https://insurance-api.com/verify`
3. Map fields: `Patient.InsuranceId` → API parameter
4. Merge result: API response `isActive` → `InsuranceStatus`

**Generated Metadata**:
```json
{
  "operationType": "Query",
  "rootEntity": "Patient",
  "externalSources": [
    {
      "type": "API",
      "endpoint": "https://insurance-api.com/verify",
      "method": "POST",
      "authProfile": "InsuranceAPI_Key",
      "requestMapping": {
        "insuranceId": "{{Patient.InsuranceId}}"
      },
      "responseMapping": {
        "isActive": "InsuranceStatus"
      }
    }
  ]
}
```

---

## 3. Edge Cases & Error Handling

### 3.1. Circular Join Detection
**Problem**: User creates `Order → Customer → Order` (infinite loop)

**Detection Strategy**:
- Track join path during visual builder interaction
- Prevent adding a join that creates a cycle
- Display warning: "Circular reference detected: Order is already in the join path"

**Implementation**:
```typescript
function detectCircularJoin(currentPath: string[], newEntity: string): boolean {
  return currentPath.includes(newEntity);
}
```

---

### 3.2. Schema Mismatch (Field Renamed/Deleted)
**Problem**: Report uses `Patient.SSN` but field was renamed to `Patient.NationalId`

**Detection Strategy**:
- Validate metadata against current entity schema before execution
- Return validation errors with field mapping suggestions

**Error Response**:
```json
{
  "status": "ValidationError",
  "errors": [
    {
      "field": "Patient.SSN",
      "message": "Field 'SSN' does not exist in entity 'Patient'",
      "suggestions": ["NationalId", "TaxId"]
    }
  ]
}
```

**Mitigation**:
- Store schema version with report definition
- Provide migration tool to update field references

---

### 3.3. Performance - Cartesian Product
**Problem**: User joins `Order` (1M rows) with `Product` (10K rows) without proper join condition

**Detection Strategy**:
- Analyze join metadata for missing `ON` conditions
- Estimate result set size before execution
- Block execution if estimated rows > threshold (e.g., 10M)

**Warning Message**:
```
"This query may produce a very large result set (estimated 10 billion rows). 
Please add a join condition between Order and Product."
```

---

### 3.4. Data Volume Overflow
**Problem**: User requests 10M rows for a Quick Job (synchronous)

**Handling Strategy**:
- **Quick Jobs**: Hard limit of 10,000 rows
- **Long-Running Jobs**: Use streaming/chunked processing
- Display warning if result exceeds limit

**Implementation**:
```csharp
if (executionMode == "Quick" && estimatedRows > 10000) {
    throw new InvalidOperationException(
        "Result set too large for Quick Job. Use Long-Running mode or add filters."
    );
}
```

---

### 3.5. Null Handling in Aggregations
**Problem**: `Sum(TotalAmount)` where some rows have `NULL` values

**Strategy**:
- Default behavior: Ignore NULLs (SQL standard)
- Provide option: "Treat NULL as Zero"
- Display warning if significant NULL percentage detected

**Metadata Option**:
```json
{
  "aggregations": [
    {
      "function": "Sum",
      "field": "TotalAmount",
      "nullHandling": "IgnoreNulls" // or "TreatAsZero"
    }
  ]
}
```

---

### 3.6. Date/Time Zone Issues
**Problem**: User filters `AppointmentDate > '2024-01-01'` but server is in different timezone

**Strategy**:
- Store user's timezone in session
- Convert all date literals to UTC before query execution
- Return dates in user's timezone

**Metadata Enhancement**:
```json
{
  "executionContext": {
    "userTimezone": "America/New_York",
    "serverTimezone": "UTC"
  }
}
```

---

### 3.7. Division by Zero
**Problem**: Calculated field `Revenue / OrderCount` where `OrderCount = 0`

**Strategy**:
- Wrap division in NULLIF or CASE statement
- Return NULL instead of error

**Generated SQL**:
```sql
SELECT 
  CASE WHEN OrderCount = 0 THEN NULL ELSE Revenue / OrderCount END as AvgOrderValue
```

---

### 3.8. Security - Row Level Security (RLS)
**Problem**: User tries to query data outside their tenant/permissions

**Strategy**:
- Automatically inject RLS filters based on user context
- Apply filters at query generation time (not post-processing)

**Auto-Injected Filter**:
```json
{
  "filters": {
    "operator": "AND",
    "conditions": [
      { "field": "TenantId", "operator": "Equals", "value": "{{CurrentUser.TenantId}}" },
      // ... user-defined filters
    ]
  }
}
```

---

### 3.9. Parameter Injection Attack
**Problem**: User enters `'; DROP TABLE Patient; --` in a parameter

**Strategy**:
- Use parameterized queries (never string concatenation)
- Validate parameter types before execution
- Sanitize all user inputs

**Implementation**:
```csharp
// GOOD: Parameterized
query.Where(p => p.Name == @name);

// BAD: String concatenation
query.Where($"Name = '{name}'"); // NEVER DO THIS
```

---

### 3.10. Timeout Handling
**Problem**: Query takes > 30 seconds for Quick Job

**Strategy**:
- Set strict timeout for Quick Jobs (30s)
- Suggest converting to Long-Running Job
- Provide query optimization hints

**Error Response**:
```json
{
  "status": "Timeout",
  "message": "Query exceeded 30 second limit",
  "suggestion": "Convert to Long-Running Job or add filters to reduce data volume",
  "estimatedDuration": "2 minutes"
}
```

---

## 4. Generic Wrapper API Architecture

### 4.1. API Design Philosophy

The Generic Wrapper API provides a **unified interface** for all data operations, abstracting the complexity of:
- Entity queries
- Joins and relationships
- Aggregations
- External API calls
- Workflow execution
- Static data

**Key Principles**:
- Single endpoint for all operations
- Metadata-driven execution
- Provider-agnostic design
- Consistent error handling
- Performance optimization

---

### 4.2. Core API Endpoint

#### **POST /api/data/execute**

**Request Body**:
```json
{
  "provider": "Entity | API | Workflow | Static | Custom",
  "operation": "Query | Aggregate | Union | Pivot | Execute",
  "metadata": {
    // Provider-specific metadata (see scenarios above)
  },
  "executionMode": "Quick | LongRunning",
  "outputFormat": "JSON | Excel | PDF | CSV",
  "parameters": {
    // Runtime parameters
    "startDate": "2024-01-01",
    "region": "North"
  },
  "context": {
    "userId": "guid",
    "tenantId": "guid",
    "timezone": "America/New_York"
  }
}
```

**Response (Quick Job)**:
```json
{
  "status": "Success",
  "executionTime": "1.2s",
  "rowCount": 150,
  "data": [
    { "id": 1, "name": "John Doe", "email": "john@example.com" },
    // ... more rows
  ],
  "metadata": {
    "columns": [
      { "name": "id", "type": "number" },
      { "name": "name", "type": "string" },
      { "name": "email", "type": "string" }
    ]
  }
}
```

**Response (Long-Running Job)**:
```json
{
  "status": "Queued",
  "jobId": "job-guid-123",
  "estimatedDuration": "5 minutes",
  "statusUrl": "/api/data/jobs/job-guid-123/status"
}
```

---

### 4.3. Provider Handlers

Each provider implements the `IDataProvider` interface:

```csharp
public interface IDataProvider
{
    string ProviderType { get; } // "Entity", "API", "Workflow", etc.
    
    Task<DataResult> ExecuteAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        ExecutionContext context,
        CancellationToken cancellationToken
    );
    
    Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata);
    
    Task<long> EstimateRowCountAsync(DataOperationMetadata metadata);
}
```

#### 4.3.1. Entity Provider
**Responsibility**: Execute queries against platform entities

**Implementation**:
```csharp
public class EntityDataProvider : IDataProvider
{
    private readonly IGenericRepository _repository;
    private readonly IQueryBuilder _queryBuilder;
    
    public async Task<DataResult> ExecuteAsync(...)
    {
        // 1. Parse metadata to LINQ expression
        var query = _queryBuilder.BuildQuery(metadata);
        
        // 2. Apply RLS filters
        query = ApplySecurityFilters(query, context);
        
        // 3. Execute with timeout
        var data = await query.ToListAsync(cancellationToken);
        
        return new DataResult { Data = data, RowCount = data.Count };
    }
}
```

#### 4.3.2. API Provider
**Responsibility**: Fetch data from external APIs

**Implementation**:
```csharp
public class ApiDataProvider : IDataProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthProfileManager _authManager;
    
    public async Task<DataResult> ExecuteAsync(...)
    {
        // 1. Build HTTP request
        var request = BuildRequest(metadata, parameters);
        
        // 2. Apply authentication
        await _authManager.ApplyAuthAsync(request, metadata.AuthProfile);
        
        // 3. Execute with retry policy
        var response = await ExecuteWithRetryAsync(request, cancellationToken);
        
        // 4. Parse and map response
        var data = await ParseResponseAsync(response, metadata.ResponseMapping);
        
        return new DataResult { Data = data };
    }
}
```

#### 4.3.3. Workflow Provider
**Responsibility**: Execute workflow and return result

**Implementation**:
```csharp
public class WorkflowDataProvider : IDataProvider
{
    private readonly IWorkflowEngine _workflowEngine;
    
    public async Task<DataResult> ExecuteAsync(...)
    {
        // 1. Resolve workflow by name
        var workflow = await _workflowEngine.GetWorkflowAsync(metadata.Source);
        
        // 2. Execute with parameters
        var result = await _workflowEngine.ExecuteAsync(
            workflow, 
            parameters, 
            context,
            cancellationToken
        );
        
        return new DataResult { Data = result.Output };
    }
}
```

#### 4.3.4. Static Provider
**Responsibility**: Return static/mock data

**Implementation**:
```csharp
public class StaticDataProvider : IDataProvider
{
    public Task<DataResult> ExecuteAsync(...)
    {
        // Simply return the parameters as data
        return Task.FromResult(new DataResult { Data = parameters });
    }
}
```

---

### 4.4. Query Builder Architecture

The Query Builder translates metadata into executable queries:

```csharp
public interface IQueryBuilder
{
    IQueryable<object> BuildQuery(DataOperationMetadata metadata);
}

public class DynamicQueryBuilder : IQueryBuilder
{
    public IQueryable<object> BuildQuery(DataOperationMetadata metadata)
    {
        // 1. Start with root entity
        var query = GetEntityQueryable(metadata.RootEntity);
        
        // 2. Apply joins
        foreach (var join in metadata.Joins ?? [])
        {
            query = ApplyJoin(query, join);
        }
        
        // 3. Apply filters
        if (metadata.Filters != null)
        {
            query = ApplyFilters(query, metadata.Filters);
        }
        
        // 4. Apply aggregations or select fields
        if (metadata.Aggregations != null)
        {
            query = ApplyAggregations(query, metadata);
        }
        else
        {
            query = ApplyFieldSelection(query, metadata.Fields);
        }
        
        // 5. Apply sorting
        query = ApplyOrdering(query, metadata.OrderBy);
        
        // 6. Apply pagination
        query = ApplyPagination(query, metadata.Limit, metadata.Offset);
        
        return query;
    }
}
```

---

### 4.5. Execution Engine

The Execution Engine orchestrates the entire process:

```csharp
public class DataExecutionEngine
{
    private readonly IEnumerable<IDataProvider> _providers;
    private readonly IJobQueueService _jobQueue;
    private readonly IOutputGenerator _outputGenerator;
    
    public async Task<object> ExecuteAsync(DataExecutionRequest request)
    {
        // 1. Select appropriate provider
        var provider = _providers.First(p => p.ProviderType == request.Provider);
        
        // 2. Validate metadata
        var validation = await provider.ValidateAsync(request.Metadata);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
        
        // 3. Determine execution mode
        if (request.ExecutionMode == "Quick")
        {
            return await ExecuteQuickJobAsync(provider, request);
        }
        else
        {
            return await QueueLongRunningJobAsync(provider, request);
        }
    }
    
    private async Task<DataResult> ExecuteQuickJobAsync(
        IDataProvider provider, 
        DataExecutionRequest request)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        try
        {
            var result = await provider.ExecuteAsync(
                request.Metadata,
                request.Parameters,
                request.Context,
                cts.Token
            );
            
            // Generate output format
            if (request.OutputFormat != "JSON")
            {
                result.Output = await _outputGenerator.GenerateAsync(
                    result.Data,
                    request.OutputFormat
                );
            }
            
            return result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Query exceeded 30 second limit");
        }
    }
    
    private async Task<JobQueueResult> QueueLongRunningJobAsync(
        IDataProvider provider,
        DataExecutionRequest request)
    {
        var jobId = Guid.NewGuid().ToString();
        
        await _jobQueue.EnqueueAsync(new JobDefinition
        {
            JobId = jobId,
            Provider = provider,
            Request = request,
            Status = "Queued",
            CreatedAt = DateTime.UtcNow
        });
        
        return new JobQueueResult
        {
            JobId = jobId,
            Status = "Queued",
            StatusUrl = $"/api/data/jobs/{jobId}/status"
        };
    }
}
```

---

### 4.6. Job Status API

#### **GET /api/data/jobs/{jobId}/status**

**Response**:
```json
{
  "jobId": "job-guid-123",
  "status": "Running | Completed | Failed | Queued",
  "progress": 65,
  "startedAt": "2024-01-27T10:00:00Z",
  "estimatedCompletion": "2024-01-27T10:05:00Z",
  "rowsProcessed": 650000,
  "totalRows": 1000000,
  "downloadUrl": null, // Available when status = "Completed"
  "error": null
}
```

---

### 4.7. Output Generators

Each output format has a dedicated generator:

```csharp
public interface IOutputGenerator
{
    string Format { get; } // "Excel", "PDF", "CSV"
    
    Task<Stream> GenerateAsync(
        IEnumerable<object> data,
        OutputOptions options
    );
}

public class ExcelOutputGenerator : IOutputGenerator
{
    public async Task<Stream> GenerateAsync(IEnumerable<object> data, OutputOptions options)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Data");
        
        // 1. Write headers
        var properties = data.First().GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = properties[i].Name;
        }
        
        // 2. Write data rows
        int row = 2;
        foreach (var item in data)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = properties[i].GetValue(item);
            }
            row++;
        }
        
        // 3. Apply styling
        ApplyStyling(worksheet, options);
        
        var stream = new MemoryStream();
        await package.SaveAsAsync(stream);
        stream.Position = 0;
        return stream;
    }
}
```

---

## 5. Metadata Schema Definitions

### 5.1. Root Metadata Structure

```typescript
interface DataOperationMetadata {
  operationType: 'Query' | 'Aggregate' | 'Union' | 'Pivot' | 'Execute';
  rootEntity?: string;
  fields?: FieldDefinition[];
  joins?: JoinDefinition[];
  filters?: FilterGroup;
  aggregations?: AggregationDefinition[];
  groupBy?: GroupByDefinition[];
  orderBy?: OrderByDefinition[];
  limit?: number;
  offset?: number;
  calculatedFields?: CalculatedFieldDefinition[];
  windowFunctions?: WindowFunctionDefinition[];
  externalSources?: ExternalSourceDefinition[];
}
```

### 5.2. Filter Definitions

```typescript
interface FilterGroup {
  operator: 'AND' | 'OR';
  conditions: (FilterCondition | FilterGroup)[];
}

interface FilterCondition {
  field: string;
  operator: 'Equals' | 'NotEquals' | 'GreaterThan' | 'LessThan' | 
            'GreaterThanOrEqual' | 'LessThanOrEqual' | 'Contains' | 
            'StartsWith' | 'EndsWith' | 'In' | 'NotIn' | 'IsNull' | 'IsNotNull';
  value: any;
  type?: 'Subquery';
  subquery?: DataOperationMetadata;
}
```

### 5.3. Join Definitions

```typescript
interface JoinDefinition {
  entity: string;
  type: 'Inner' | 'Left' | 'Right' | 'Full';
  on: string; // "Appointment.PatientId == Patient.Id"
  alias?: string;
}
```

### 5.4. Aggregation Definitions

```typescript
interface AggregationDefinition {
  function: 'Sum' | 'Count' | 'Average' | 'Min' | 'Max' | 'CountDistinct';
  field: string;
  alias: string;
  nullHandling?: 'IgnoreNulls' | 'TreatAsZero';
}
```

---

## 6. Visual Designer UI Components

### 6.1. Entity Picker
- Dropdown list of all available entities
- Search/filter capability
- Display entity description on hover

### 6.2. Field Selector
- Tree view of entity fields
- Support for related entity navigation (e.g., `Order.Customer.Name`)
- Checkbox selection for multiple fields
- Drag-to-reorder selected fields

### 6.3. Filter Builder
- Visual condition builder
- Support for AND/OR grouping
- Operator dropdown based on field type
- Value input with type validation
- Add/remove condition buttons

### 6.4. Join Mapper
- Visual relationship tree
- Automatic join suggestion based on foreign keys
- Join type selector (Inner/Left/Right)
- Custom join condition editor

### 6.5. Aggregation Panel
- Function selector (Sum, Count, Avg, etc.)
- Field selector for aggregation
- Group By field selector
- Having clause builder (filter on aggregated values)

### 6.6. Preview Panel
- Live data preview (limited to 10 rows)
- Column headers with data types
- Execution time display
- Row count indicator
- Refresh button

---

## 7. Implementation Phases

### Phase 1: Foundation (Weeks 1-2)
- [ ] Create metadata schema definitions
- [ ] Implement `IDataProvider` interface
- [ ] Build `EntityDataProvider` with basic query support
- [ ] Create `DataExecutionEngine`
- [ ] Implement Quick Job execution

### Phase 2: Visual Designer UI (Weeks 3-4)
- [ ] Build Entity Picker component
- [ ] Build Field Selector component
- [ ] Build Filter Builder component
- [ ] Build Join Mapper component
- [ ] Implement metadata generation from UI

### Phase 3: Advanced Query Features (Weeks 5-6)
- [ ] Implement aggregation support
- [ ] Add calculated fields
- [ ] Support for subqueries
- [ ] Implement Union operations
- [ ] Add window functions

### Phase 4: Long-Running Jobs (Week 7)
- [ ] Integrate background job queue (Hangfire)
- [ ] Implement chunked/streaming processing
- [ ] Build job status tracking
- [ ] Add notification system

### Phase 5: Output Generators (Week 8)
- [ ] Implement Excel generator (EPPlus/ClosedXML)
- [ ] Implement PDF generator (QuestPDF)
- [ ] Implement CSV generator
- [ ] Add output formatting options

### Phase 6: Additional Providers (Weeks 9-10)
- [ ] Implement API Provider
- [ ] Implement Workflow Provider
- [ ] Implement Static Provider
- [ ] Add provider validation

### Phase 7: Edge Cases & Optimization (Weeks 11-12)
- [ ] Implement circular join detection
- [ ] Add schema validation
- [ ] Implement RLS auto-injection
- [ ] Add query optimization hints
- [ ] Performance testing and tuning

---

## 8. API Usage Examples

### Example 1: Simple Patient Query

**Request**:
```json
POST /api/data/execute
{
  "provider": "Entity",
  "operation": "Query",
  "metadata": {
    "operationType": "Query",
    "rootEntity": "Patient",
    "fields": ["FirstName", "LastName", "Email"],
    "filters": {
      "operator": "AND",
      "conditions": [
        { "field": "Status", "operator": "Equals", "value": "Active" }
      ]
    },
    "limit": 100
  },
  "executionMode": "Quick",
  "outputFormat": "JSON"
}
```

### Example 2: Revenue Report (Long-Running)

**Request**:
```json
POST /api/data/execute
{
  "provider": "Entity",
  "operation": "Aggregate",
  "metadata": {
    "operationType": "Aggregate",
    "rootEntity": "SalesOrder",
    "aggregations": [
      { "function": "Sum", "field": "TotalAmount", "alias": "Revenue" },
      { "function": "Count", "field": "*", "alias": "OrderCount" }
    ],
    "groupBy": [
      { "field": "OrderDate", "datepart": "Month", "alias": "Month" }
    ],
    "filters": {
      "operator": "AND",
      "conditions": [
        { "field": "OrderDate", "operator": "GreaterThanOrEqual", "value": "2020-01-01" }
      ]
    }
  },
  "executionMode": "LongRunning",
  "outputFormat": "Excel",
  "parameters": {
    "startDate": "2020-01-01"
  }
}
```

**Response**:
```json
{
  "status": "Queued",
  "jobId": "job-abc-123",
  "estimatedDuration": "3 minutes",
  "statusUrl": "/api/data/jobs/job-abc-123/status"
}
```

### Example 3: External API Integration

**Request**:
```json
POST /api/data/execute
{
  "provider": "API",
  "operation": "Execute",
  "metadata": {
    "endpoint": "https://api.weather.com/current",
    "method": "GET",
    "authProfile": "WeatherAPI_Key",
    "responseMapping": {
      "temperature": "temp",
      "humidity": "humidity",
      "condition": "weather.description"
    }
  },
  "executionMode": "Quick",
  "outputFormat": "JSON",
  "parameters": {
    "city": "New York"
  }
}
```

---

## 9. Security Considerations

### 9.1. Authentication & Authorization
- All API calls require valid JWT token
- User permissions checked before query execution
- Entity-level access control

### 9.2. Row-Level Security (RLS)
- Automatic injection of tenant filters
- User-based data filtering
- Cannot be bypassed by metadata manipulation

### 9.3. Query Sandboxing
- Parameterized queries only (no SQL injection)
- Read-only database context for reports
- Resource limits (CPU, memory, timeout)

### 9.4. Audit Logging
- Log all data access operations
- Track user, timestamp, query metadata
- Monitor for suspicious patterns

---

## 10. Performance Optimization

### 10.1. Query Optimization
- Automatic index suggestions
- Query plan analysis
- Materialized view recommendations

### 10.2. Caching Strategy
- Cache frequently accessed metadata
- Cache entity schemas
- Cache small static datasets

### 10.3. Streaming & Chunking
- Process large datasets in chunks (1000 rows at a time)
- Stream output directly to file/blob storage
- Never load entire dataset into memory

### 10.4. Database Connection Pooling
- Reuse database connections
- Configure appropriate pool size
- Monitor connection usage

---

## 11. Summary

The Visual Data Designer provides a comprehensive, low-code solution for data operations with:

✅ **10+ Core Scenarios**: From simple queries to complex aggregations  
✅ **10+ Edge Cases**: Robust error handling and validation  
✅ **Generic Wrapper API**: Unified interface for all data providers  
✅ **Metadata-Driven**: Fully configurable without code changes  
✅ **Scalable**: Support for both quick and long-running operations  
✅ **Secure**: Built-in RLS, authentication, and audit logging  
✅ **Extensible**: Easy to add new providers and operations  

This architecture enables users to build 90%+ of reporting and data visualization needs without writing code, while providing escape hatches (Code Window) for complex scenarios.
