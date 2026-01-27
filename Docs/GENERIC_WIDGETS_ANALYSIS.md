# Generic Widgets Analysis: Requirements & Common Use Case Scenarios

## Executive Summary

This document analyzes the generic widget requirements for the DynamicPlatform based on the Widget Standard Specification, Data Integration Guide, and Page & Widget Designer Architecture. It identifies the standard widget catalog that should be available by default and outlines common use case scenarios for each widget type.

---

## 1. Standard Widget Catalog (Default Available Widgets)

### 1.1. **StatCard** (Summary Widget)
**Purpose**: Display single metric summaries with visual indicators

**Core Requirements**:
- Single value display with optional prefix/suffix (e.g., "$", "%", "units")
- Trend indicator support (up/down arrows, percentage change)
- Server-side aggregation (Count, Sum, Average, Min, Max)
- Compact visual footprint (typically 2-4 grid columns)

**Data Contract**:
```json
{
  "value": "number | string",
  "label": "string",
  "trend": "number (percentage)",
  "trendDirection": "up | down | neutral",
  "prefix": "string (optional)",
  "suffix": "string (optional)",
  "icon": "string (optional)"
}
```

**Common Use Cases**:
- **Healthcare**: Total Patients, Active Appointments, Revenue This Month
- **E-commerce**: Total Sales, Pending Orders, Conversion Rate
- **Finance**: Account Balance, Outstanding Invoices, Payment Success Rate
- **HR**: Active Employees, Open Positions, Average Salary

---

### 1.2. **DataGrid** (Tabular Widget)
**Purpose**: Display detailed lists of records with sorting, filtering, and pagination

**Core Requirements**:
- Column configuration (visibility, width, data type)
- Client-side and server-side sorting
- Search/filter capabilities
- Pagination support (configurable page size)
- Export functionality (CSV, PDF, Excel)
- Row actions (View, Edit, Delete)
- Responsive column hiding for mobile

**Data Contract**:
```json
{
  "columns": [
    {
      "field": "string",
      "header": "string",
      "type": "text | number | date | boolean | enum",
      "sortable": "boolean",
      "filterable": "boolean",
      "visible": "boolean"
    }
  ],
  "rows": "Array<Record>",
  "totalCount": "number",
  "pageSize": "number",
  "currentPage": "number"
}
```

**Common Use Cases**:
- **Healthcare**: Patient List, Appointment Schedule, Medical Records
- **E-commerce**: Order History, Product Inventory, Customer List
- **Finance**: Transaction Log, Invoice List, Payment History
- **Project Management**: Task List, Team Members, Project Timeline

---

### 1.3. **AnalyticsChart** (Visual Widget)
**Purpose**: Render data trends and distributions using various chart types

**Core Requirements**:
- Multiple chart types (Bar, Line, Pie, Doughnut, Area, Scatter)
- Legend configuration (position, visibility)
- Axis customization (labels, scale, grid lines)
- Color scheme support
- Tooltip customization
- Responsive sizing
- Group-by aggregation support

**Data Contract**:
```json
{
  "chartType": "bar | line | pie | doughnut | area | scatter",
  "labels": "Array<string>",
  "datasets": [
    {
      "label": "string",
      "data": "Array<number>",
      "backgroundColor": "string | Array<string>",
      "borderColor": "string"
    }
  ],
  "options": {
    "legend": { "position": "top | bottom | left | right" },
    "responsive": "boolean"
  }
}
```

**Common Use Cases**:
- **Healthcare**: Patient Visits by Month, Disease Distribution, Treatment Outcomes
- **E-commerce**: Sales Trends, Revenue by Category, Customer Demographics
- **Finance**: Expense Breakdown, Revenue vs Costs, Cash Flow Analysis
- **Marketing**: Campaign Performance, Lead Sources, Conversion Funnel

---

### 1.4. **Calendar** (Temporal Widget)
**Purpose**: Display and manage time-based events and schedules

**Core Requirements**:
- Multiple view modes (Day, Week, Month, Agenda)
- Working hours configuration
- Event creation/editing
- Drag-and-drop rescheduling
- Color-coded event categories
- Recurring event support
- Time zone handling
- Conflict detection

**Data Contract**:
```json
{
  "events": [
    {
      "id": "string",
      "title": "string",
      "startTime": "DateTime",
      "endTime": "DateTime",
      "duration": "number (minutes)",
      "category": "string",
      "color": "string",
      "isRecurring": "boolean",
      "recurrencePattern": "string (optional)"
    }
  ],
  "viewMode": "day | week | month | agenda",
  "workingHours": { "start": "time", "end": "time" }
}
```

