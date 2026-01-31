# AI Healing & Runtime Rule Engine Implementation Plan

This document outlines the strategy for implementing **AI-driven healing** for broken metadata and a **Runtime Business Rule Engine**.

## 1. AI Healing Proposal Engine 🩹
High-level goal: Automatically suggest and apply fixes for dependencies broken by renames or type changes.

### Phase 4.1: Detection & Extraction
- [ ] **Enhance `IMetadataDiffService`**: 
    - Identify "Secondary Hazards" (Workflows, Pages, or Rules that reference a renamed/deleted element).
    - Map the Hazard to the specific GUID of the renamed element.
- [ ] **Dependency Registry**: Create a lookup service to find where a specific Entity/Field GUID is used across all JSON artifacts.

### Phase 4.2: AI-Powered Suggestion (The "Healer")
- [ ] **Implement `IHealingService`**:
    - **Input**: The `MigrationPlan` (detailing what changed) + The affected Artifacts.
    - **Logic**: Construct a prompt for the `GeminiService`:
      > "The field 'ConsultationFee' (GUID: 123) was renamed to 'Charge'. 
      > Here is a Workflow JSON using 'ConsultationFee'. 
      > Return the updated JSON with the correct mapping."
    - **Output**: A list of `HealingProposal` objects (Old JSON vs. New JSON).

### Phase 4.3: One-Click Healing API
- [ ] **Controller Endpoints**:
    - `POST /api/publish/{id}/propose-fixes`: Returns AI suggestions.
    - `POST /api/publish/{id}/apply-fixes`: Updates the project's artifacts with the healed values.

---

## 2. Runtime Rule Engine ⚡
High-level goal: Execute "If-Then" business logic dynamically during database operations.

### Phase 5.1: Rule Evaluation Foundation
- [ ] **Expression Parser**: Integrate `DynamicExpresso` or `System.Linq.Dynamic.Core`.
- [ ] **`IBusinessRuleEngine`**:
    - `EvaluateCondition(object entity, string expression)`: Returns bool.
    - `ExecuteAction(object entity, string actionExpression)`: Mutates the entity.

### Phase 5.2: EF Core Integration
- [ ] **`BusinessRuleInterceptor`**: 
    - A `SaveChangesInterceptor` that intercepts EF Core operations.
    - For every entity being saved:
        1. Query `BusinessRuleMetadata` for that Entity Type.
        2. Run `BeforeSave` rules (e.g., "If Amount > 1000 then Set Tier = 'Gold'").
        3. Validate against `ValidationRules`.

### Phase 5.3: Reporting & Audit
- [ ] **Rule Execution Log**: Track when a rule was triggered and if it failed to parse.

---

## 3. Combined E2E Flow
1. **User** renames a field in the Studio.
2. **Delta Engine** detects the rename.
3. **Healing Engine** identifies 5 Workflows using the old name and generates "Healed JSON" via Gemini.
4. **User** clicks "Apply Fixes".
5. **Runtime Engine** now uses the new field name during live transactions because the metadata was healed.

---

## Technical Stack
- **AI**: Gemini Pro 1.5.
- **Rule Evaluation**: `DynamicExpresso` (High performance string-to-LINQ expression compiler).
- **Triggering**: EF Core Interceptors.
