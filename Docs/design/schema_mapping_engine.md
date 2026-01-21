# Schema Mapping Engine & Visual Relation Mapper

This document details the **Schema Mapping Engine**, a visual tool for transforming data structures between disparate systems (e.g., mapping an External API Response to a Local Entity, or transforming one DTO to another).

## 1. The Visual Mapper UI (The "Data Weaver")

The Studio provides a specialized **Mapping Canvas** used inside Integration connectors and Workflow steps.

### 1.1 Layout
*   **Left Panel (Source)**: A tree view of the incoming schema (e.g., API JSON Response).
*   **Right Panel (Target)**: A tree view of the destination (e.g., Local Database Entity).
*   **Center Canvas**: The workspace where "Wires" are drawn connecting the two sides.

### 1.2 Interactions
1.  **Direct Mapping**: User drags `Source.customer_name` to `Target.FullName`. A bezier curve connects them.
2.  **Transformation Nodes**: User places a "Function Box" in the center.
    *   *Example*: `Concat` Block.
    *   Input 1 connected from `Source.FirstName`.
    *   Input 2 connected from `Source.LastName`.
    *   Output connected to `Target.FullName`.

## 2. Metadata Structure (The "Map Definition")

The mapping is serialized into a JSON structure compatible with standard libraries or our custom engine.

```json
{
  "mapId": "map_external_cust_to_local",
  "sourceSchema": "schema_crm_customer", // Reference to JSON SChema
  "targetSchema": "schema_local_customer",
  "mappings": [
    {
      "type": "Direct",
      "sourcePath": "$.data.attributes.email",
      "targetPath": "Email"
    },
    {
      "type": "Transform",
      "function": "Concat",
      "inputs": ["$.data.firstName", "$.data.lastName"],
      "params": { "separator": " " },
      "targetPath": "FullName"
    },
    {
      "type": "Lookup",
      "function": "ValueMap",
      "sourcePath": "$.status",
      "targetPath": "StatusCode",
      "params": {
        "map": { "Active": 1, "Inactive": 0, "Pending": 2 }
      }
    }
  ]
}
```

## 3. Transformation Function Library

The engine supports a library of "No-Code" transformations:

*   **String**: `UpperCase`, `LowerCase`, `Trim`, `Concat`, `Split`.
*   **Math**: `Add`, `Multiply`, `Round`.
*   **Date**: `FormatDate` (e.g., "MM/dd/yyyy" -> "yyyy-MM-dd"), `AddDays`.
*   **Logic**: `IfEmpty`, `Coalesce` (First non-null).
*   **Collections**: `Pluck` (Extract list of properties from list of objects).

## 4. Execution Engine (AutoMapper + JsonPath)

At runtime, we need high-speed execution. We generally use **JsonPath** (specifically `Newtonsoft.Json` or `System.Text.Json` extensions) to traverse the dynamic source.

### 4.1 The `DataMapperService` (C#)

```csharp
public class DataMapperService {
    public JObject Map(JObject source, MappingDefinition mapDef) {
        var target = new JObject();

        foreach(var rule in mapDef.Mappings) {
            
            // 1. Extract Value(s)
            JToken value;
            if (rule.Type == "Direct") {
                value = source.SelectToken(rule.SourcePath);
            } else if (rule.Type == "Transform") {
                var inputs = rule.Inputs.Select(p => source.SelectToken(p)).ToArray();
                value = ExecuteTransform(rule.Function, inputs, rule.Params);
            }

            // 2. Set Value in Target
            // Uses a helper to create nested objects if path is "Address.City"
            SetNestedValue(target, rule.TargetPath, value);
        }

        return target;
    }
}
```

## 5. Integration with Other Modules

*   **Integration Hub**:
    *   *Usage*: "Response Mapping".
    *   *Flow*: HTTP 200 OK -> `DataMapper.Map(ResponseBody, MapDef)` -> Return Clean DTO.
*   **Workflows**:
    *   *Usage*: "Convert Variable".
    *   *Flow*: Workflow Step `Map Data` takes Variable A, applies Map, outputs Variable B.
*   **Import Wizard** (Excel/CSV):
    *   *Usage*: User uploads CSV. The "Columns" are the Source. The "Entity Fields" are the Target. User maps them once, saves as an "Import Template".

## 6. Testing Tool (The "Simulator")

In the Studio, we provide a **"Test Transformation"** button.
1.  User pastes a sample JSON into the "Source" pane.
2.  Clicks "Run".
3.  The "Target" pane populates with the result in real-time.
4.  Errors (e.g., "Date parse failed") are highlighted on the specific connection line.
