# Developer Guide: Metadata Virtualization & Compatibility Middleware

## 1. The Problem: The "Evolution Paradox"
In a typical low-code platform, if a developer renames an entity (e.g., `Patient` -> `Client`), all existing workflows, reports, and API calls that refer to `Patient` will immediately break. This creates "Transformation Debt" where schema improvements are avoided because they are too expensive to fix downstream.

## 2. Our Solution: Identity over Naming
We have implemented a **Identity-First Middleware** architecture. Instead of relying on string names, the platform uses persistent **GUIDs** to track elements across time.

### Key Components

#### A. `ICompatibilityProvider` (The Memory)
This service acts as the platform's historical memory. When a project is loaded:
1. It scans all **Published Project Snapshots**.
2. it builds a multi-way map:
   - `GUID -> Current Name`
   - `Historical Name -> GUID`
3. It allows the system to ask: *"What is the current name of the entity that used to be called 'Doctor'?"*

#### B. `IMetadataNormalizationService` (The Translator)
This is a recursive middleware that intercepts incoming `DataOperationMetadata` (the JSON structure that defines a query).
- **Inbound Transformation**: It walks through Fields, Filters, Joins, and Aggregations. If it encounters a legacy name, it transparently swaps it for the current physical name.
- **Example**: 
   - *Input*: `SELECT ConsultationFee FROM Doctor`
   - *Middleware Output*: `SELECT Charge FROM Practitioner`

#### C. Result Virtualization (Shadow Properties)
To ensure the UI/Frontend doesn't break, the middleware can inject "Shadow Properties" back into the result set.
- If the database returns a column called `Charge`, the middleware will clone that value into a property called `ConsultationFee` before sending it to the caller.

---

## 3. High-Level Logic Flow

1.  **Request**: A Legacy Report asks for data using v1.0 names.
2.  **Normalization Interface**: `DataExecutionEngine` calls `NormalizationService.NormalizeAsync()`.
3.  **GUID Resolution**: The `CompatibilityProvider` looks up the GUIDs for those legacy names and finds their v2.0 counterparts.
4.  **Execution**: The `IDataProvider` (SQL/Entity) executes the cleaned query against the real database schema.
5.  **Virtualization**: The `NormalizationService` injects legacy field names back into the output JSON.
6.  **Response**: The caller receives a response that looks exactly like they expected, despite the database having a different schema.

---

## 4. Why this is "Antigravity"
- **Zero Breaking Changes**: You can rename, move, and refactor your data schema 100 times, and your logic stays stable.
- **Decoupled Evolution**: The DB administrator can optimize the schema while the Business Analyst continues to use familiar logical names in the Studio.
- **Traceability**: Every mapping is backed by the `ProjectSnapshot` audit trail.

## 5. Usage in Code

### Registering the Service
```csharp
builder.Services.AddScoped<ICompatibilityProvider, CompatibilityProvider>();
builder.Services.AddScoped<IMetadataNormalizationService, MetadataNormalizationService>();
```

### Manually Normalizing a Query
```csharp
public async Task HandleRequest(DataOperationMetadata metadata, Guid projectId)
{
    await _normalizationService.NormalizeAsync(projectId, metadata);
    // metadata is now safe for database execution
}
```

---

## 6. Known Edge Cases Handled
- **Recursive Renames**: Handles cases where a Field and its Parent Entity were both renamed in different versions.
- **Collision Handling**: Uses timestamps to resolve name "Shadowing" (where a new field takes an old field's name).
- **Subqueries**: Deeply nested `UnionQueries` and `Subqueries` are recursively normalized.
