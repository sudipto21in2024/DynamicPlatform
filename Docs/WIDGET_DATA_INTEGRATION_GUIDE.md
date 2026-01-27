# Widget Data Integration Guide

## 1. Overview
The **Universal Data Binding System** allows any widget (Standard or Custom) to connect to any data source without code changes. This is achieved via the `WidgetDataSource` contract, which decouples the *Widget's View* from the *Data Providers*.

## 2. The Data Contract (`WidgetDataSource`)

Every widget in the Page Designer has a `bindings` object:

```csharp
public class WidgetDataSource
{
    public string Provider { get; set; }  // "Entity", "API", "Workflow", "Static"
    public string Source { get; set; }    // "Patient", "https://api.weather.com", "calc_risk"
    public Dictionary<string, object> Params { get; set; } // { "limit": 10, "state": "active" }
    public Dictionary<string, string> Mapping { get; set; } // { "cardTitle": "FirstName", "score": "RiskValue" }
}
```

### 2.1. The Mapping Layer
Crucially, the **Mapping** dictionary translates the Source's schema into the Widget's expected props.
*   **Key**: The styling property on the Widget (e.g., `title`, `value`, `color`).
*   **Value**: The field name from the Data Source (e.g., `PatientName`, `TotalDebt`, `StatusColor`).

---

## 3. Integration Scenarios

### Scenario A: Entity Binding (The "Smart List")
**Use Case**: Displaying a list of recent Appointments in a `DataGrid` or `Timeline`.
*   **Provider**: `Entity`
*   **Source**: `Appointment`
*   **Params**: `Filter="Status == 'Confirmed'"`, `Sort="Date DESC"`, `Limit=5`
*   **Handshake**: The backend `DashboardController` interprets "Entity" + "Appointment", creates a dynamic EF Core query, applies the filter string, and returns the result.

### Scenario B: API Integration (Third-Party Data)
**Use Case**: Showing Live Stock Prices in a `StatCard`.
*   **Provider**: `API`
*   **Source**: `https://api.finance.com/quote/MSFT`
*   **Params**: `AuthProfile="FinanceAPI_Key"`
*   **Mapping**: `{ "value": "currentPrice", "subTitle": "lastUpdated" }`
*   **Edge Case Handling**: If the API is slow, the widget renders a "Loading..." skeleton. If it fails, it shows an error state.

### Scenario C: Workflow Output (Complex Logic)
**Use Case**: A "Patient Health Score" widget that requires checking history, medications, and age.
*   **Provider**: `Workflow`
*   **Source**: `CalculateHealthScore_Flow`
*   **Params**: `PatientId="{page.context.id}"`
*   **Details**: The widget triggers a server-side short-lived workflow. The flow executes logic and returns a JSON object.

### Scenario D: Static Mocking (Design Phase)
**Use Case**: A designer wants to test how a widget looks with "Error" data without breaking the app.
*   **Provider**: `Static`
*   **Source**: *ignored*
*   **Params**: `{ "value": 99.9, "message": "Critical Failure" }`
*   **Handshake**: The data is passed directly through to the widget props.

---

## 4. Edge Cases & Error Handling

### 4.1. Network & Timeout Failures
*   **Scenario**: The backend API takes > 5 seconds to respond.
*   **Handling**:
    *   **Frontend**: Widget must show a timeout specific skeleton/error after X seconds.
    *   **Backend**: Controller must implement `CancellationToken` for external API and Workflow calls to prevent resource starvation.

### 4.2. Schema Mismatch (Breaking Changes)
*   **Scenario**: The API changes `currentPrice` to `price.current`.
*   **Handling**: 
    *   The **Mapping Layer** fails to find the key. 
    *   **Mitigation**: The system should return `null` for that prop, or the Widget should define "Fallbacks" for critical properties.
    *   **Alerting**: The Page Designer Inspector should warn if a mapped field no longer exists in the Entity metadata.

### 4.3. Data Volume & Pagination
*   **Scenario**: User binds a custom widget to a table with 1M rows without a limit.
*   **Handling**:
    *   **Hard Limits**: The system enforces a default `Limit=100` if none is provided.
    *   **Backend Protection**: The standard `GenericRepository` must reject queries without pagination parameters if `Results > 1000`.

### 4.4. Security & Context
*   **Scenario**: A "Salary Widget" is placed on a public page.
*   **Handling**: 
    *   **Resolution**: The `DataSource` resolver checks RLS (Row Level Security) and Permissions before executing the query.
    *   **Result**: It returns `403 Forbidden` for the data packet. The widget receives an "Access Denied" state, not zero values.

---

## 5. Future Implementation Roadmap

1.  **Expression Engine**: Allow params to use expressions (e.g., `Filter="Date > Today() - 7"`).
2.  **Real-Time Subscriptions**: Add `RefreshMode` ("Poll", "Socket") to the `WidgetDataSource` to support SignalR updates.
3.  **Cross-Widget Dependency**: Allow Widget A (User List) to filter Widget B (User Details) via client-side event bus.
