# Delta Management & Versioning Implementation Plan

## Overview
Status: ⏳ PENDING
Priority: High
Owner: Antigravity

This plan outlines the implementation of the **Delta Detection Engine** and **Versioning Strategy** as defined in `Docs/DELTA_MANAGEMENT_AND_VERSIONING.md`. It ensures that changes to low-code metadata (Entities, Fields, Workflows) are tracked via GUIDs and applied safely to the database without data loss.

---

## Task List

### Phase 1: Metadata Foundation ✅ COMPLETE
- [x] **Task 1.1: GUID-Based Identity**
  - Update `EngineModels.cs` to include `Guid Id` for all metadata elements (`EntityMetadata`, `FieldMetadata`, `RelationMetadata`, `EnumValue`).
  - Ensure the Frontend/API generates these GUIDs on creation.
- [x] **Task 1.2: Snapshot Storage**
  - Create `ProjectSnapshot` entity in `Platform.Core`.
  - Add `Snapshots` DbSet to `PlatformDbContext`.
  - Create migration to update the database. (Note: Migration `AddProjectSnapshots` created, pending DB connection to update).
- [x] **Task 1.3: Versioning Service**
  - Implement `IVersioningService` to create immutable snapshots from current metadata.
  - Implemented `GenericRepository<T>` to support snapshots.

### Phase 2: Delta Detection Engine ✅ COMPLETE
- [x] **Task 2.1: Diff Analyzer**
  - Implement `IMetadataDiffService`.
  - Support detection of: `AddEntity`, `RemoveEntity`, `RenameEntity` (via GUID), `AddField`, `RemoveField`, `ChangeDataType`.
- [x] **Task 2.2: Migration Plan Generator**
  - Create `MigrationPlan` model to report proposed changes back to the user.

### Phase 3: Safe Deployment Engine ✅ COMPLETE
- [x] **Task 3.1: SQL Evolution Service**
  - Implement `ISqlSchemaEvolutionService` to generate DDL (Data Definition Language) for additive and destructive changes.
  - Implement "Safe Delete" (renaming to `_deprecated_`).
- [x] **Task 3.2: Publication Flow**
  - Update `ArtifactController` or create `PublishController` to execute the migration plan.
  - Snapshot integration for point-in-time recovery.

---

## Next Steps
1.  **Modify `src/Platform.Engine/Models/EngineModels.cs`** to add GUIDs.
2.  **Implement `ProjectSnapshot` entity.**
