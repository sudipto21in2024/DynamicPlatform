# Enrich Entity Field Metadata — Validation, Labels & Display Properties

## Background

The current `FieldMetadata` model (`EngineModels.cs:L24-45`) stores only basic properties:

| Property | Current State |
|----------|--------------|
| `Name` | ✅ Present |
| `Type` | ✅ Present (string, int, guid, datetime, decimal, bool) |
| `IsRequired` | ✅ Present |
| `MaxLength` | ✅ Present |
| `Rules` | ✅ Partial — only `Regex`, `Range`, `Email`, `Phone` |

**What's missing** (compared to real-world entity modeling):

- **Display Metadata:** No `Label`, `Placeholder`, `Tooltip`, `Description` — the frontend generates labels by splitting PascalCase names at runtime (`entity-designer.ts:L1094-1096`), but this is fragile and not user-controllable.
- **More Validation Rules:** Missing `MinLength`, `MinValue`, `MaxValue`, `URL`, `CreditCard`, `Comparison` (compare two fields), `Custom` (expression-based).
- **Custom Validation Messages:** The existing `ValidationRule.ErrorMessage` exists per rule, but there's no field-level "required message" or "type-mismatch message".
- **Default Values:** No `DefaultValue` property.
- **Database Hints:** No `IsIndexed`, `IsUnique`, `ColumnName` overrides.
- **Control Hints:** No `DisplayOrder`, `GridSpan`, `IsReadOnly`, `IsComputed`, `HideInTable`, `HideInForm`.
- **Enum/Lookup Reference:** The `FormField` model has `EnumReference` but `FieldMetadata` doesn't.

---

## Design Decisions

> **Breaking Schema Change:** Adding new properties to `FieldMetadata` will change the JSON payload shape sent between the frontend and API. Existing saved project snapshots that contain entity metadata JSON may need a **migration** or must handle missing properties gracefully with defaults.

> **Validation Rules Scope:** This plan expands the rule types to include `URL`, `CreditCard`, `Comparison`, `MinLength`, `MinValue`, `MaxValue`, and `Custom`.

---

## Design Decision Analysis

### Question 1: Enum Reference — Where should it live?

Should we add an `EnumReference` field (string pointing to an `EnumMetadata.Name`) directly on `FieldMetadata`? Currently, this only exists on `FormField`.

#### Option A: Add `EnumReference` to `FieldMetadata` ✅ CHOSEN

