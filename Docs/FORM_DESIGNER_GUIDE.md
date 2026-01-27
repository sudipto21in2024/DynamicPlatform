# Form Designer – Detailed Specification

## 1. Purpose
The **Form Designer** (also called *Form Architect*) is a visual, low‑code workspace that lets product owners, power users, and developers define **data entry forms** for any entity in the platform. It complements the existing **Page Architect** (dashboard layout) by focusing on **CRUD‑centric UI**, field grouping, validation, and conditional logic.

---

## 2. Core Concepts
| Concept | Description |
|---|---|
| **Form Metadata** | Stored as `ArtifactType.Form`. JSON schema describes the target entity, layout style, sections, and individual fields. |
| **Form Section** | Logical grouping of fields (e.g., *Personal Info*, *Contact Details*). Sections can be ordered and optionally rendered as tabs or accordions. |
| **Form Field** | Represents a single input control. Includes type, label, placeholder, tooltip, validation rules, enum reference, default value, and UI order. |
| **Layout Modes** | `Vertical` (default), `Horizontal`, `Inline`. Determines how fields are placed within a section. |
| **Conditional Logic** | Simple *show/hide* rules based on other field values. Defined in the UI and persisted as a JSON expression. |

---

## 3. Metadata Schema (JSON)
```json
{
  "name": "PatientForm",
  "entityTarget": "Patient",
  "layout": "Vertical",
  "sections": [
    {
      "title": "Basic Information",
      "fieldNames": ["FullName", "DateOfBirth"],
      "order": 0
    },
    {
      "title": "Contact Details",
      "fieldNames": ["Email", "Phone"],
      "order": 1
    }
  ],
  "fields": [
    {
      "name": "FullName",
      "type": "string",
      "isRequired": true,
      "label": "Full Name",
      "placeholder": "John Doe",
      "tooltip": "Legal name as on ID",
      "order": 0
    },
    {
      "name": "DateOfBirth",
      "type": "datetime",
      "isRequired": true,
      "label": "Date of Birth",
      "placeholder": "yyyy‑mm‑dd",
      "order": 1
    },
    {
      "name": "Email",
      "type": "string",
      "isRequired": false,
      "label": "Email",
      "placeholder": "example@domain.com",
      "validationPattern": "^[\\w\\.-]+@[\\w\\.-]+\\.\\w{2,}$",
      "order": 2
    },
    {
      "name": "Phone",
      "type": "string",
      "isRequired": false,
      "label": "Phone",
      "placeholder": "(555) 123‑4567",
      "validationPattern": "^\\(\\d{3}\\) \\d{3}‑\\d{4}$",
      "order": 3
    }
  ]
}
```
*All field names must match an existing `Entity` field name.*

---

## 4. Visual Designer UI (Angular)
1. **Explorer (Left pane)** – List of existing forms with *Create New* button.
2. **Canvas (Center)** – Drag‑and‑drop sections, reorder fields, set layout mode.
3. **Inspector (Right pane)** – Edit field properties (label, placeholder, validation, enum reference, conditional visibility).
4. **Toolbar** – Buttons for *Save*, *Sync Metadata*, *Preview* (renders a live Angular reactive form).

### UI Components
| Component | Responsibility |
|---|---|
| `FormExplorerComponent` | Lists forms, handles selection and deletion. |
| `FormCanvasComponent` | Renders sections and fields, supports drag‑drop via `@angular/cdk/drag-drop`. |
| `FormFieldInspectorComponent` | Property editor for the currently selected field. |
| `FormPreviewComponent` | Generates a live Angular reactive form based on the metadata. |

---

## 5. Code Generation Pipeline
1. **Metadata Load** – `MetadataLoader.LoadFormMetadata` deserializes the JSON into `FormMetadata` objects.
2. **FormGenerator** – Uses the Scriban template `Templates/Backend/Form.scriban` to emit a **C# POCO** (`{{Name}}Form.cs`) with data‑annotation attributes for validation.
3. **Angular Form Component** – The front‑end generator creates a **stand‑alone Angular component** (`{{name}}-form.component.ts`) that builds a `FormGroup` based on the field definitions, wiring validation and conditional logic.
4. **Packaging** – Both the C# class and the Angular component are added to the ZIP export in `BuildController` under `Models/` and `Frontend/src/app/forms/` respectively.

---