**Common Use Cases**:
- **Healthcare**: Doctor Appointments, Surgery Schedule, Staff Shifts
- **Education**: Class Schedule, Exam Calendar, Faculty Availability
- **Project Management**: Sprint Planning, Milestone Tracking, Resource Allocation
- **Facilities**: Room Booking, Equipment Reservation, Maintenance Schedule

---

### 1.5. **NarrativeTimeline** (Sequential Widget)
**Purpose**: Display chronological events with visual storytelling

**Core Requirements**:
- Vertical/horizontal orientation
- Status-based color coding
- Compact/expanded view modes
- Icon/avatar support
- Timestamp formatting
- Grouping by date/category
- Infinite scroll or pagination

**Data Contract**:
```json
{
  "events": [
    {
      "id": "string",
      "timestamp": "DateTime",
      "title": "string",
      "description": "string",
      "status": "string",
      "statusColor": "string",
      "icon": "string (optional)",
      "metadata": "object (optional)"
    }
  ],
  "orientation": "vertical | horizontal",
  "viewMode": "compact | expanded",
  "parentId": "string (optional for filtering)"
}
```

**Common Use Cases**:
- **Healthcare**: Patient Medical History, Treatment Timeline, Medication Log
- **E-commerce**: Order Tracking, Shipment History, Customer Journey
- **Finance**: Transaction History, Account Activity, Audit Trail
- **Support**: Ticket History, Communication Log, Issue Resolution Timeline

---

### 1.6. **Hero Section** (Branding Widget)
**Purpose**: High-impact landing page header with call-to-action

**Core Requirements**:
- Large heading and sub-heading support
- Background image/video/gradient
- Call-to-action button(s)
- Overlay opacity control
- Text alignment options
- Responsive text sizing
- Animation support (fade-in, slide-in)

**Data Contract**:
```json
{
  "heading": "string",
  "subHeading": "string",
  "backgroundType": "image | video | gradient | color",
  "backgroundSource": "string (URL or gradient definition)",
  "ctaButtons": [
    {
      "label": "string",
      "action": "Navigate | OpenModal | runWorkflow",
      "target": "string",
      "style": "primary | secondary | outline"
    }
  ],
  "textAlignment": "left | center | right",
  "overlayOpacity": "number (0-1)"
}
```

**Common Use Cases**:
- **Marketing**: Product Launch Pages, Campaign Landing Pages
- **Healthcare**: Patient Portal Welcome, Service Highlights
- **E-commerce**: Promotional Banners, Seasonal Campaigns
- **Corporate**: Company Overview, Service Introduction

---

### 1.7. **Contact Form** (Interaction Widget)
**Purpose**: Capture user input and trigger workflows

**Core Requirements**:
- Dynamic field configuration (text, email, phone, textarea, select, checkbox)
- Validation rules (required, pattern, min/max length)
- Success/error message display
- CAPTCHA/anti-spam integration
- File upload support
- Multi-step form capability
- Workflow trigger on submission

**Data Contract**:
```json
{
  "fields": [
    {
      "name": "string",
      "type": "text | email | phone | textarea | select | checkbox | file",
      "label": "string",
      "placeholder": "string",
      "required": "boolean",
      "validation": {
        "pattern": "regex (optional)",
        "minLength": "number (optional)",
        "maxLength": "number (optional)"
      }
    }
  ],
  "submitAction": {
    "type": "Workflow | API | Email",
    "target": "string (workflow name or endpoint)"
  },
  "successMessage": "string",
  "errorMessage": "string"
}
```

**Common Use Cases**:
- **Marketing**: Lead Generation, Newsletter Signup, Demo Request
- **Healthcare**: Patient Registration, Appointment Booking, Feedback Form
- **Support**: Contact Us, Bug Report, Feature Request
- **HR**: Job Application, Employee Onboarding, Survey

---

### 1.8. **Location Map** (Spatial Widget)
**Purpose**: Display geographic locations with interactive markers

**Core Requirements**:
- Map provider integration (Google Maps, OpenStreetMap, Mapbox)
- Multiple marker support
- Custom marker icons/colors
- Info window on marker click
- Zoom and pan controls
- Geolocation support
- Route/directions display
- Clustering for dense markers

**Data Contract**:
```json
{
  "center": { "lat": "number", "lng": "number" },
  "zoom": "number (1-20)",
  "markers": [
    {
      "id": "string",
      "position": { "lat": "number", "lng": "number" },
      "label": "string",
      "icon": "string (optional)",
      "infoWindow": "string (HTML content)"
    }
  ],
  "mapType": "roadmap | satellite | hybrid | terrain",
  "enableClustering": "boolean"
}
```