| | Details |
|---|---|
| ✅ **Single source of truth** | The entity model itself declares "this field is backed by enum X" — no ambiguity when generating code across all 4 languages |
| ✅ **Simpler code generation** | Templates can directly emit `public StatusEnum Status { get; set; }` (C#), `private StatusEnum status` (Java), etc. without needing form metadata |
| ✅ **Better AI integration** | When AI auto-generates entities, it can set `type: "enum"` + `enumReference: "StatusEnum"` in one shot — the form builder inherits it automatically |
| ✅ **Consistency** | Currently `FormField` has `EnumReference` but `FieldMetadata` doesn't — this mismatch means the form builder has to "guess" or manually wire it |
| ⚠️ **Duplication risk** | Both `FieldMetadata.EnumReference` and `FormField.EnumReference` will exist — must ensure `FormField` inherits from `FieldMetadata` during form generation, not independently set |
| ⚠️ **Schema coupling** | Ties field metadata to the enum catalog — if an enum is renamed/deleted, orphaned references need cleanup |

#### Option B: Keep `EnumReference` only on `FormField`

| | Details |
|---|---|
| ✅ **Separation of concerns** | Entity model stays "pure data" — enum rendering is a UI/form concern |
| ✅ **No migration needed** | Existing `FieldMetadata` JSON stays unchanged |
| ❌ **Code generation gap** | Backend entity templates (C#/Java/Python/Node) can't generate typed enum properties — they'd generate `string` instead of `StatusEnum` |
| ❌ **Manual wiring** | The `buildFormFields()` function must manually look up enum references every time, which is fragile |

**Decision:** Option A — add `EnumReference` to `FieldMetadata`. The backend generators *need* to know the enum type to produce correct code. `FormField` can simply inherit the value during form generation.

---

### Question 2: Comparison Rules — Field-level vs Entity-level?

The existing `BusinessRuleMetadata` (`EngineModels.cs:L204-213`) handles entity-level conditions (e.g., "if ConsultationFee > 1000, set IsPremium = true"). Should the new `Comparison` rule type (e.g., "EndDate >= StartDate") live at the field level or entity level?

#### Option A: Field-level (on `ValidationRule` with `CompareField`)

| | Details |
|---|---|
| ✅ **Intuitive UX** | When editing the `EndDate` field, you see its validation rule "must be ≥ StartDate" right there in the Guards panel |
| ✅ **Co-located with other rules** | All validation for a field lives in one place — Regex, Range, Email, and Comparison are all peers |
| ✅ **Template simplicity** | The existing `{{ for rule in field.Rules }}` loop handles it — just emit `[Compare("StartDate")]` in C# or equivalent |
| ⚠️ **Asymmetric** | The rule lives on `EndDate` but references `StartDate` — if `StartDate` is deleted, the rule silently becomes invalid |
| ⚠️ **Limited expressiveness** | Only supports binary field-to-field comparison — can't express "Field A + Field B ≤ Field C" |

#### Option B: Entity-level (on `EntityMetadata` as a new `EntityValidationRules` list)

| | Details |
|---|---|
| ✅ **Symmetric** | Both fields are referenced equally — "EndDate ≥ StartDate" doesn't "belong" to either field |
| ✅ **Richer expressions** | Can support multi-field constraints like "DiscountPrice < OriginalPrice AND DiscountPrice > 0" |
| ✅ **Aligns with `BusinessRuleMetadata`** | Cross-field rules are conceptually entity-level concerns, like the existing `BusinessRuleMetadata` |
| ❌ **Separate UI needed** | Requires a new "Entity Validation Rules" panel in the designer — separate from the field-level Guards |
| ❌ **Template complexity** | Templates need a second loop `{{ for rule in EntityRules }}` alongside the field-level loop |
| ❌ **Discoverability** | Users might not find entity-level rules — they'd expect "EndDate must be after StartDate" to live on the EndDate field |

#### Option C: Hybrid — Simple comparisons at field-level, complex at entity-level ✅ CHOSEN

| | Details |
|---|---|
| ✅ **Best of both** | Simple "GreaterThan / LessThan / Equal" → field-level Guards. Complex multi-field expressions → `BusinessRuleMetadata` |
| ✅ **No new UI panel needed** | Simple cases handled in existing Guards; complex cases already have `BusinessRuleMetadata` |
| ⚠️ **Boundary ambiguity** | Users might be confused about when to use field-level Comparison vs entity-level Business Rules |

**Decision:** Option C (Hybrid) — Keep simple binary comparisons (`EndDate >= StartDate`) at the field level using `CompareField`. Complex multi-field logic stays in the existing `BusinessRuleMetadata`. This avoids new UI panels while covering 90% of use cases.

---

### Question 3: HideInTable / HideInForm — Where should these flags live?

Should `HideInTable` and `HideInForm` flags live on `FieldMetadata` (backend model) or only on `FormField` (form generation model)?

#### Option A: On `FieldMetadata` only (backend model)

| | Details |
|---|---|
| ✅ **Single source of truth** | The field's visibility intent is declared once, at the entity level, and inherited by all generated forms and tables |
| ✅ **Cross-language consistency** | All 4 language generators can respect the flag — e.g., Java DTOs exclude hidden fields, Python schemas skip them |
| ✅ **AI-friendly** | When AI generates entities, it can set `hideInTable: true` for fields like `PasswordHash` or `InternalNotes` — the form builder auto-respects it |
| ✅ **Designer UX** | Users configure visibility right in the field card where they set all other metadata — no context switching |
| ⚠️ **Less flexible** | A single field can't be hidden in the Create form but visible in the Edit form — it's all-or-nothing |

#### Option B: Only on `FormField` (form generation model)

| | Details |
|---|---|
| ✅ **Per-form flexibility** | `HideInForm` can differ between Create form, Edit form, and View form |
| ✅ **Clean separation** | Entity metadata describes the data shape; form metadata describes the UI shape |
| ❌ **Redundant configuration** | If you want a field hidden everywhere, you must set it on every form separately |
| ❌ **No effect on code generation** | Backend generators (Entity classes, DTOs) won't know to exclude hidden fields |
| ❌ **Discovery problem** | Users designing entities won't see visibility flags until they go to form generation |

#### Option C: Both — `FieldMetadata` as default, `FormField` as override ✅ CHOSEN

| | Details |
|---|---|
| ✅ **Maximum flexibility** | Entity-level sets the default ("Description is always hidden in tables"), form-level can override ("but show it in the Detail View form") |
| ✅ **Progressive disclosure** | 90% of cases: set it once on the field. 10% of cases: override per-form |
| ⚠️ **Complexity** | The form builder must implement merge logic: `formField.hideInTable ?? fieldMetadata.hideInTable` |

**Decision:** Option C (Both) — Add `HideInTable` / `HideInForm` on `FieldMetadata` as the default. The `FormField` already has these as optional overrides during `buildFormFields()`. The form builder applies: *"use field-level setting unless form-level explicitly overrides."*

---

### Summary of Design Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| **1. EnumReference** | Add to `FieldMetadata` | Backend generators need it; `FormField` inherits it |
| **2. Comparison rules** | Hybrid (field-level simple, entity-level complex) | Covers 90% of cases without new UI panels |
| **3. HideInTable/Form** | Both layers (field = default, form = override) | Single source of truth with per-form flexibility |

---

## Proposed Changes

### 1. Backend Model — `FieldMetadata` Enrichment

#### [MODIFY] `src/Platform.Engine/Models/EngineModels.cs`

Expand `FieldMetadata` (L24-45) with new properties:

```diff
 public class FieldMetadata
 {
     public Guid Id { get; set; } = Guid.NewGuid();
     public string Name { get; set; } = string.Empty;
     public string Type { get; set; } = "string";
     public bool IsRequired { get; set; }
     public int MaxLength { get; set; }
     public List<ValidationRule> Rules { get; set; } = new();

+    // ── Display Metadata ──
+    public string Label { get; set; } = string.Empty;           // e.g. "First Name"
+    public string Placeholder { get; set; } = string.Empty;     // e.g. "Enter first name"
+    public string Tooltip { get; set; } = string.Empty;         // Help icon hover text
+    public string Description { get; set; } = string.Empty;     // Longer help text / guidance
+    public string ValidationMessage { get; set; } = string.Empty; // Custom "required" error message
+
+    // ── Defaults & Behavior ──
+    public string DefaultValue { get; set; } = string.Empty;    // Default for new records
+    public bool IsReadOnly { get; set; }                        // Non-editable after creation
+    public bool IsComputed { get; set; }                        // Server-computed, not user-entered
+
+    // ── Database Hints ──
+    public int MinLength { get; set; }                          // For string fields
+    public bool IsIndexed { get; set; }                         // Create DB index
+    public bool IsUnique { get; set; }                          // Unique constraint
+    public string ColumnName { get; set; } = string.Empty;      // Override default column name
+
+    // ── UI Control Hints ──
+    public int DisplayOrder { get; set; }                       // Sorting in forms/tables
+    public int GridSpan { get; set; } = 1;                      // Form grid columns (1 or 2)
+    public bool HideInTable { get; set; }                       // Exclude from list/table views
+    public bool HideInForm { get; set; }                        // Exclude from form generation
+
+    // ── Lookup/Enum Reference ──
+    public string EnumReference { get; set; } = string.Empty;   // Name of linked EnumMetadata
 
     // Helper for Scriban (existing)
     public string CsharpType => ...
+
+    // New Scriban helper — auto-generate label from Name if Label is empty
+    public string DisplayLabel => string.IsNullOrEmpty(Label)
+        ? System.Text.RegularExpressions.Regex.Replace(Name, "([A-Z])", " $1").Trim()
+        : Label;
 }
```

Expand `ValidationRule` (L62-67) with new rule types and structured metadata:

```diff
 public class ValidationRule
 {
     public string Type { get; set; } = string.Empty;  // see expanded list below
     public string Value { get; set; } = string.Empty;
     public string ErrorMessage { get; set; } = string.Empty;
+    public string CompareField { get; set; } = string.Empty;  // For "Comparison" type: name of other field
 }
```

**Expanded Rule Types:**

| Type | Value | Description |
|------|-------|-------------|
| `Regex` | Pattern string | Existing — regex match |
| `Range` | `"min,max"` | Existing — numeric range |
| `Email` | (ignored) | Existing — email format |
| `Phone` | (ignored) | Existing — phone format |
| `MinLength` | `"5"` | **New** — minimum string length |
| `MaxValue` | `"1000"` | **New** — maximum numeric value |
| `MinValue` | `"0"` | **New** — minimum numeric value |
| `URL` | (ignored) | **New** — valid URL format |
| `CreditCard` | (ignored) | **New** — credit card format |
| `Comparison` | `"GreaterThan"` or `"Equal"` | **New** — compare to `CompareField` |
| `Custom` | C# expression string | **New** — custom expression evaluation |

---

### 2. Runtime Validation — `RuleEvaluator` Enhancement

#### [MODIFY] `src/Platform.Runtime/Validation/RuleEvaluator.cs`

Add `switch` cases for the new rule types:

- `minlength` — check `stringValue.Length >= int.Parse(ruleValue)`
- `maxvalue` / `minvalue` — numeric comparison
- `url` — URI regex pattern
- `creditcard` — Luhn algorithm or regex
- `phone` — phone regex (currently missing from `RuleEvaluator`, only in Entity template annotations)

---

### 3. Code Generation Templates — All 4 Languages

All templates already iterate `field.Rules`. We extend them to emit the new validation annotations.

---

#### [MODIFY] `src/Platform.Engine/Templates/Backend/Entity.scriban` (.NET)

Add new annotation mappings inside the `{{ for rule in field.Rules }}` block:

```
{{ if rule.Type == "MinLength" }}[MinLength({{ rule.Value }}, ErrorMessage = "{{ rule.ErrorMessage }}")]{{ end }}
{{ if rule.Type == "URL" }}[Url(ErrorMessage = "{{ rule.ErrorMessage }}")]{{ end }}
{{ if rule.Type == "CreditCard" }}[CreditCard(ErrorMessage = "{{ rule.ErrorMessage }}")]{{ end }}
{{ if rule.Type == "MinValue" || rule.Type == "MaxValue" }}...mapped to [Range]...{{ end }}
```

Also add `[Display(Name = "...")]` using `field.DisplayLabel`, `[Column("...")]` for custom column name, and `[Index]` support.

---

#### [MODIFY] `src/Platform.Engine/Templates/Backend/Java/Entity.scriban`

Add Jakarta validation annotations:

```
{{ if rule.Type == "MinLength" }}@Size(min = {{ rule.Value }}, message = "{{ rule.ErrorMessage }}"){{ end }}
{{ if rule.Type == "URL" }}@org.hibernate.validator.constraints.URL(message = "{{ rule.ErrorMessage }}"){{ end }}
```

---

#### [MODIFY] `src/Platform.Engine/Templates/Backend/Python/models.py.scriban`

Add Pydantic `Field()` validators and `@field_validator` decorators:

```python
{{ if field.MinLength > 0 }}= Field(min_length={{ field.MinLength }}){{ end }}
```

---

#### [MODIFY] `src/Platform.Engine/Templates/Backend/Node/entity.ts.scriban`

Add class-validator decorators:

```typescript
{{ if rule.Type == "MinLength" }}@MinLength({{ rule.Value }}, { message: '{{ rule.ErrorMessage }}' }){{ end }}
{{ if rule.Type == "URL" }}@IsUrl({}, { message: '{{ rule.ErrorMessage }}' }){{ end }}
```

---

#### [MODIFY] `src/Platform.Engine/Templates/Backend/Form.scriban`

Use `field.DisplayLabel` instead of `field.label`, and add `[StringLength]` for `MinLength`/`MaxLength` combo.

---

#### [MODIFY] Frontend form templates (3 themes × 2 form templates each)

- `src/Platform.Engine/Templates/Frontend/Default/FormComponent.scriban`
- `src/Platform.Engine/Templates/Frontend/Default/PremiumFormComponent.scriban`
- `src/Platform.Engine/Templates/Frontend/TailAdmin/FormComponent.scriban`
- `src/Platform.Engine/Templates/Frontend/TailAdmin/PremiumFormComponent.scriban`

Add `Validators.minLength()`, `Validators.maxLength()`, and custom error messages per rule.

---

### 4. Frontend — Entity Designer UI Enhancement

#### [MODIFY] `platform-studio/src/app/pages/entity-designer/entity-designer.ts`

**4a. Expand the field card UI (L371-431)** to include new inputs:

```
┌─────────────────────────────────────────┐
│ [PropertyName_________] [⚡] [🗑]       │  ← existing
│ [Type ▼] [Mandatory]                    │  ← existing
│ [Label__________] [Placeholder________] │  ← NEW
│ [Tooltip________] [DefaultValue_______] │  ← NEW
│ [☐ ReadOnly] [☐ Indexed] [☐ Unique]    │  ← NEW
│ [☐ HideInTable] [☐ HideInForm]         │  ← NEW
│ ▸ Guards (Validation Rules Panel)       │  ← existing, enhanced
│   ┌──────────────────────────────────┐  │
│   │ [Type ▼] includes new types      │  │  ← ENHANCED
│   │ [Value___] [CompareField___]     │  │  ← NEW for Comparison
│   │ [ErrorMessage___]                │  │  ← existing
│   └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

**4b. Expand the `addField()` method (L1029-1034)** to include defaults for new properties.

**4c. Expand the `addRule()` method (L1064-1072)** to include `CompareField`.

**4d. Expand the rule type `<select>` dropdown (L413-418)** to include:
- `MinLength`, `MaxValue`, `MinValue`, `URL`, `CreditCard`, `Comparison`, `Custom`

**4e. Update `buildFormFields()` (L1092-1131)** to pass `Label`, `Placeholder`, `Tooltip`, `DefaultValue`, `GridSpan`, and validation messages from field metadata rather than auto-generating them.

**4f. New CSS classes** for the expanded field card layout (inline grid for label/placeholder row, checkbox row styling).

---

### 5. Form Generation Bridge — `generateCrudForms()`

#### [MODIFY] `platform-studio/src/app/pages/entity-designer/entity-designer.ts`

Update `buildFormFields()` at L1092 to use the new field-level metadata directly instead of computing them:

```typescript
return {
  Name: field.name,
  Type: field.type,
  Label: field.label || autoLabel,         // Use explicit label if set
  Placeholder: field.placeholder || `Enter ${autoLabel}`,
  Tooltip: field.tooltip || '',
  DefaultValue: field.defaultValue || '',
  IsRequired: !!field.isRequired,
  ValidationPattern: validationPattern,
  EnumReference: field.enumReference || '',
  GridSpan: field.gridSpan || (isDescription ? 2 : 1),
  Order: field.displayOrder ?? index,
  HideInForm: field.hideInForm || false,
  // ... etc
};
```

---

## Summary of Files Changed

| Layer | File | Change |
|-------|------|--------|
| **Backend Model** | `src/Platform.Engine/Models/EngineModels.cs` | Expand `FieldMetadata` + `ValidationRule` |
| **Runtime** | `src/Platform.Runtime/Validation/RuleEvaluator.cs` | Add `minlength`, `url`, `creditcard`, `minvalue`, `maxvalue`, `phone` cases |
| **Template (.NET)** | `src/Platform.Engine/Templates/Backend/Entity.scriban` | New annotations + Display attribute |
| **Template (.NET)** | `src/Platform.Engine/Templates/Backend/Form.scriban` | Use `DisplayLabel`, add `StringLength` |
| **Template (.NET)** | `src/Platform.Engine/Templates/Backend/Repository.scriban` | Pass new rule types to `RuleEvaluator` |
| **Template (Java)** | `src/Platform.Engine/Templates/Backend/Java/Entity.scriban` | New Jakarta annotations |
| **Template (Python)** | `src/Platform.Engine/Templates/Backend/Python/models.py.scriban` | Pydantic `Field()` validators |
| **Template (Node)** | `src/Platform.Engine/Templates/Backend/Node/entity.ts.scriban` | New class-validator decorators |
| **Template (Frontend)** | 6 form templates across 3 themes | Add validators for new rule types |
| **Frontend** | `platform-studio/src/app/pages/entity-designer/entity-designer.ts` | Expanded field card UI + rule types + form builder |

---

## Verification Plan

### Automated Tests
```bash
dotnet build DynamicPlatform.sln
cd platform-studio && npm run build
```

### Manual Verification
- Open Entity Designer, add a field, verify all new properties (Label, Placeholder, Tooltip, etc.) appear in the sidebar
- Add validation rules using new types (MinLength, URL, etc.) and verify they persist
- Export a .NET project and inspect the generated Entity class for correct annotations
- Export a Java project and inspect Jakarta annotations
- Export a Node.js project and inspect class-validator decorators
