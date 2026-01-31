# Tutorial: Building a Clinical Trial Management System (CTMS)

In this tutorial, you will build a system to manage drug trials across 100+ sites globally. The primary challenge is that **Trial Protocols** change frequently, requiring the database to evolve without losing historical patient data.

## 🛠️ Phase 1: The Base Schema
Create your baseline entities in the **Entity Designer**:
1. **Trial**: `Name`, `Phase`, `StartDate`.
2. **Subject**: `PatientInitials`, `EnrollmentDate`, `SiteId`.
3. **Visit**: `VisitType (Screening/Dosing/Followup)`, `VisitDate`.

## 🧬 Phase 2: Handling Protocol Amendments (Delta Management)
Midway through the trial, the FDA requires a new data point: `OxygenLevel` for every `Visit`.
1. Open the `Visit` entity.
2. Add `OxygenLevel` (Decimal).
3. Set `IsRequired = true`.
4. Click **Publish**.
5. **DynamicPlatform Logic**: The engine detects the delta, adds the column to the database via `SqlSchemaEvolutionService`, and creates a new `ProjectSnapshot` for v1.1.0.

## 🤖 Phase 3: Automated Enrollment Workflow
Use the **Workflow Designer (Elsa)** to automate site notifications:
1. **Trigger**: *EntityCreated* on `Subject`.
2. **Action**: *ExecuteDataQuery* to find Site Coordinators' emails.
3. **Action**: *NotifyUser* via Email and In-App notification.
4. **Logic**: If `PatientInitials` match an existing record, trigger a "Duplicate Enrollment" alert.

## 📄 Phase 4: Regulatory Reporting (QuestPDF)
The trial needs a monthly "Subject Status Report" in PDF.
1. Use the **Report Designer**.
2. Create a query: `JOIN Subject with Visit GROUP BY SubjectId`.
3. Map the results to a table layout.
4. Output as **PDF**.
5. The `OutputGenerator` will handle the high-fidelity rendering for submission to regulators.

## 🛡️ Phase 5: Version Pinning
Since this is a clinical trial, you can't have "Site A" on a different version than "Site B".
- Use the **Snapshot Comparison** tool to verify that all environments have the same Migration Hash.
- If a change is breaking, the **Compatibility Middleware** ensures that old data entry forms (built for v1.0) don't crash when trying to save to the v1.1 database.