## 6. Usage Flow
1. **Create Form** – In Studio, click **New Form**, give it a name, select the target entity.
2. **Design Layout** – Add sections, drag fields, set layout mode.
3. **Configure Fields** – Define labels, placeholders, validation regex, enum references, default values.
4. **Add Conditional Logic** – In the inspector, specify a simple expression like `Status == 'Cancelled' ? true : false` to toggle visibility of a *Cancellation Reason* field.
5. **Sync** – Click **Sync Metadata** – the JSON is persisted as an `Artifact` of type **Form**.
6. **Generate** – When the user clicks **Export Code** or the system runs a build, the FormGenerator emits server‑side DTOs and a client‑side Angular component.
7. **Consume** – Developers can import the generated component into any page (`<app-patient-form></app-patient-form>`) or reference the DTO in API contracts.

---

## 7. Example: Patient Registration Form
*Target Entity*: `Patient`
*Layout*: `Vertical`
*Sections*: `Basic Info`, `Contact`, `Medical History`
*Conditional Logic*: Show **Allergies** textarea only when **HasAllergies** checkbox is true.

The generated **C#** class (`PatientForm.cs`) includes `[Required]` attributes for mandatory fields, while the generated **Angular** component automatically adds the conditional `*ngIf` directive for the allergies field.

---

## 8. Roadmap & Milestones
| Milestone | ETA | Description |
|---|---|---|
| **M1 – Core Metadata & Generator** | ✅ Completed (v1.0) | FormMetadata classes, Scriban template, backend generator. |
| **M2 – Studio UI (Explorer + Canvas)** | ✅ Completed (v1.0) | Angular components, route registration, navigation button. |
| **M3 – Conditional Logic Engine** | Q1 2026 | Simple expression parser, UI toggles, runtime evaluation. |
| **M4 – Advanced UI Controls** | Q2 2026 | File upload, signature pad, rich‑text editor, multi‑select dropdowns. |
| **M5 – Form‑to‑Workflow Binding** | Q3 2026 | Auto‑create a *Create* workflow that persists the form data and triggers post‑save actions. |

---

## 9. FAQ
**Q:** *Can a form be used for both *Create* and *Update*?**
**A:** Yes. The generated Angular component exposes an `@Input` for an existing entity instance. If the input is present, the form is pre‑filled and the submit action calls the **Update** API; otherwise it calls **Create**.

**Q:** *How are enums displayed?**
**A:** If a field’s `enumReference` is set, the generator creates a `<select>` bound to the enum values, automatically pulling the generated C# enum type.

**Q:** *Do forms support file uploads?**
**A:** Currently the core generator supports `string` and `byte[]` types. Future releases (M4) will add a dedicated `FileUpload` control and server‑side handling.

---

## 10. References
- **Engine Model** – `FormMetadata`, `FormSection`, `FormField` (see `EngineModels.cs`).
- **Generator** – `FormGenerator.cs` and `Templates/Backend/Form.scriban`.
- **Studio Component** – `form-designer.ts` (Angular). 
- **Build Integration** – `BuildController.cs` loads `ArtifactType.Form` and adds generated files to the export ZIP.

---

## 11. Master‑Detail Scenario

### Overview
The Form Designer can model **master‑detail** relationships (e.g., a Patient with multiple Appointments) by using the `detailRelations` property on `FormMetadata`. This lets the engine generate a master view with an embedded detail grid, automatically handling parent‑child IDs.

### Metadata Example
```json
{
  "name": "PatientForm",
  "entityTarget": "Patient",
  "layout": "Vertical",
  "sections": [
    { "title": "Basic Info", "fieldNames": ["FullName", "DateOfBirth"], "order": 0 }
  ],
  "fields": [
    { "name": "FullName", "type": "string", "isRequired": true, "label": "Full Name", "order": 0 },
    { "name": "DateOfBirth", "type": "datetime", "isRequired": true, "label": "DOB", "order": 1 }
  ],
  "detailRelations": [
    {
      "navPropName": "Appointments",
      "detailFormName": "AppointmentForm",
      "presentation": "Modal",
      "filterExpression": "Status != 'Cancelled'"
    }
  ]
}
```

### Generated UI
* **Master Component** – renders the patient fields and a button *Add Appointment*.
* **Detail Grid** – displayed as a modal (or drawer) that uses `AppointmentForm`. The grid receives the `parentEntityId` automatically via `FormContext.ParentEntityId`.
* **Submit Flow** – when the detail form is saved, the backend receives the `PatientId` and creates the child record, preserving referential integrity.

### Runtime Context
The `FormContext` added to `FormMetadata` stores:
* `mode` – `Create` or `Edit`.
* `parentEntityId` – the ID of the master record.
* `additionalData` – optional key‑value pairs (e.g., workflow IDs).
These values are injected into the generated Angular component so the child form always knows its master.

---

*End of Document*

*End of Document*
