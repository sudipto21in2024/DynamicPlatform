# Generic Page & Widget Designer Architecture

## 1. Vision & Objectives
The goal is to evolve the platform's UI capabilities from simple "Dashboards" to a **Universal Page & Widget functionality**.
This requires two distinct but integrated designers:
1.  **Page Designer**: Arranges Layouts, Slots, and Widgets.
2.  **Custom Widget Designer**: A low-code studio to build *new* reusable UI components (Widgets) with custom visuals, properties, and data bindings.

---

## 2. Generic Widget Interface (The "Contract")

To make widgets truly generic and pluggable into any data source (Entity, API, Workflow), we define a strict **Widget Contract**.

### 2.1. Anatomy of a Widget
A Widget is composed of:
1.  **View (Template)**: HTML/CSS (Scriban/Angular template).
2.  **Model (Props)**: Input properties configurable in the Page Designer (e.g., `Title`, `Color`).
3.  **Data Contract (Inputs)**: Expected data shape (e.g., `List<{ Label: string, Value: number }>`).
4.  **Events (Outputs)**: signals emitted by the widget (e.g., `Clicked`, `SelectionChanged`).

### 2.2. The `IWidgetDataSource` Standard
To "connect widgets to different data builder stubs", we standardize the data request format.

```json
{
  "provider": "Entity | API | Static | Workflow",
  "source": "Appointment | /api/external/weather | Flow_CalculateRisk",
  "params": {
    "filter": "Status == 'Active'",
    "limit": 10
  },
  "mapping": {
    "title": "PatientName",    // Maps 'PatientName' from source to Widget's 'title' prop
    "value": "TotalBill"       // Maps 'TotalBill' from source to Widget's 'value' prop
  }
}
```

---

## 3. Custom Widget Designer Architecture

This new tool allows developers/users to create *new* widget types without writing code.

### 3.1. Capability
-   **Visual Canvas**: Draw basic HTML elements (Div, Span, Text, Icon, Image).
-   **Property Editor**: Define exposed properties (e.g., "Header Text (String)", "Background Color (Color)").
-   **Binding Studio**: Map internal elements to exposed properties.

### 3.2. Metadata Structure (`WidgetDefinition`)
```json
{
  "id": "widget-custom-001",
  "name": "Patient Risk Card",
  "category": "Healthcare",
  "template": "<div class='card {{theme}}'><h3>{{title}}</h3><span class='risk'>{{riskScore}}</span></div>",
  "props": [
    { "name": "title", "type": "string", "default": "Patient Risk" },
    { "name": "theme", "type": "enum", "options": ["light", "dark"] },
    { "name": "riskScore", "type": "number", "bindingTarget": true }
  ],
  "events": ["onRiskClick"]
}
```

---

## 4. Generic Page Designer Enhancements

We move from "Dashboard" (Grid of Widgets) to "Generic Page".

### 4.1. Structure
-   **Layouts**: Pre-defined structures (Sidebar Left, Header Top, 3-Column, Blank).
-   **Slots**: Named areas in a Layout where Widgets can be dropped.
-   **Responsive Rules**: Breakpoint-specific overrides.

### 4.2. Micro-Interactions & Navigation
Widgets support a standard "Interaction" array for visual feedback and navigation.

```json
"interactions": [
  {
    "trigger": "onClick",
    "action": "Navigate",
    "target": "Page:PatientDetails",
    "params": { "patientId": "{{currentItem.id}}" }
  },
  {
    "trigger": "onHover",
    "action": "ShowTooltip",
    "content": "View details for {{currentItem.name}}"
  }
]
```

---

## 5. Integration with Visual Data Builder (Future)

The **Visual Data Builder Engine** (planned) will produce "Data Stubs".
-   The Page Designer will list available Data Stubs (e.g., "GetHighRiskPatients").
-   The user drags a Widget and selects a Data Stub.
-   The **Generic Adapter** maps the Stub's output to the Widget's Input Contract.

### 5.1. Data Flow
`[Data Builder Stub] --> [Generic Adapter] --> [Widget Inputs] --> [Render]`

---

## 6. Implementation Stages

### Phase 1: Foundations
-   Refactor `WidgetMetadata` to support generic `props` map instead of fixed `config`.
-   Implement `IWidgetDataSource` interface in the Engine.

### Phase 2: Custom Widget Designer (UI)
-   Create a Simple Designer to define a template and props.
-   Save as `ArtifactType.Widget`.

### Phase 3: Generic Page Designer Updates
-   Update Page Designer to load Custom Widgets.
-   Implement the "Mapper" UI to map Data Source fields to Widget Props.