**Common Use Cases**:
- **Healthcare**: Clinic Locations, Hospital Network, Service Areas
- **E-commerce**: Store Locator, Delivery Zones, Warehouse Locations
- **Real Estate**: Property Listings, Office Locations
- **Logistics**: Fleet Tracking, Delivery Routes, Distribution Centers

---

### 1.9. **Media Box** (Static Widget)
**Purpose**: Display images, videos, or embedded media

**Core Requirements**:
- Image display with aspect ratio control
- Video playback (local or embedded)
- Object-fit options (cover, contain, fill)
- Lazy loading support
- Lightbox/modal view on click
- Caption/overlay text
- Multiple media carousel

**Data Contract**:
```json
{
  "mediaType": "image | video | iframe",
  "source": "string (URL or AssetId)",
  "altText": "string",
  "aspectRatio": "16:9 | 4:3 | 1:1 | custom",
  "objectFit": "cover | contain | fill | scale-down",
  "caption": "string (optional)",
  "enableLightbox": "boolean",
  "autoplay": "boolean (for video)"
}
```

**Common Use Cases**:
- **Marketing**: Product Images, Promotional Videos, Brand Assets
- **Healthcare**: Medical Imaging, Educational Videos, Facility Photos
- **E-commerce**: Product Gallery, Tutorial Videos, Customer Testimonials
- **Education**: Course Materials, Lecture Videos, Infographics

---

### 1.10. **Action Feed** (Activity Stream Widget)
**Purpose**: Display real-time or recent activity updates

**Core Requirements**:
- Chronological list of activities
- User avatar/icon display
- Action type categorization
- Timestamp (relative or absolute)
- Filtering by action type
- Pagination or infinite scroll
- Real-time updates (via SignalR/WebSocket)

**Data Contract**:
```json
{
  "activities": [
    {
      "id": "string",
      "userId": "string",
      "userName": "string",
      "userAvatar": "string (URL)",
      "actionType": "string",
      "description": "string",
      "timestamp": "DateTime",
      "metadata": "object (optional)"
    }
  ],
  "enableRealTime": "boolean",
  "pageSize": "number",
  "filterByType": "Array<string> (optional)"
}
```

**Common Use Cases**:
- **Social**: User Activity Feed, Notifications, Friend Updates
- **Project Management**: Team Activity, Task Updates, Comment Stream
- **Healthcare**: Patient Activity Log, Staff Actions, System Events
- **E-commerce**: Order Updates, Inventory Changes, Customer Actions

---

## 2. Data Integration Scenarios

### 2.1. **Entity Binding** (Database-Driven)
**Provider**: `Entity`

**Scenario**: Display a list of recent appointments in a DataGrid

```json
{
  "provider": "Entity",
  "source": "Appointment",
  "params": {
    "filter": "Status == 'Confirmed'",
    "sort": "Date DESC",
    "limit": 5
  },
  "mapping": {
    "columns": ["PatientName", "DoctorName", "Date", "Status"]
  }
}
```

**Use Cases**:
- Patient lists, appointment schedules, inventory tables
- Any scenario where data comes directly from platform entities

---

### 2.2. **API Integration** (Third-Party Data)
**Provider**: `API`

**Scenario**: Show live stock prices in a StatCard

```json
{
  "provider": "API",
  "source": "https://api.finance.com/quote/MSFT",
  "params": {
    "authProfile": "FinanceAPI_Key"
  },
  "mapping": {
    "value": "currentPrice",
    "subTitle": "lastUpdated"
  }
}
```

**Use Cases**:
- Weather data, stock prices, currency exchange rates
- Social media feeds, external analytics
- Third-party service status

---

### 2.3. **Workflow Output** (Complex Logic)
**Provider**: `Workflow`

**Scenario**: Patient health score calculation requiring multiple data sources

```json
{
  "provider": "Workflow",
  "source": "CalculateHealthScore_Flow",
  "params": {
    "patientId": "{page.context.id}"
  },
  "mapping": {
    "value": "healthScore",
    "trend": "scoreChange",
    "riskLevel": "riskCategory"
  }
}
```

**Use Cases**:
- Complex calculations (risk scores, recommendations)
- Multi-step data aggregation
- Business rule evaluation
- AI/ML model predictions

---

### 2.4. **Static/Mock Data** (Design Phase)
**Provider**: `Static`

**Scenario**: Testing widget appearance with sample data

```json
{
  "provider": "Static",
  "params": {
    "value": 99.9,
    "message": "Critical Failure",
    "trend": -15.3
  }
}
```

