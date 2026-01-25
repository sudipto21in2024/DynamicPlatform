# Multi-Dashboard & Landing Page Architecture

To support diverse business needs (like the Multi-Doctor Clinic System), DynamicPlatform utilizes a **Role-Based Page Propagation System**. This ensures that an Admin, a Doctor, and a Patient each see a tailored environment upon login.

---

## 1. Architectural Components

### 1.1. Visual Page Architect
The platform provides a high-fidelity workspace (Studio) for defining:
- **Routes & SEO**: e.g., `/doctor/dashboard`, with custom page titles and SEO overrides.
- **12-Column Layout Engine**: A CSS-Grid powered responsive system where widgets possess `colSpan`, `colStart`, and `rowSpan` properties.
- **Access Contexts**: Logic to define if a page is **Public (Landing)**, **Authenticated (Internal)**, or **Role-Based (Dynamic)**.

### 1.2. Metadata Mapping (Logic Layer)
Artifacts of type `Page` store the layout and binding JSON. The generator uses these to build Angular components.
- **Role -> Dashboard Mapping**: Enforced by the Security Artifact, defining default landing pages per role.

### 1.3. Standardized Widget Library
A predefined set of UI components designed for enterprise workflows:
- **Metrics**: `StatCard`, `AnalyticsChart`.
- **Interactions**: `Calendar` (Temporal), `NarrativeTimeline` (Sequential).
- **Core UI**: `Hero Section`, `Rich Text`, `Contact Form`, `Map`.

---

## 2. Universal Data Propagation

The architecture supports two distinct data binding modes for widgets:

### 2.1. Raw Entity Binding
Direct CRUD access to database entities. Designed for simple list views and basic metric counts.

### 2.2. Custom Object (DTO) Binding
Binding to specialized data shapes (CustomObjects) designed in the engine. This allows for complex, aggregated data views (e.g., `PatientRiskSummary`) that combine data from multiple tables.

### 2.3. Native Pagination
The dashboard architecture supports server-side pagination by default for `DataGrid` widgets.
- **Metadata Toggle**: `pagination.enabled` in the `dataSource` schema.
- **API Support**: Accepts `pageIndex` and `pageSize` parameters, returning `totalCount` for UI pager synchronization.

---

## 3. Deployment & Build Flow

1.  **Stage 1: Architecting**: User visually arranges widgets in the 12-column grid.
2.  **Stage 2: Metadata Persist**: Layout saved as JSON in `ArtifactType.Page`.
3.  **Stage 3: Generation**: The `DashboardGenerator` uses Scriban templates to emit Angular `.ts` files.
4.  **Stage 4: Compilation**: The generated app is compiled with the `dashboard-grid` CSS utility classes for perfect responsive fidelity.

---

## 4. Visual Concept: The Page Designer

I have designed a mockup of the **Page Designer** interface which allows for this multi-dashboard configuration. It follows our premium dark-mode aesthetic with glassmorphism effects.

> [View Page Designer Interface Mockup]
