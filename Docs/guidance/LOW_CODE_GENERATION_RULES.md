# Low-Code Angular CRUD Generation — Master Instruction Manual

---

## Table of Contents

1. [Entity Definition Schema](#1-entity-definition-schema)
2. [Data Type → Input Type Mapping](#2-data-type--input-type-mapping)
3. [Form Layout Rules](#3-form-layout-rules)
4. [Field-Level Configuration](#4-field-level-configuration)
5. [Validation Rules](#5-validation-rules)
6. [Section & Grouping Rules](#6-section--grouping-rules)
7. [Dependent Field Rules](#7-dependent-field-rules)
8. [Form Modes](#8-form-modes)
9. [Parent–Child Entity Rules](#9-parentchild-entity-rules)
10. [List / Grid View Rules](#10-list--grid-view-rules)
11. [Navigation & Routing Rules](#11-navigation--routing-rules)
12. [API Generation Rules](#12-api-generation-rules)
13. [Angular Code Generation Rules](#13-angular-code-generation-rules)
14. [System & Audit Fields](#14-system--audit-fields)
15. [Permissions & Visibility Rules](#15-permissions--visibility-rules)
16. [UX & Interaction Rules](#16-ux--interaction-rules)
17. [Error Handling Rules](#17-error-handling-rules)
18. [Naming Conventions](#18-naming-conventions)
19. [Scenario Reference Matrix](#19-scenario-reference-matrix)

---

## 1. Entity Definition Schema

Every entity must be described using a structured definition. The generator reads this schema to produce all artefacts.

### 1.1 Top-Level Entity Properties

| Property | Type | Required | Description |
|---|---|---|---|
| `entityName` | string | ✅ | PascalCase logical name, e.g. `CustomerOrder` |
| `tableName` | string | ✅ | Snake-case DB table name, e.g. `customer_order` |
| `displayName` | string | ✅ | Human-readable label shown in UI, e.g. `Customer Order` |
| `displayField` | string | ✅ | Field used to represent this entity in dropdowns / lookups |
| `primaryKey` | FieldDef | ✅ | Definition of the PK field |
| `fields` | FieldDef[] | ✅ | All data fields |
| `sections` | SectionDef[] | ❌ | Logical groupings for multi-section forms |
| `relations` | RelationDef[] | ❌ | Foreign keys and parent-child links |
| `softDelete` | boolean | ❌ | If true, generate `is_deleted` / `deleted_at` instead of hard delete |
| `auditFields` | boolean | ❌ | Auto-add created/modified metadata fields |
| `versionField` | boolean | ❌ | Add `row_version` for optimistic locking |
| `searchable` | boolean | ❌ | Generate a full-text search endpoint and search bar in grid |
| `exportable` | boolean | ❌ | Show CSV / Excel export button in grid |
| `sortable` | boolean | ❌ | Allow column-level sort in grid (default: true) |
| `permissions` | PermissionDef | ❌ | Role-based access control map |

### 1.2 Field Definition (`FieldDef`)

| Property | Type | Required | Description |
|---|---|---|---|
| `fieldName` | string | ✅ | camelCase field name |
| `columnName` | string | ✅ | snake_case DB column name |
| `label` | string | ✅ | Human-readable label |
| `dataType` | DataType | ✅ | See Section 2 |
| `inputType` | InputType | ❌ | Override auto-mapped input type |
| `required` | boolean | ❌ | Field is mandatory |
| `readOnly` | boolean | ❌ | Shown but not editable |
| `hidden` | boolean | ❌ | Not shown in form or grid |
| `defaultValue` | any | ❌ | Pre-filled value on create |
| `placeholder` | string | ❌ | Input placeholder text |
| `tooltip` | string | ❌ | Help icon with tooltip text |
| `maxLength` | number | ❌ | For string types |
| `minLength` | number | ❌ | For string types |
| `min` | number | ❌ | For numeric types |
| `max` | number | ❌ | For numeric types |
| `precision` | number | ❌ | Decimal places for numeric types |
| `pattern` | string | ❌ | Regex validation pattern |
| `mask` | string | ❌ | Input mask pattern (phone, ID, etc.) |
| `unique` | boolean | ❌ | Triggers async uniqueness check |
| `section` | string | ❌ | Section key this field belongs to |
| `row` | number | ❌ | Explicit row number in form grid |
| `colSpan` | 1 \| 2 | ❌ | Override auto column span |
| `order` | number | ❌ | Render order within section |
| `lookupEntity` | string | ❌ | Entity name for foreign key dropdown |
| `lookupFilter` | FilterDef | ❌ | Pre-filter applied to lookup data |
| `dependsOn` | string[] | ❌ | Fields that drive this field's options or visibility |
| `visibleWhen` | ConditionDef | ❌ | Conditional visibility rule |
| `disabledWhen` | ConditionDef | ❌ | Conditional disabled rule |
| `enumValues` | EnumValue[] | ❌ | Static options for dropdown / radio |
| `multiSelect` | boolean | ❌ | Allow multiple selections |
| `accept` | string | ❌ | File types for upload, e.g. `.pdf,.docx` |
| `maxFileSize` | number | ❌ | Max upload size in KB |
| `showInGrid` | boolean | ❌ | Show as column in list view (default: true for non-large types) |
| `gridOrder` | number | ❌ | Column position in grid |
| `sortable` | boolean | ❌ | Column is sortable in grid |
| `filterable` | boolean | ❌ | Column gets a filter control in grid |
| `gridWidth` | number | ❌ | Pixel width hint for grid column |
| `sensitive` | boolean | ❌ | Mask value in grid (e.g. last-4 only) |

### 1.3 Section Definition (`SectionDef`)

| Property | Type | Required | Description |
|---|---|---|---|
| `key` | string | ✅ | Unique identifier referenced by fields |
| `title` | string | ✅ | Section header label |
| `order` | number | ✅ | Render order of section |
| `collapsible` | boolean | ❌ | Section can be expanded/collapsed |
| `defaultCollapsed` | boolean | ❌ | Collapsed by default |
| `visibleWhen` | ConditionDef | ❌ | Conditional section visibility |
| `columns` | 1 \| 2 | ❌ | Override column count for this section (default: 2) |

### 1.4 Relation Definition (`RelationDef`)

| Property | Type | Required | Description |
|---|---|---|---|
| `type` | `many-to-one` \| `one-to-many` \| `many-to-many` | ✅ | Cardinality |
| `relatedEntity` | string | ✅ | Entity name of the related entity |
| `foreignKey` | string | ✅ | FK field name on the owning side |
| `label` | string | ✅ | Label for child grid section header |
| `order` | number | ❌ | Render order when multiple children exist |
| `cascadeDelete` | boolean | ❌ | Auto-delete children when parent is deleted |
| `childFormPopup` | boolean | ❌ | Open child add/edit in popup (default: true) |
| `inlineEdit` | boolean | ❌ | Allow inline row editing in child grid |
| `minChildren` | number | ❌ | Minimum required child records |
| `maxChildren` | number | ❌ | Maximum allowed child records |
| `sortField` | string | ❌ | Default sort field for child grid |
| `sortDir` | `asc` \| `desc` | ❌ | Default sort direction for child grid |

---

## 2. Data Type → Input Type Mapping

### 2.1 Core Mapping Table

| `dataType` | Default `inputType` | Default `colSpan` | Notes |
|---|---|---|---|
| `string.short` | `text` | 1 | maxLength ≤ 100 |
| `string.medium` | `text` | 1 | maxLength 101–255 |
| `string.long` | `textarea` | 2 | maxLength 256–1000; rows=4 |
| `string.richtext` | `richtext` | 2 | Full-row; WYSIWYG editor |
| `string.code` | `codeeditor` | 2 | Monospace, syntax highlight |
| `string.email` | `email` | 1 | HTML email input + format validation |
| `string.url` | `url` | 1 | HTML url input + format validation |
| `string.phone` | `tel` | 1 | Masked input |
| `string.color` | `colorpicker` | 1 | Color swatch picker |
| `string.password` | `password` | 1 | Masked, toggle visibility icon |
| `string.uuid` | `text` | 1 | ReadOnly + hidden in create mode |
| `number.integer` | `number` | 1 | No decimal; spinner optional |
| `number.decimal` | `number` | 1 | Precision from field def |
| `number.currency` | `currency` | 1 | Prefix with currency symbol |
| `number.percentage` | `number` | 1 | Suffix with `%` |
| `number.rating` | `rating` | 1 | Star widget (1–5 or configurable) |
| `boolean` | `toggle` | 1 | Slide toggle; tri-state if nullable |
| `boolean.checkbox` | `checkbox` | 1 | Explicit override |
| `date` | `datepicker` | 1 | Calendar picker |
| `datetime` | `datetimepicker` | 1 | Date + time picker |
| `time` | `timepicker` | 1 | Time only |
| `daterange` | `daterangepicker` | 2 | From/to date pair |
| `enum.single` | `select` | 1 | Dropdown; ≤ 5 options can use `radio` |
| `enum.multi` | `multiselect` | 1 | Chip-based multi-select |
| `lookup.single` | `autocomplete` | 1 | Async search dropdown |
| `lookup.multi` | `multiautocomplete` | 2 | Multi-select with search |
| `file.single` | `fileupload` | 2 | Single file; drag-drop zone |
| `file.multi` | `multifileupload` | 2 | Multi-file; drag-drop zone |
| `image.single` | `imageupload` | 2 | With inline preview |
| `image.multi` | `multiimageupload` | 2 | Thumbnail gallery + upload |
| `json` | `jsoneditor` | 2 | Formatted JSON editor |
| `array.tags` | `taginput` | 2 | Chip/tag input |
| `geo.coordinates` | `mappoint` | 2 | Map with draggable pin |
| `geo.address` | `addresslookup` | 2 | Google/OpenStreetMap autocomplete |
| `signature` | `signaturepad` | 2 | Canvas-based signature |
| `icon` | `iconpicker` | 1 | Icon library picker |
| `slug` | `text` | 1 | Auto-generates from a source field; editable |

### 2.2 Column Span Override Rule

- Any `inputType` that requires wide visual context (`textarea`, `richtext`, `codeeditor`, `fileupload`, `multifileupload`, `imageupload`, `multiimageupload`, `jsoneditor`, `mappoint`, `addresslookup`, `signaturepad`, `daterangepicker`, `multiautocomplete`, `taginput`) **always renders full-row (colSpan = 2)**, even if `colSpan: 1` is explicitly set.
- All other types default to `colSpan: 1` unless the field definition overrides it.
- A field with `required: true` should never be visually buried; if auto-placement would push it below the fold on a typical screen, force it to a top row.

### 2.3 Enum Rendering Decision Tree

```
enumValues.length <= 3   AND  multiSelect = false  → radio group (horizontal)
enumValues.length <= 5   AND  multiSelect = false  → select dropdown
enumValues.length > 5    AND  multiSelect = false  → searchable select
multiSelect = true       AND  enumValues.length <= 8 → checkbox group
multiSelect = true       AND  enumValues.length > 8  → chip multiselect
```

---

## 3. Form Layout Rules

### 3.1 Grid System

- The form uses a **2-column responsive grid**.
- On screens ≤ 768 px (mobile), all fields collapse to **1 column** regardless of `colSpan`.
- On screens 769–1024 px (tablet), full-row fields stay full-row; others fit 2-per-row if both are `colSpan: 1`.

### 3.2 Auto Row Assignment

When `row` is not explicitly set:

1. Iterate fields in `order` sequence within each section.
2. Pack `colSpan: 1` fields into pairs on the same row.
3. A `colSpan: 2` field always starts on a **new row** and occupies it entirely.
4. A `colSpan: 1` field that is **unpaired** at the end of a section occupies the left column; the right column renders empty.
5. Never split a dependent group (see Section 7) across rows unless unavoidable; if the group exceeds 2 fields per row, allow up to 3-per-row only for `colSpan: 1` fields in that group, on a dedicated row.

### 3.3 Field Ordering Priority

Within a section, fields are sorted by:

1. Explicit `order` value (ascending) — highest priority.
2. `required: true` fields before optional fields (when `order` is equal).
3. Fields that are `dependsOn` targets before their dependents.
4. Original definition order as tiebreaker.

### 3.4 Form Width

- **Standard form**: max-width 960 px, centered.
- **Popup / dialog form** (child entities): max-width 640 px; if field count > 8 or any full-row field exists, expand to 800 px.
- **Wizard form** (multi-step): full page, steps shown in top stepper.

### 3.5 Label Placement

- Default: label **above** the input field.
- If a section has `columns: 1` and all fields are `colSpan: 2`, labels may be placed **inline-left** (side-by-side, label 30% / input 70%) to improve density.
- For boolean toggles and checkboxes: label is placed **to the right** of the control.

---

## 4. Field-Level Configuration

### 4.1 Required Fields

- Append a red asterisk (`*`) to the label.
- Show validation message immediately on blur, not only on submit.
- The submit button must remain **enabled**; show inline errors rather than disabling submission silently.

### 4.2 Read-Only Fields

- Render as styled `<span>` or disabled input — never a plain editable `<input>`.
- In **view mode** (see Section 8.3), all fields are read-only; render as label-value pairs, not form controls.
- Read-only fields are included in the form model but excluded from the PATCH/PUT payload.

### 4.3 Hidden Fields

- `hidden: true` fields are excluded from the DOM entirely.
- They may still exist in the reactive form model if needed for logic or API payload.
- System-assigned fields (UUID PKs, `created_at`) are hidden in create/edit but can appear in view mode.

### 4.4 Default Values

- Applied when the form initialises in **create mode** only.
- Special tokens: `$NOW` (current datetime), `$TODAY` (current date), `$CURRENT_USER_ID`, `$CURRENT_USER_NAME`.
- For lookups: `$FIRST` populates the first record returned by the lookup API.

### 4.5 Placeholder Text

- Shown as HTML `placeholder` attribute.
- If not specified, generate a sensible default: `"Enter {label}"` for text; `"Select {label}"` for dropdowns; `"Pick a date"` for dates.

### 4.6 Tooltip / Help Text

- Render as a `?` icon to the right of the label.
- On hover / focus, show a popover with the tooltip text.
- On mobile, tap triggers the popover.

### 4.7 Sensitive Fields

- In the **grid**, show masked value: last 4 characters visible, rest replaced with `•`.
- In the **form view mode**, show masked value with a "Show" toggle button.
- In create/edit mode, sensitive fields render as `password` type with a visibility toggle.

### 4.8 Slug Fields

- Automatically derive value from the configured source field (e.g., `name` → `name.toLowerCase().replace(/\s+/g, '-')`).
- Show a subtle indicator that it is auto-generated.
- The user can manually override; once overridden, auto-generation stops for that record.
- Validate uniqueness asynchronously.

---

## 5. Validation Rules

### 5.1 Built-in Validators (Auto-applied by `dataType`)

| `dataType` | Auto-applied validators |
|---|---|
| `string.email` | `Validators.email` |
| `string.url` | URL pattern regex |
| `string.phone` | Configurable phone regex or mask |
| `string.short/medium` | `Validators.maxLength(n)` |
| `number.*` | `Validators.min`, `Validators.max` if set |
| `date` / `datetime` | Date range check if `min`/`max` set |
| `file.*` | Size check, MIME type check |
| `string.uuid` | UUID v4 format if editable |

### 5.2 Field-level Custom Validators

Defined via `pattern` (regex string). A `patternMessage` property provides the error message. If absent, generate: `"{label} format is invalid"`.

### 5.3 Async Validators

- Triggered when `unique: true` is set.
- Debounced 400 ms after the last keystroke.
- Shows a spinner inside the input while checking.
- Caches the last-checked value to avoid redundant API calls.
- On edit mode, the current record's own value should not trigger a "duplicate" error.

### 5.4 Cross-Field Validation

Defined as a `formLevelValidators` array on the entity:

```json
{
  "formLevelValidators": [
    {
      "rule": "greaterThan",
      "fieldA": "endDate",
      "fieldB": "startDate",
      "message": "End date must be after start date"
    },
    {
      "rule": "requiredIf",
      "field": "taxNumber",
      "condition": { "field": "isTaxable", "equals": true },
      "message": "Tax number is required when entity is taxable"
    },
    {
      "rule": "atLeastOne",
      "fields": ["email", "phone"],
      "message": "At least one contact method is required"
    }
  ]
}
```

Supported cross-field rules: `greaterThan`, `lessThan`, `greaterThanOrEqual`, `lessThanOrEqual`, `requiredIf`, `disabledIf`, `atLeastOne`, `sumEquals`, `matchField` (confirm password pattern), `mutuallyExclusive`.

### 5.5 Validation Message Placement

- Inline, immediately below the offending field.
- For form-level (cross-field) errors: a banner at the **top of the section** that contains the primary field, or at the form top if it spans sections.
- Never use alert dialogs for validation errors.

### 5.6 Submit Behaviour

1. On submit click: mark all controls as `touched` to reveal untouched-field errors.
2. If valid: disable submit button, show loading spinner, call API.
3. If API returns validation errors (HTTP 422): map `field` errors to specific controls; map global errors to the top-level error banner.
4. Re-enable submit button after API response (success or failure).

---

## 6. Section & Grouping Rules

### 6.1 When to Use Sections

- **Single entity, ≤ 8 fields, no logical sub-groups**: no sections; render all fields in a flat 2-column grid.
- **Single entity, 9–20 fields**: group into 2–4 titled sections using a card/panel layout.
- **Single entity, > 20 fields**: use a **tab-based** layout; each tab corresponds to a section group. The first tab contains all required fields.
- **Multiple related entities on one form** (e.g., `Order` + embedded `Address` + embedded `ContactPerson`): each entity block gets its own **titled card section** with a distinct header style.

### 6.2 Section Header Style

- Renders as a full-width divider row with a label (e.g., `── Shipping Address ──`).
- If `collapsible: true`, add a chevron icon; clicking toggles the section body.
- `defaultCollapsed: true` sections should not contain any required fields (enforce at parse time; throw a generation warning if violated).
- Collapsed sections still validate on submit — surface a "Expand section to fix errors" message.

### 6.3 Multi-Entity Sections

When a parent form embeds a secondary entity inline (not in a child grid):

- The secondary entity section has a **styled card** with a coloured left border or background tint to distinguish it visually.
- Its fields follow all the same layout rules independently.
- Include a "Same as billing address" type shortcut checkbox where the semantic relationship supports it (detected when two sections share identical field structures).

### 6.4 Wizard / Multi-Step Forms

Trigger conditions (any one):

- Entity has `wizard: true` explicitly set.
- Form has > 3 sections AND at least one section is `collapsible`.
- Entity relation has `minChildren > 0` requiring the child section to be completed before save.

Wizard rules:

- Display a **step indicator** at the top (not a sidebar).
- Every step has **Next** and **Back** buttons; the final step has **Save**.
- Validate the current step on **Next** click before advancing.
- Allow clicking already-visited step indicators to jump back.
- Show a summary/review step as the last step if `wizardReviewStep: true` is set.

---

## 7. Dependent Field Rules

### 7.1 Definition

A field is **dependent** when its options, visibility, or value are driven by another field. All dependent fields must be declared via `dependsOn`.

### 7.2 Spatial Proximity Rule

- Dependent fields must be rendered **after** their source fields, in visual order.
- If source and dependent fields share the same section, the generator auto-sequences them: source first, then dependents immediately following, even if explicit `order` values would place them elsewhere.
- A **dependent chain** (e.g., Country → State → City → Pincode) must always be rendered as a **contiguous group** on consecutive rows:
  - If all 4 fit in 2 rows of 2 columns, use that.
  - If the chain is > 4 fields, force each to its own row (colSpan 1, left-aligned) to preserve visual flow.
- Never split a dependent chain across sections.

### 7.3 Cascading Lookup Behaviour

```
On [sourceField] value change:
  1. Clear [dependentField] value
  2. Show loading spinner in [dependentField]
  3. Call API: GET /api/{dependentEntity}?{filterParam}={sourceValue}
  4. Populate [dependentField] options with response
  5. If response has exactly 1 record, auto-select it
  6. If [dependentField] is a further source for deeper dependents, repeat recursively
```

- If `sourceField` is cleared, disable all downstream dependents and clear their values.
- Preserve previously selected values on **edit mode** load (do not re-cascade on initial load, only on user change).

### 7.4 Conditional Visibility

`visibleWhen` supports the following operators: `equals`, `notEquals`, `greaterThan`, `lessThan`, `in`, `notIn`, `isEmpty`, `isNotEmpty`.

```json
{ "field": "employmentType", "operator": "equals", "value": "CONTRACT" }
```

- When a field becomes hidden, its value is **cleared** from the form model (not just visually hidden).
- When a field re-appears, its default value (if any) is re-applied.
- Animate show/hide with a subtle fade + height transition (200ms).

### 7.5 Conditional Disable

`disabledWhen` follows the same structure as `visibleWhen`. A disabled field retains its value in the model but is excluded from the save payload (unless `includeDisabledInPayload: true`).

### 7.6 Computed / Auto-Populated Fields

Defined via `computedFrom`:

```json
{
  "fieldName": "fullName",
  "computedFrom": {
    "expression": "concat",
    "sources": ["firstName", " ", "lastName"]
  }
}
```

Supported expressions: `concat`, `sum`, `multiply`, `subtract`, `divide`, `percentage`, `slugify`, `uppercase`, `lowercase`, `trim`. Computed fields are `readOnly: true` by default. They update in real time as source fields change.

---

## 8. Form Modes

### 8.1 Create Mode

- All fields start empty (or with `defaultValue`).
- System fields (`id`, `created_at`, `created_by`) are hidden.
- URL: `/{entity}/new`
- On save success: navigate to the newly created record's **view mode**, or back to the list (controlled by `afterSaveNavigation: "view" | "list" | "new"`).

### 8.2 Edit Mode

- Fields pre-populated from the API response.
- System fields are hidden except `row_version` (sent in payload for optimistic lock).
- `readOnly` fields render as disabled inputs or styled text.
- URL: `/{entity}/{id}/edit`
- Detect unsaved changes; show a confirmation dialog on navigate-away.
- On save success: navigate to view mode or list per `afterSaveNavigation`.

### 8.3 View / Detail Mode

- All fields render as **label + value pairs** (no form controls).
- Actions available: **Edit**, **Delete**, **Clone** (if `cloneable: true`).
- Sensitive fields are masked with a **Show** toggle.
- Child entity grids are fully functional in view mode.
- URL: `/{entity}/{id}`
- Boolean values display as "Yes / No" or configurable `trueLabel` / `falseLabel`.
- Empty fields display a configurable `emptyPlaceholder` (default: `—`).
- File/image fields display download links or thumbnails.

### 8.4 Clone Mode

- Loads record data as if in create mode but pre-populated.
- Fields with `excludeFromClone: true` are cleared.
- System fields (`id`, timestamps) are always cleared.
- URL: `/{entity}/{id}/clone`
- The page title should indicate cloning (e.g., "New Customer Order (Copied from #1042)").

### 8.5 Inline Edit Mode (Grid Cell)

- Available only for grids that have `inlineEdit: true` on the relation.
- Clicking a cell enters edit mode for that row.
- Show Save (✓) and Cancel (✗) action icons at the end of the row.
- Only `colSpan: 1` fields with simple input types should support inline edit; complex types (file upload, rich text) must always use the popup form.

---

## 9. Parent–Child Entity Rules

### 9.1 Overview

When an entity has `one-to-many` relations, the parent form renders each child entity as a **sub-grid section** below the main form fields.

### 9.2 Child Grid Structure

```
┌─────────────────────────────────────────────────────────┐
│  [Section Icon]  Order Lines                   [+ Add]  │
├──────┬────────────────────┬────────┬────────────────────┤
│  #   │  Product           │  Qty   │  Unit Price  │ ...  │
├──────┼────────────────────┼────────┼──────────────┼──────┤
│  1   │  Widget A          │  10    │  ₹ 250.00    │ ✏ 🗑 │
│  2   │  Widget B          │  5     │  ₹ 500.00    │ ✏ 🗑 │
├──────┴────────────────────┴────────┴──────────────┴──────┤
│  Showing 1–2 of 2         [< Prev]  1  [Next >]          │
└─────────────────────────────────────────────────────────┘
```

### 9.3 Child Grid Column Rules

- Display only fields with `showInGrid: true`.
- Auto-select grid columns: always include `displayField`, then fields with `gridOrder` set, then `required` fields, up to a maximum of **6 columns** before adding a "more" overflow.
- Exclude: `string.long`, `string.richtext`, `string.code`, `json`, `file.*`, `image.*`, `signature`, `mappoint`.
- Always include an **Actions column** (last column, fixed-width 80–120px) with Edit (pencil) and Delete (trash) icons.
- If `inlineEdit: true`: clicking Edit icon on a row enters inline edit mode for that row instead of opening a popup.

### 9.4 Child Grid Pagination

- Default page size: **10**; configurable via `childPageSize`.
- Pagination controls: Previous / Next with page number display.
- If total records ≤ 5, hide pagination controls.
- Server-side pagination is used; never load all child records at once.

### 9.5 Add / Edit Child — Popup Rules

- Popup opens as a **modal dialog** centred on the screen.
- Width: 640 px default; 800 px if any full-row field exists or field count > 10.
- Title: `"Add {childDisplayName}"` or `"Edit {childDisplayName}"`.
- The popup form follows all layout and validation rules from Sections 3–7.
- Parent FK field (`foreignKey`) is hidden in the child popup and auto-set to the parent's ID.
- Popup actions: **Save** and **Cancel**.
- On save success: close popup, refresh child grid, do NOT reload parent form.
- If `minChildren` / `maxChildren` is set: disable the **+ Add** button when `maxChildren` is reached; show validation warning on parent save when `minChildren` is not met.

### 9.6 Delete Child Record

- Always show a **confirmation dialog**: `"Delete this {childDisplayName}? This action cannot be undone."`.
- If `cascadeDelete: true` on a nested child, warn: `"This will also delete all related {grandChildDisplayName} records."`.
- Soft-delete if the child entity has `softDelete: true`.
- On delete success: refresh child grid without reloading parent.

### 9.7 Multiple Child Entities

When a parent has more than one `one-to-many` relation:

- Render each child as a **separate section** below the parent fields, stacked vertically.
- Each section has its own header (per `label` from `RelationDef`) and its own grid.
- The sections are ordered by the `order` property on `RelationDef`.
- Alternatively, if `childLayout: "tabs"` is set on the parent entity, child sections are rendered as **tabs** below the parent fields instead of stacked panels.

### 9.8 Nested Children (Grandchildren)

- A child entity may itself have `one-to-many` children.
- In the child popup, grandchild grids follow the same rules as child grids (Sections 9.2–9.6).
- Nesting is supported to a maximum of **2 levels** (parent → child → grandchild). Beyond this, redirect to a separate full-page form.

### 9.9 Many-to-Many Relations

- Render as a **dual-list or tag-input** control within the parent form, not as a child grid.
- Left panel: available records. Right panel: selected records. Arrow buttons to move between.
- Alternative: chip/tag multiselect if the associated entity is simple (3 or fewer display fields).
- Add an **inline search** to the available-records panel for large datasets.

### 9.10 Parent Form Save Behaviour with Children

- If the parent is in **create mode** and has required children (`minChildren > 0`):
  - Option A (`saveStrategy: "two-step"`): Save parent first, then the form transitions to edit mode enabling the child grid. A banner guides the user to add child records.
  - Option B (`saveStrategy: "transaction"`): Buffer child records in memory and submit parent + children in a single API call.
- Default strategy: `"two-step"`.

### 9.11 Parent Delete Behaviour

- If `cascadeDelete: false` (default) and children exist: block delete and show error `"Cannot delete {parentDisplayName} because it has associated {childDisplayName} records."`.
- If `cascadeDelete: true`: show a strong warning listing the count of child records to be deleted.

---

## 10. List / Grid View Rules

### 10.1 Grid Page Layout

```
┌─────────────────────────────────────────────────────────┐
│  Customer Orders                           [+ New Order] │
├────────────────┬────────────────────────────────────────┤
│  🔍 Search...  │  [Filter ▾]  [Sort ▾]  [Export ▾]       │
├──────┬─────────┬──────────────┬────────────┬────────────┤
│  ☐  │ Order # │ Customer     │ Date       │ Status     │
├──────┼─────────┼──────────────┼────────────┼────────────┤
│  ☐  │ 1042    │ Acme Corp    │ 01/06/2026 │ Confirmed  │
│  ☐  │ 1041    │ Beta Ltd     │ 31/05/2026 │ Draft      │
├──────┴─────────┴──────────────┴────────────┴────────────┤
│  Showing 1–10 of 48    [< 1  2  3  4  5 >]  Per page: 10▾│
└─────────────────────────────────────────────────────────┘
```

### 10.2 Grid Column Auto-selection

1. Include all fields where `showInGrid: true` (or unset, and type is not `string.long`/`richtext`/`file`/`image`/`json`/`signature`).
2. Sort by `gridOrder` ascending; unset `gridOrder` fields come after.
3. Always include the `displayField` as the **first data column**.
4. Always include an **Actions column** as the last column.
5. Maximum visible columns before horizontal scroll: **8** (excluding checkbox and actions).
6. For columns exceeding the max, add a **Column Chooser** icon in the header row.

### 10.3 Row Click Behaviour

- Clicking anywhere on a row (outside action icons) navigates to the **view mode** of that record.
- Action icon clicks are isolated (no row click bubbling).

### 10.4 Search

- Full-text search bar at the top of the grid (if `searchable: true`).
- Debounced 300 ms.
- Highlights matched text in results.
- Searches across all `filterable: true` fields.

### 10.5 Column Filters

- Activated via the **Filter** button or column header dropdown.
- Filter control type matches the field's `inputType`:
  - Text → text contains input
  - Number → min/max range
  - Date → date range picker
  - Boolean → Yes / No / All radio
  - Enum/Lookup → multi-select checkbox list
- Multiple filters can be active simultaneously; show active filter count badge.
- A **Clear All** button removes all active filters.

### 10.6 Sorting

- Click column header to sort ascending; click again to sort descending; third click clears sort.
- Show sort direction arrow in header.
- Only one column sorted at a time (unless `multiSort: true`).

### 10.7 Bulk Actions

- Checkbox in each row + a **Select All** checkbox in the header.
- When rows are selected, a **bulk action toolbar** appears above the grid:
  - **Delete Selected** (with confirmation dialog listing count).
  - Any custom bulk actions defined in `bulkActions` on the entity.
- Bulk delete respects soft-delete if configured.

### 10.8 Export

If `exportable: true`:

- **CSV Export**: current filtered/sorted result set (all pages).
- **Excel Export**: same, with column headers.
- Only columns currently visible in the grid are exported.
- Sensitive fields are masked in exports unless the user has an elevated role.

### 10.9 Empty State

- When no records exist: show an illustration + `"No {displayName} records found."` + a **Create First {displayName}** button.
- When filters produce no results: show `"No results match your filters."` + **Clear Filters** button.

### 10.10 Loading & Skeleton State

- On initial load and page change: show a skeleton row shimmer (same number of rows as `pageSize`) instead of a spinner to reduce layout shift.

---

## 11. Navigation & Routing

### 11.1 Route Structure

```
/{entityRoute}                        → List/Grid view
/{entityRoute}/new                    → Create form
/{entityRoute}/:id                    → View/Detail
/{entityRoute}/:id/edit               → Edit form
/{entityRoute}/:id/clone              → Clone form
```

`entityRoute` is the plural, kebab-case form of `entityName` (e.g., `customer-orders`).

### 11.2 Breadcrumbs

- Grid page: `Home > {displayName}`
- Create: `Home > {displayName} > New {displayName}`
- View: `Home > {displayName} > {displayFieldValue}`
- Edit: `Home > {displayName} > {displayFieldValue} > Edit`
- Clone: `Home > {displayName} > {displayFieldValue} > Clone`

Breadcrumb links navigate to their respective pages.

### 11.3 Module Lazy Loading

- Each entity generates its own **Angular feature module** with lazy loading.
- Route registration: `{ path: '{entityRoute}', loadChildren: () => import('./...') }`.

### 11.4 Guard Rules

- `CanDeactivate` guard on all create/edit forms. If the form is dirty (unsaved changes), prompt: `"You have unsaved changes. Leave anyway?"`.
- `CanActivate` guard if permissions are configured (see Section 15).

### 11.5 Query Params for Grid State

Grid state (current page, sort, filters, search term) is reflected in the URL query params so that sharing the URL or using the browser back button restores the exact grid state.

---

## 12. API Generation Rules

### 12.1 Standard REST Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/{entity}` | List with pagination, sort, filter, search |
| `GET` | `/api/{entity}/:id` | Single record by PK |
| `POST` | `/api/{entity}` | Create new record |
| `PUT` | `/api/{entity}/:id` | Full update |
| `PATCH` | `/api/{entity}/:id` | Partial update |
| `DELETE` | `/api/{entity}/:id` | Delete (or soft-delete) |
| `GET` | `/api/{entity}/lookup` | Lightweight list for dropdown (id + displayField only) |
| `GET` | `/api/{entity}/export` | CSV/Excel export stream |
| `POST` | `/api/{entity}/bulk-delete` | Bulk delete by ID array |
| `GET` | `/api/{entity}/count` | Total record count with optional filter |
| `POST` | `/api/{entity}/:id/clone` | Clone a record |

Child entity endpoints follow the same pattern nested under parent:
`/api/{parentEntity}/:parentId/{childEntity}`

### 12.2 List Endpoint Query Parameters

| Param | Description |
|---|---|
| `page` | 1-based page number |
| `pageSize` | Records per page (default 10, max 100) |
| `sortField` | Field name to sort by |
| `sortDir` | `asc` or `desc` |
| `search` | Full-text search string |
| `filter[fieldName]` | Value filter per field |
| `filter[fieldName][op]` | Operator: `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `contains`, `startsWith`, `in`, `between` |
| `fields` | Comma-separated field list for sparse fieldsets |

### 12.3 Response Envelopes

**List response:**
```json
{
  "data": [ { ...record } ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "total": 48,
    "totalPages": 5
  }
}
```

**Single record response:**
```json
{
  "data": { ...record }
}
```

**Error response (HTTP 4xx/5xx):**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more fields are invalid",
    "fields": {
      "email": "Email already in use",
      "endDate": "End date must be after start date"
    }
  }
}
```

### 12.4 Soft Delete Behaviour

If `softDelete: true`:

- `DELETE /api/{entity}/:id` sets `is_deleted = true` and `deleted_at = NOW()`.
- All `GET` list endpoints automatically filter `is_deleted = false`.
- `GET /api/{entity}/:id` returns 404 for soft-deleted records.
- A separate `GET /api/{entity}/archive` endpoint returns only soft-deleted records.
- `POST /api/{entity}/:id/restore` un-deletes a record.

### 12.5 Optimistic Locking

If `versionField: true`, every `PUT` / `PATCH` request must include `row_version`. If the server's version doesn't match, return HTTP 409 with `"CONFLICT"` error code.

### 12.6 File Upload Endpoints

```
POST /api/{entity}/:id/files/{fieldName}   → Upload file, returns fileUrl
DELETE /api/{entity}/:id/files/{fieldName}  → Remove file
GET /api/{entity}/:id/files/{fieldName}     → Download / stream file
```

---

## 13. Angular Code Generation Rules

### 13.1 Technology Choices

| Concern | Choice |
|---|---|
| Forms | Reactive Forms (`FormGroup`, `FormArray`) |
| State | Component-level RxJS; NgRx optional via flag |
| HTTP | `HttpClient` with a generated service |
| Validation | `Validators` + custom validator functions |
| UI Components | Angular Material (default) or configurable component library |
| Routing | Angular Router with lazy-loaded feature modules |
| Popups | `MatDialog` (Angular Material) |
| Grid | `MatTable` + `MatPaginator` + `MatSort` |
| Notifications | `MatSnackBar` for success/info; `MatDialog` for confirmation |
| Styling | SCSS with CSS custom properties |

### 13.2 Generated File Structure per Entity

```
src/app/features/{entity-route}/
├── {entity-route}.module.ts
├── {entity-route}-routing.module.ts
├── models/
│   └── {entity}.model.ts          ← TypeScript interfaces
├── services/
│   └── {entity}.service.ts        ← API calls
├── store/                         ← (if NgRx flag)
│   ├── {entity}.actions.ts
│   ├── {entity}.reducer.ts
│   ├── {entity}.effects.ts
│   └── {entity}.selectors.ts
├── list/
│   ├── {entity}-list.component.ts
│   ├── {entity}-list.component.html
│   └── {entity}-list.component.scss
├── form/
│   ├── {entity}-form.component.ts
│   ├── {entity}-form.component.html
│   └── {entity}-form.component.scss
├── detail/
│   ├── {entity}-detail.component.ts
│   ├── {entity}-detail.component.html
│   └── {entity}-detail.component.scss
└── shared/
    ├── {entity}-filter.component.ts  ← Filter panel
    └── {entity}-column-chooser.component.ts
```

For each child entity popup:
```
└── {child-entity}-popup/
    ├── {child-entity}-popup.component.ts
    ├── {child-entity}-popup.component.html
    └── {child-entity}-popup.component.scss
```

### 13.3 TypeScript Model Generation

Generate:

```typescript
// Raw API shape
export interface {Entity}Dto {
  id: string | number;
  fieldName: FieldType;
  // ...
  createdAt?: string;   // ISO 8601
  updatedAt?: string;
}

// Form model (includes UI-only state)
export interface {Entity}FormModel extends {Entity}Dto {
  // derived/computed fields
}

// List row model (sparse)
export interface {Entity}ListItem {
  id: string | number;
  displayField: string;
  // grid-visible fields only
}

// Paginated response
export interface {Entity}Page {
  data: {Entity}ListItem[];
  pagination: Pagination;
}
```

### 13.4 Service Generation

Every generated service extends a `BaseCrudService<T>` that provides all standard CRUD methods. Entity-specific overrides go in the generated class.

```typescript
@Injectable({ providedIn: 'root' })
export class {Entity}Service extends BaseCrudService<{Entity}Dto> {
  protected override basePath = '/api/{entityRoute}';

  // Auto-generated lookup method
  getLookup(filters?: Record<string, unknown>): Observable<LookupItem[]> { ... }

  // Override points for custom logic
  beforeSave(payload: Partial<{Entity}Dto>): Partial<{Entity}Dto> { return payload; }
  afterSave(record: {Entity}Dto): void {}
}
```

### 13.5 Form Builder Generation

- Use `FormBuilder.group()` for the root form.
- Use `FormBuilder.array()` for `array.tags` and `one-to-many` child collections when `saveStrategy: "transaction"`.
- Each section maps to a nested `FormGroup`.
- Validators are added at declaration time, not imperatively later.
- Async validators are added separately with `{ asyncValidators: [...] }`.

### 13.6 Change Detection

- Use `ChangeDetectionStrategy.OnPush` on all generated components.
- Use `async` pipe in templates for all observables; no manual subscriptions in components unless absolutely necessary.
- Unsubscribe via `takeUntilDestroyed()` (Angular 16+) or a `DestroyRef` injection.

### 13.7 Loading & Error State

Each component exposes:

```typescript
isLoading = signal(false);
isSaving = signal(false);
error = signal<string | null>(null);
```

These drive template conditional rendering and button disabled states.

### 13.8 Interceptors (Generated Once per Project)

- **Auth Interceptor**: Attaches Bearer token to every API request.
- **Error Interceptor**: Translates HTTP errors to user-facing messages; handles 401 (redirect to login), 403 (show forbidden page), 500 (show generic error snackbar).
- **Loading Interceptor**: Tracks in-flight requests for a global progress bar.

---

## 14. System & Audit Fields

### 14.1 Audit Fields (when `auditFields: true`)

| Field | Column | Type | Behaviour |
|---|---|---|---|
| `createdAt` | `created_at` | `datetime` | Auto-set on create; read-only |
| `createdBy` | `created_by` | `string.uuid` | Auto-set from current user ID; read-only |
| `createdByName` | `created_by_name` | `string.short` | Denormalised display name; read-only |
| `updatedAt` | `updated_at` | `datetime` | Auto-set on update; read-only |
| `updatedBy` | `updated_by` | `string.uuid` | Auto-set from current user ID; read-only |
| `updatedByName` | `updated_by_name` | `string.short` | Denormalised display name; read-only |

- These fields are **hidden** in create/edit mode.
- They are **visible** in view/detail mode as a collapsed "Record Info" section at the bottom.
- They are **not exported** to CSV/Excel by default (configurable via `exportAuditFields`).

### 14.2 Soft Delete Fields (when `softDelete: true`)

| Field | Column | Type | Behaviour |
|---|---|---|---|
| `isDeleted` | `is_deleted` | `boolean` | Default false; always hidden in UI |
| `deletedAt` | `deleted_at` | `datetime` | Nullable; always hidden in UI |
| `deletedBy` | `deleted_by` | `string.uuid` | Nullable; always hidden in UI |

### 14.3 Version Field (when `versionField: true`)

| Field | Column | Type | Behaviour |
|---|---|---|---|
| `rowVersion` | `row_version` | `number.integer` | Hidden in all form views; sent in PUT/PATCH payload |

### 14.4 Sort / Order Field (when `sortable: true` and `manualSort: true`)

| Field | Column | Type | Behaviour |
|---|---|---|---|
| `sortOrder` | `sort_order` | `number.integer` | Exposed in child grids as a drag handle column; hidden in standalone forms |

In child grids with `manualSort: true`, add a drag-handle icon column as the first column; row drag-and-drop updates `sortOrder` via a `PATCH /api/{entity}/sort-order` endpoint accepting an ordered ID array.

---

## 15. Permissions & Visibility Rules

### 15.1 Permission Definition

```json
{
  "permissions": {
    "create": ["admin", "editor"],
    "read": ["admin", "editor", "viewer"],
    "update": ["admin", "editor"],
    "delete": ["admin"],
    "export": ["admin", "editor"],
    "fields": {
      "salary": { "read": ["admin", "hr"], "update": ["admin"] },
      "internalNotes": { "read": ["admin", "editor"], "update": ["admin"] }
    }
  }
}
```

### 15.2 Permission Application Rules

- **Page-level**: If the current user lacks `read` permission, the route guard redirects to a 403 page.
- **Button-level**: The **+ New** button is hidden if user lacks `create`. Edit and Delete action icons are hidden if user lacks `update` / `delete`.
- **Field-level**:
  - If user lacks `read` permission for a field: field is completely excluded from the DOM and from API response.
  - If user has `read` but not `update`: field renders as read-only in edit mode.
- Permissions are evaluated on the client for UI hiding AND enforced on the server independently.

### 15.3 Permission Service Interface

```typescript
interface PermissionService {
  canCreate(entity: string): boolean;
  canRead(entity: string): boolean;
  canUpdate(entity: string): boolean;
  canDelete(entity: string): boolean;
  canReadField(entity: string, field: string): boolean;
  canUpdateField(entity: string, field: string): boolean;
}
```

---

## 16. UX & Interaction Rules

### 16.1 Action Button Placement

- **Primary action** (Save, Next): bottom-right of form.
- **Secondary action** (Cancel, Back): bottom-left of form.
- **Destructive action** (Delete): never adjacent to Save; place in view-mode header or accessible via a "More actions" (...) menu.
- For popup/dialog forms: actions in the dialog footer (Material `mat-dialog-actions`).

### 16.2 Loading States

| Context | Indicator |
|---|---|
| Initial page / grid load | Skeleton shimmer |
| Form load (edit/view) | Skeleton rows matching field count |
| Save / submit | Spinner inside submit button; button disabled |
| Lookup search | Spinner inside dropdown |
| File upload | Progress bar below upload zone |
| Delete confirmation executing | Spinner in dialog confirm button |

### 16.3 Success / Error Notifications

- **Save success**: Snackbar (bottom-center), 3 s auto-dismiss, green, message: `"{displayName} saved successfully."`.
- **Delete success**: Snackbar, 3 s, with **Undo** action (if soft-delete enabled).
- **API error**: Snackbar, 5 s, red/error colour; includes a **Details** action to expand the full error.
- **Validation error on submit**: No snackbar — errors are shown inline. Scroll to the first invalid field.
- **Connection/network error**: Persistent snackbar (no auto-dismiss) with **Retry** action.

### 16.4 Confirmation Dialogs

Required for:

- Record delete (single or bulk).
- Navigate away with unsaved changes.
- Cascade delete where children will be affected.
- Any action with `requireConfirmation: true` in entity definition.

Confirmation dialog structure:

```
Title:    "Delete Customer Order?"
Body:     "This will permanently delete order #1042. This action cannot be undone."
Actions:  [Cancel]  [Delete]  ← destructive action is right-aligned, coloured red
```

### 16.5 Inline Row Actions vs Menu

- ≤ 2 actions per row: show icon buttons directly.
- 3 actions per row: show 2 icons + a `...` overflow menu for the third.
- > 3 actions per row: always use a `...` overflow menu.

### 16.6 Form Dirty Tracking

- Track dirty state via `form.dirty` (Angular built-in).
- Clear dirty state after a successful save.
- Clear dirty state if the user cancels and confirms the "Discard changes?" dialog.

### 16.7 Keyboard Navigation

- `Escape` closes popups and dialogs (without saving).
- `Ctrl+S` / `Cmd+S` triggers form save (where applicable).
- Tab order follows visual field order (match DOM order to visual order).
- Data grids support arrow key navigation between cells when `inlineEdit: true`.

### 16.8 Responsive Behaviour Summary

| Viewport | Form Layout | Grid Layout |
|---|---|---|
| < 600 px (mobile) | 1-column, full-width | Card-based list view instead of table |
| 600–960 px (tablet) | 2-column, full-width | Horizontal scroll table, sticky first column |
| > 960 px (desktop) | 2-column, max-width 960 px | Full table with all columns |

On mobile, replace the data table with a card-list view showing `displayField`, 2–3 key fields, and action icons. Pagination remains the same.

---

## 17. Error Handling Rules

### 17.1 HTTP Error Mapping

| HTTP Status | User Message |
|---|---|
| 400 Bad Request | Show field-level errors from response body |
| 401 Unauthorized | Redirect to login |
| 403 Forbidden | Show "You don't have permission to perform this action" |
| 404 Not Found | Show "Record not found. It may have been deleted." + Back to List button |
| 409 Conflict | Show "This record was modified by someone else. Please reload and try again." |
| 422 Unprocessable Entity | Show field-level validation errors from `error.fields` |
| 429 Too Many Requests | Show "Too many requests. Please wait a moment and try again." |
| 500 Server Error | Show "An unexpected server error occurred. Please try again." |
| Network Error | Show "Unable to connect to the server. Check your connection." |

### 17.2 Optimistic UI Updates (Optional)

If `optimisticUI: true` on the entity:

- For delete: remove the row immediately, then confirm with the server; restore on failure.
- For status-toggle: update the toggle immediately, confirm with server; revert on failure.
- Not applied to create or full save operations.

### 17.3 Retry Logic

- For GET requests: auto-retry up to 2 times with exponential backoff (1 s, 2 s) on network errors or 500s.
- For POST / PUT / PATCH / DELETE: do **not** auto-retry (to avoid duplicate mutations); show manual Retry action.

---

## 18. Naming Conventions

### 18.1 Angular Artifacts

| Artifact | Convention | Example |
|---|---|---|
| Module | `{PascalCase}Module` | `CustomerOrderModule` |
| Component | `{PascalCase}Component` | `CustomerOrderListComponent` |
| Service | `{PascalCase}Service` | `CustomerOrderService` |
| Model/Interface | `{PascalCase}Dto`, `{PascalCase}FormModel` | `CustomerOrderDto` |
| Route path | `{kebab-case}` (plural) | `customer-orders` |
| File names | `{kebab-case}.{type}.ts` | `customer-order-list.component.ts` |
| Selector | `app-{kebab-case}` | `app-customer-order-list` |
| Store action prefix | `[{DisplayName}]` | `[Customer Order] Load List` |

### 18.2 API Paths

| Convention | Example |
|---|---|
| Plural kebab-case | `/api/customer-orders` |
| Nested child | `/api/customer-orders/:id/order-lines` |
| Action sub-resource | `/api/customer-orders/:id/clone` |

### 18.3 TS Identifiers

| Artifact | Convention | Example |
|---|---|---|
| Interface property | camelCase | `orderDate` |
| Enum value | UPPER_SNAKE_CASE | `ORDER_STATUS.CONFIRMED` |
| Observable variable | camelCase with `$` suffix | `orders$`, `loading$` |
| Signal variable | camelCase no suffix | `isLoading`, `errorMessage` |

---

## 19. Scenario Reference Matrix

### 19.1 Field Type Decision Tree

```
Is the field a foreign key reference?
├── Yes, single value → lookup.single (autocomplete dropdown)
└── Yes, multiple values → lookup.multi

Is the value chosen from a fixed list?
├── Yes, ≤ 3 options, single select → enum.single → radio group
├── Yes, 4-5 options, single select → enum.single → select dropdown
├── Yes, > 5 options, single select → enum.single → searchable select
├── Yes, any count, multi-select → enum.multi (see Section 2.3)
└── No → continue

Is it a date/time?
├── Date only → date
├── Time only → time
├── Date + time → datetime
├── Date range → daterange
└── No → continue

Is it a number?
├── Money/price → number.currency
├── Rate/ratio → number.percentage
├── Score/rating (1-5) → number.rating
├── Other decimal → number.decimal
└── Integer → number.integer

Is it a file or binary?
├── Single image → image.single
├── Multiple images → image.multi
├── Single file → file.single
└── Multiple files → file.multi

Is it a boolean?
├── Toggle/switch semantic → boolean (toggle)
└── Checkbox semantic → boolean.checkbox

Is it a string?
├── Email → string.email
├── URL → string.url
├── Phone → string.phone
├── Password/secret → string.password
├── Long text/description → string.long
├── Rich/formatted text → string.richtext
├── Code/script → string.code
├── maxLength > 100 → string.medium
└── Default → string.short
```

### 19.2 Common Pattern Catalogue

| Pattern | Configuration |
|---|---|
| Country → State → City cascade | Three `lookup.single` fields; State `dependsOn: ["country"]`; City `dependsOn: ["country", "state"]` |
| Billing + Shipping address | Two sections, same field structure; "Same as billing" checkbox triggers a `copySection` action |
| Product + Line items | Parent entity (Product) with `one-to-many` to `OrderLine`; child grid in parent form |
| Draft → Published workflow | `enum.single` status field; `visibleWhen` conditions show/hide fields per status |
| Conditional required field | `taxNumber` with `visibleWhen` and `requiredIf` cross-field validator |
| Auto-generated slug | `slug` field with `computedFrom: { expression: "slugify", sources: ["name"] }` |
| Confirm password | Two `string.password` fields; `matchField` cross-field validator |
| Date range validation | `startDate` + `endDate` with `greaterThan` cross-field validator |
| File upload with preview | `image.single` field; preview renders below upload zone on selection |
| Rating with label | `number.rating` field; `enumValues` used to map numeric value to label text |
| Soft-deletable with restore | `softDelete: true`; archive route; Restore action in archived grid |
| Auditable record | `auditFields: true`; "Record Info" collapsed section in view mode |
| Inline editable child grid | `RelationDef.inlineEdit: true`; row edit/save without popup |
| Drag-to-sort child records | `manualSort: true` on child entity; drag handle column in child grid |
| Multi-level nested entities | Parent → Child → Grandchild; grandchild grid inside child popup |
| Role-restricted field | `field.permissions.read: ["admin"]`; field absent from DOM for non-admin |
| Wizard form | `wizard: true`; section-per-step; step validation on Next click |

---

*End of Low-Code Generation Rules — Version 1.0*