**Use Cases**:
- UI/UX design and prototyping
- Demo environments
- Testing error states
- Documentation screenshots

---

## 3. Cross-Cutting Widget Features

### 3.1. **Micro-Interactions**
All widgets should support standard interaction patterns:

```json
{
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
    },
    {
      "trigger": "onDoubleClick",
      "action": "OpenModal",
      "target": "EditForm"
    }
  ]
}
```

**Common Interaction Types**:
- **Navigate**: Route to another page/component
- **OpenModal**: Display detail view or form
- **RunWorkflow**: Trigger server-side logic
- **ShowTooltip**: Display contextual help
- **EmitEvent**: Trigger cross-widget communication

---

### 3.2. **Responsive Layout**
All widgets must define behavior across breakpoints:

```json
{
  "layout": {
    "desktop": { "colStart": 0, "colSpan": 4, "rowStart": 0, "rowSpan": 2 },
    "tablet": { "colStart": 0, "colSpan": 6, "rowStart": 0, "rowSpan": 2 },
    "mobile": { "colStart": 0, "colSpan": 12, "rowStart": 0, "rowSpan": "auto" }
  }
}
```

---

### 3.3. **Error Handling**
All widgets must handle common error states:

- **Loading State**: Skeleton/spinner while data loads
- **Empty State**: Friendly message when no data available
- **Error State**: Clear error message with retry option
- **Timeout State**: Specific handling for slow/failed requests
- **Access Denied**: 403 handling with appropriate message

---

### 3.4. **Accessibility**
All widgets must support:

- ARIA labels and roles
- Keyboard navigation
- Screen reader compatibility
- High contrast mode
- Focus indicators

---

## 4. Widget Composition Patterns

### 4.1. **Dashboard Pattern**
**Scenario**: Executive overview with multiple metrics

**Widget Combination**:
- 4x StatCards (Revenue, Orders, Customers, Conversion)
- 1x AnalyticsChart (Sales Trend)
- 1x DataGrid (Recent Orders)
- 1x Action Feed (Recent Activity)

---

### 4.2. **Detail Page Pattern**
**Scenario**: Patient profile page

**Widget Combination**:
- 1x Hero Section (Patient Info Header)
- 1x NarrativeTimeline (Medical History)
- 1x Calendar (Upcoming Appointments)
- 1x DataGrid (Prescriptions)
- 1x Media Box (Medical Images)

---

### 4.3. **Landing Page Pattern**
**Scenario**: Marketing website homepage

**Widget Combination**:
- 1x Hero Section (Main CTA)
- 3x StatCards (Key Metrics)
- 1x Media Box (Product Demo Video)
- 1x Contact Form (Lead Capture)
- 1x Location Map (Office Locations)

---

## 5. Future Widget Enhancements

### 5.1. **Planned Additions**
- **Kanban Board**: Task management with drag-and-drop
- **Gantt Chart**: Project timeline visualization
- **File Manager**: Document upload/management
- **Rich Text Editor**: Content creation
- **Chat Widget**: Real-time messaging
- **Notification Center**: Alert management
- **User Profile Card**: Team member display
- **Progress Tracker**: Multi-step process visualization

### 5.2. **Advanced Features**
- **Expression Engine**: Dynamic parameter evaluation (e.g., `Filter="Date > Today() - 7"`)
- **Real-Time Subscriptions**: SignalR integration for live updates
- **Cross-Widget Dependencies**: Widget A filters Widget B via event bus
- **Conditional Visibility**: Show/hide based on user role or data state
- **Custom Theming**: Per-widget color schemes and styles

---

## 6. Implementation Priorities

### Phase 1: Core Widgets (MVP)
1. StatCard
2. DataGrid
3. AnalyticsChart
4. Contact Form

### Phase 2: Enhanced Widgets
5. Calendar
6. NarrativeTimeline
7. Hero Section
8. Media Box

### Phase 3: Advanced Widgets
9. Location Map
10. Action Feed
11. Kanban Board
12. File Manager

---

## 7. Summary

The generic widget library provides a comprehensive foundation for building diverse applications across industries. Each widget follows the **Universal Data Binding System** contract, ensuring:

✅ **Flexibility**: Connect to any data source (Entity, API, Workflow, Static)  
✅ **Consistency**: Standardized configuration and behavior  
✅ **Reusability**: Same widgets work across Healthcare, E-commerce, Finance, etc.  
✅ **Extensibility**: Easy to add new widget types or customize existing ones  
✅ **Low-Code Friendly**: Visual configuration without coding required  

By implementing these 10 core widgets with proper data integration patterns, the platform can support 80%+ of common enterprise application scenarios out of the box.
