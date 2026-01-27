# Delta Detection, Versioning & Change Management Strategy

## 1. Executive Summary
In a metadata-driven low-code platform, "Change" is the only constant. As users modify entities, workflows, and logic, the platform must guarantee that these changes do not corrupt existing data or break in-flight processes. This document outlines the **Delta Detection Engine**, the **Versioning Strategy**, and the **Safe Deployment Protocol** required to handle application evolution robustly.

## 2. The Core Problem: Types of Change
We categorize changes into three severity levels based on their impact on the running system.

| Severity | Type | Examples | Impact Strategy |
| :--- | :--- | :--- | :--- |
| **Level 1** | **Non-Breaking (Hot-Swappable)** | UI Label change, new CSS class, adding a new validation rule, new permission role. | **Hot Reload**: Can be applied immediately without downtime. |
| **Level 2** | **Additive (Backward Compatible)** | Adding a new Entity, adding a nullable column, creating a new Workflow, adding a new Report. | **Rolling Update**: Database schema is expanded; old code ignores new fields. |
| **Level 3** | **Destructive (Breaking)** | Deleting a table/column, renaming a column (without ID tracking), changing data types (String -> Int), modifying a Workflow logic that is currently executing. | **Migration Required**: Requires downtime or Blue/Green deployment with data transformation scripts. |

## 3. Architecture Solution

### 3.1. Metadata Versioning System (The "Git" of Low-Code)
Every application is not just a single JSON file, but a **Time-Series of Snapshots**.
*   **Structure**: `AppId / Version / hash`
*   **Immutable Snapshots**: Once Version 1.0 is "Published", its metadata is locked. Any edit creates a Draft (v1.1-draft).
*   **GUID-Based Tracking**:
    *   *Critical*: We must use **GUIDs** (Persistent IDs) for all schema elements, not Names.
    *   *Scenario*: User renames column `Phone` to `Mobile`.
        *   If tracked by Name: System thinks "Delete Phone, Add Mobile" -> **DATA LOSS**.
        *   If tracked by GUID: System knows "Column {guid} name changed" -> **SAFE RENAME**.

### 3.2. The Delta Detection Engine (Diff Analyzer)
Before any deployment, the engine compares `Current_Prod_Version` vs `Candidate_Version`.

**Algorithm:**
1.  **Entity Diff**: Compare Field GUIDs, Types, and Constraints.
    *   *Output*: `[ { Action: "Alter", Entity: "User", Field: "Status", Change: "Type(Int->String)" } ]`
2.  **Logic Diff**: Compare Workflow Step revisions.
    *   *Output*: Identifies if modified steps have active instances.
3.  **Dependency Check**: Checks if deleted fields are referenced in *Reports, API Endpoints, or UI Widgets*.

### 3.3. Database Evolution Strategy (Schema Migrations)
The platform must act as a "DBA in a Box".
1.  **Additive Changes**: Auto-generated SQL (`ALTER TABLE ADD ...`).
2.  **Destructive Changes (Strict Mode)**:
    *   If a column is deleted in Metadata, the physical column is **NOT deleted immediately**.
    *   It is renamed to `_deprecated_ColName_v1`.
    *   **Reason**: Allows rollback if the user made a mistake. A background "Garbage Collector" job deletes these after X days.
3.  **Type Conversions**:
    *   If User changes `ZipCode` (Int) to `ZipCode` (String): Safe.
    *   If User changes `Notes` (String) to `Age` (Int): **Data Hazard**.
    *   **Requirement**: The UI must prompt the user for a **Transformation Expression** (e.g., `value => int.TryParse(value, out var i) ? i : 0`) before publishing.

### 3.4. Workflow & Logic Versioning
Running workflows are the hardest to manage. If an "Approval" flow is at Step 2, and the user changes Step 3, what happens?

**Strategy: Side-by-Side Versioning**
*   **Workflow Definitions** are versioned (v1, v2).
*   **Workflow Instances** are pinned to a version.
*   **Policy**:
    *   **New Instances**: Start on v2.
    *   **In-Flight Instances**: Continue draining on v1.
    *   *Exception*: "Force Migration" (Complex, usually avoided).
*   **Clean Up**: v1 logic is kept active in the engine until `Count(Instances_v1) == 0`.

## 4. Implementation Plan

### Phase 1: Metadata History & GUIDs
*   Ensure the `EntityDesigner` assigns a GUID to every field/table upon creation.
*   Implement `MetadataSnapshot` store.

### Phase 2: The Diff Engine
*   Build a C# Service `IMetadataDiffService`.
*   Input: `SnapshotA`, `SnapshotB`.
*   Output: `MigrationPlan` object (Proposed SQL, Proposed Workflow Updates).

### Phase 3: Smart Deployer
*   UI for "Publish": Shows the `MigrationPlan`.
*   "Impact Report": "Warning: Deleting 'Status' field will break 3 Reports and 1 Dashboard." (User must acknowledge).

## 5. Missing Scenarios & Edge Cases

### 5.1. API Contract Breakage (External Consumers)
*   **Scenario**: An external ERP system calls your generated API `GET /api/orders`. You rename `Total` to `GrandTotal`. The ERP integration breaks.
*   **Solution**: **API Versioning**.
    *   The platform automatically maintains `GET /v1/orders` (mapping to old schema shape if possible, or erroring gracefully) and exposes `GET /v2/orders`.
    *   We need a "contract mapping" layer for v1 to ensure stability for external consumers.

### 5.2. Multi-Environment Drift
*   **Scenario**:
    *   Dev Environment: v10 (Has new feature X).
    *   QA Environment: v5.
    *   Prod Environment: v2.
    *   User tries to promote Dev -> Prod directly.
*   **Risk**: Skipping migration scripts from v2->v5 might cause the v5->v10 scripts to fail.
*   **Solution**: **Cumulative Migration Generation**. The Delta Engine must compute the diff between *Target* (Prod) and *Source* (Dev) strictly, ignoring intermediate QA states.

### 5.3. collaborative Conflicts
*   **Scenario**: User A edits Page X. User B edits Page X at the same time.
*   **Solution**: **Pessimistic Locking**. When User A opens "Order Entity", it is locked for others. (Simpler for MVP than Merge Conflict Resolution).

### 5.4. Cyclic Dependencies in Deployment
*   **Scenario**: Table A needs Table B (Foreign Key). Table B needs Table A.
*   **Solution**: The Migration Generator must be smart enough to create Tables first, *then* apply Foreign Key constraints in a second pass.
