# Standard: Data-Driven Widget Specification (v1.0)

This document defines the unified standard for creating and configuring widgets within DynamicPlatform. All widgets must be defined by metadata, allowing them to be fully data-driven, customizable via the UI, and automatically generated.

---

## 1. The Widget Meta-Model

Every widget in a dashboard is a JSON object stored within a `Page` artifact. The schema is divided into **Identification**, **Visual Configuration**, and **Data Binding**.

### 1.1. Core Schema
```json
{
  "id": "unique-widget-uuid",
  "type": "StatCard | DataGrid | Chart | Timeline | Feed",
  "layout": {
    "desktop": { "colStart": 0, "colSpan": 4, "rowStart": 0, "rowSpan": 2 },
    "tablet":  { "colStart": 0, "colSpan": 6, "rowStart": 0, "rowSpan": 2 },
    "mobile":  { "colStart": 0, "colSpan": 12, "rowStart": 0, "rowSpan": "auto" },
    "zIndex": 10,
    "hasPadding": true
  },
  "config": {
    "title": "Total Revenue",
    "subTitle": "Last 30 days",
    ...
```

---

## 2. Grid & Layout System (Standard v1.0)

The dashboard utilizes a **12-Column Floating Grid** system. All width and height parameters are based on these units.

### 2.1. Coordinate System
- **`colStart` (0-11)**: The horizontal starting position.
- **`colSpan` (1-12)**: The width of the widget in columns.
- **`rowStart`**: The vertical starting position (incremental).
- **`rowSpan`**: The height units (fixed height or 'auto').

### 2.2. Responsive Breakpoints
Every widget MUST define its behavior across devices to ensure enterprise usability:
- **Desktop (>= 1200px)**: Default 12-column flow.
- **Tablet (768px - 1199px)**: Usually transitions to 6 or 8 column logic.
- **Mobile (< 768px)**: Widgets typically force `colSpan: 12` to stack vertically.

### 2.3. Visual Constraints
- **Aspect Ratio**: Some widgets (Charts) can be locked to an aspect ratio (e.g., 16:9).
- **Max/Min Height**: Optional parameters to prevent widget overflow in dynamic grids.
  "dataSource": {
    "entityName": "Appointment | CustomQueryName",
    "dataType": "Entity | CustomObject",
    "aggregate": "sum | count | avg | list",
    "dataField": "FeeAmount",
    "filter": "Status == 'Confirmed'",
    "sort": "CreatedAt DESC",
    "pagination": {
      "enabled": true,
      "pageSize": 25,
      "allowClientOverride": true
    },
    "limit": 10
  },
  "actions": [
    {
      "trigger": "onClick",
      "action": "Navigate | OpenModal | runWorkflow",
      "target": "/billing/details"
    }
  ]
}

---

## 3. Data Propagation & Pagination (Standard v1.0)

### 3.1. Source Types
- **Entity**: Direct binding to an auto-generated CRUD API.
- **Custom Object**: Binding to a user-defined logic flow or query that returns a specialized data shape (DTO).

### 3.2. Pagination Standard
Widgets that display lists (DataGrid, Timeline, Action Feed) MUST support the following pagination contract:
- **`enabled` (Boolean)**: If false, the API returns the full result set (up to the `limit`).
- **`pageSize` (Integer)**: Number of records per chunk.
- **`allowClientOverride`**: If true, the generated UI provides a "Items per page" selector.

The generated Backend API will accept `?pageIndex=0&pageSize=25` for these requests and return metadata including `totalCount`.
```

---

## 2. Standard Widget Catalog & Capabilities

### 2.1. StatCard (Summary Widget)
Summarizes a single metric.
- **Customization**: Prefix/Suffix (e.g., "$"), Trend Indicator (Up/Down arrow).
- **Data Logic**: Performs a server-side `Count` or `Sum` on the specified entity.

### 2.2. DataGrid (Tabular Widget)
Detailed list of records.
- **Customization**: Column visibility, Searchable, Exportable (CSV/PDF).
- **Data Logic**: Direct binding to an `Entity` with dynamic filtering.

### 2.3. AnalyticsChart (Visual Widget)
Renders trends using Chart.js or Recharts.
- **Customization**: Chart Type (Bar, Line, Pie), Legend position.
- **Data Logic**: Group-by logic (e.g., `GroupBy(AppointmentDate.Month)`).

### 2.4. Calendar (Temporal Widget)
Essential for scheduling and time-tracking.
- **Customization**: View Mode (Day, Week, Month), Working Hours.
- **Data Logic**: Binds to a `DateTime` field (StartTime) and optional `Duration`.

### 2.5. NarrativeTimeline (Sequential Widget)
Ideal for Medical Records or Audit Logs.
- **Customization**: Dot colors per status, compact mode.
- **Data Logic**: Ordered list of events linked to a specific Parent ID (e.g., `PatientId`).

### 2.6. Hero Section (Branding Widget)
High-impact branding for landing pages.
- **Customization**: Heading, Sub-heading, Call-to-Action (CTA) label.
- **Data Logic**: Mostly static but can be bound to dynamic content.

### 2.7. Contact Form (Interaction Widget)
Standard lead generation form.
- **Customization**: Form fields, success message.
- **Data Logic**: Submits to a specific `Workflow` (e.g., `OnContactFormSubmit`).

### 2.8. Location Map (Spatial Widget)
Interactive map for office/clinic locations.
- **Customization**: Initial Zoom, Marker label.
- **Data Logic**: Binds to a `Lat/Long` or `Address` field.

### 2.9. Media Box (Static Widget)
Renders images or video assets.
- **Customization**: Aspect ratio, object-fit.
- **Data Logic**: Binds to a `String` (URL) or `AssetId`.

---

## 3. The "Widget Engine" Architecture

### Step 1: The Configurator (UI)
In the **Page Designer**, when a user drops a widget, a JSON generator writes the `config` and `dataSource` properties based on the user's selections in the Inspector panel.

### Step 2: The Data Resolver (Backend)
The Generated API includes a `DashboardController`. It accepts a list of `dataSource` objects and returns a unified payload:
```json
{
  "widget-id-1": { "value": 4500, "trend": "+12%" },
  "widget-id-2": [ { "id": 1, "name": "John Doe", "date": "..." } ]
}
```

### Step 3: The Component Factory (Frontend)
The generated Angular app uses a **Dynamic Component Resolver**. It reads the `type` property and mounts the corresponding standard component (e.g., `<app-widget-stat-card>`), passing the `config` and `resolvedData` as `@Input`.

---

## 4. Why This Fixed Standard?
1.  **Consistency**: Every dashboard (Admin, Doctor, Patient) looks and behaves like the same application.
2.  **Extensibility**: Adding a "WeatherWidget" or "AI-Insight" widget only requires adding a new component to the factory and a new `type` to the standard.
3.  **Low Code Accuracy**: The Page Designer doesn't need to know how to write SQL; it just satisfies the `dataSource` contract.
