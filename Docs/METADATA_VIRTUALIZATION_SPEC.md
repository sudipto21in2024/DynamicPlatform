# Specification: Metadata Virtualization & Compatibility Middleware

## Overview
The **Metadata Virtualization Middleware** is a transparent translation layer designed to decouple the physical database schema (which evolves via Delta Management) from the logical rules, workflows, and UI components. It ensures that renames, moves, and type changes do not break existing business logic.

---

## 1. Architectural Components

### A. The Compatibility Registry (`ICompatibilityRegistry`)
A persistent cache of all historical identity mappings.
- **Store**: Uses the `MigrationPlan` history stored in the `ProjectSnapshots`.
- **Primary Key**: `ElementId` (GUID).
- **Secondary Index**: `LegacyName` -> `CurrentName`.
- **Responsibility**: Resolve what "ConsultationFee" means *today* for a specific project.

### B. Normalization Middleware (`IMetadataNormalizationService`)
A recursive processor that sits at the entry point of the `DataExecutionEngine`.
- **Inbound Transformation**: Translates `DataOperationMetadata` (fields, filters, joins) from legacy names to current physical names.
- **Outbound Transformation**: (Optional) Injects "Shadow Properties" into the resulting dataset so legacy-bound UI can still see the old field names.

---

## 2. Implementation Workflow

### Inbound Phase (Query Translation)
1. **Identify Project Context**: Retrieve the published migration history for the project.
2. **Recursive Traversal**: 
    - Check `RootEntity`.
    - Check `Fields[]`.
    - Deep-scan `FilterGroups` (Conditions and nested Subqueries).
    - Check `JoinDefinition.Entity` and `JoinDefinition.On`.
3. **Rewrite**: If a match is found in the legacy name pool, swap the name with the `CurrentName` linked to that GUID.

### Outbound Phase (Result Virtualization)
1. **Detect Discrepancies**: Note fields that were renamed in the Inbound phase.
2. **Inject Values**: For every row returned by the database, copy the value of `Charge` into a new dynamic property `ConsultationFee`.

---

## 3. Edge Cases & Hazard Handling

### 🚩 Edge Case 1: The "Name Swap" Hazard
**Scenario**: User renames `Field_A` to `Field_B`, and `Field_B` to `Field_A` in the same version.
- **Solution**: The middleware **must not** use name-based matching. It must use **GUID Identity**. By resolving `GUID(A) -> CurrentName` and `GUID(B) -> CurrentName`, swap conflicts are avoided.

### 🚩 Edge Case 2: Deeply Nested Subqueries
**Scenario**: A Filter condition contains a Subquery that references a renamed entity three levels deep.
- **Solution**: The `NormalizationService` must implement the **Visitor Pattern** to ensure every node in the recursive `FilterGroup` tree is visited and translated.

### 🚩 Edge Case 3: Circular Relationship Renames
**Scenario**: `Doctor.PatientId` is renamed to `Practitioner.ClientId`.
- **Solution**: Both the Entity name and the Foreign Key name must be resolved simultaneously. The `On` clause in a join (`Doctor.Id == Patient.DoctorId`) must be parsed as a token stream and updated.

### 🚩 Edge Case 4: Property Shadowing
**Scenario**: A legacy field `Status` was renamed to `State`. Later, a *new* field `Status` is added to the same entity.
- **Solution**: The **Versioning Sequence** matters. If a legacy rule asks for `Status`, and a new field `Status` exists:
    - If the rule was created *before* the rename, it should map to `State`.
    - If the rule was created *after* the rename, it should map to the new `Status`.
    - **Implementation**: The middleware should use the `CreatedAt` timestamp of the Rule vs. the `PublishedAt` timestamp of the Migration to decide the mapping.

### 🚩 Edge Case 5: Type Cast Hazards
**Scenario**: `Price` (decimal) is renamed and changed to `PriceString` (string).
- **Solution**: The middleware must provide a **Virtual Getter** that attempts to cast the value back to the expected legacy type during virtualization to prevent runtime exceptions in older UI components.

---

## 4. Implementation Steps
1. [ ] Create `ICompatibilityProvider` to aggregate metadata GUID history.
2. [ ] Implement `MetadataNormalizationService` with recursive tree walking.
3. [ ] Hook into `DataExecutionEngine.ExecuteAsync`.
4. [ ] Create E2E test verifying "Legacy Query" returns "Physical Data" without the caller knowing the schema changed.
