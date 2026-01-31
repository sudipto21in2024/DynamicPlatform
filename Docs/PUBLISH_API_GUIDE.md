# Publish & Delta Management API Guide

This document provides technical details for the Frontend Studio to integrate with the **Publishing and Versioning** system of the DynamicPlatform.

## 1. Concepts

### GUID-Based Identity
Every metadata element (Entity, Field, etc.) now has a permanent `Id` (GUID). 
**Frontend Requirement**: When creating a new entity or field in the UI, you MUST generate a new GUID and include it in the JSON payload. This allows the backend to track renames safely.

### Snapshots
A **Snapshot** is an immutable point-in-time record of all artifacts in a project.
- **Draft**: A snapshot created during the "Plan" phase.
- **Published**: A snapshot that has successfully updated the database schema.

---

## 2. API Endpoints

### 2.1. Generate Migration Plan (Dry Run)
Analyze the differences between the current "Draft" state and the "Last Published" state.

- **URL**: `POST /api/publish/{projectId}/plan?version={versionName}`
- **Response**: `MigrationPlan` object.

#### MigrationPlan Structure
```json
{
  "projectId": "guid",
  "fromVersion": "1.0.0",
  "toVersion": "1.1.0",
  "hasBreakingChanges": true,
  "deltas": [
    {
      "type": "Entity",
      "action": "Updated",
      "elementId": "guid",
      "name": "Patient",
      "changes": {
        "Name": { "oldValue": "User", "newValue": "Patient", "isBreaking": false }
      }
    },
    {
      "type": "Field",
      "action": "Added",
      "name": "DateOfBirth",
      "parentId": "patient-guid",
      "changes": {
        "Type": { "oldValue": null, "newValue": "datetime", "isBreaking": false }
      }
    }
  ]
}
```

---

### 2.2. Apply Migration (Publish)
Commit the changes to the database and seal the version.

- **URL**: `POST /api/publish/{projectId}/apply?version={versionName}`
- **Behavior**:
  1. Creates a final snapshot.
  2. Generates SQL DDL.
  3. Executes SQL against the database (Additive or Safe Rename).
  4. Marks snapshot as `IsPublished = true`.
- **Response**: `200 OK` on success.

---

## 3. Handling Breaking Changes

The `MigrationDelta.Changes` dictionary includes an `isBreaking` flag. 
**UI Recommendation**: If `hasBreakingChanges` is `true`, the Studio should show a warning modal listing the hazards:
- **Type Changes** (e.g., String -> Int).
- **Required Fields**: Adding a `Required` field to an existing table with data.
- **Deletions**: Remind the user that while data isn't lost (it's renamed), it won't be visible in the app anymore.

---

## 4. Renames Example
If a user renames an entity:
1. The `elementId` remains the same.
2. The `action` will be `Renamed`.
3. The `previousName` will contain the old name.
The system will run: `ALTER TABLE "OldName" RENAME TO "NewName";`
