# Enum Architect: Visual Constant Definitions

The Enum Architect provides a dedicated workspace for defining categorical data types and constants that can be used across your Application's Data Model and Business Logic.

## 1. Core Concepts
Enumerations (Enums) are used to define a fixed set of named constants. In the DynamicPlatform, Enums are metadata artifacts that generate native language structures (e.g., `enum` in C# and TypeScript).

## 2. Using the Enum Architect

### 2.1. Defining an Enum
1. Navigate to the **Enum Architect** from the Studio toolbar.
2. Click **"New Enum"**.
3. Set the **Enum Name** (e.g., `PriorityLevel`). This name will be used as the Type in your entities.
4. Set the **Logical Namespace** (defaults to `GeneratedApp.Entities`).

### 2.2. Managing Values
For each enum, you can add multiple values:
- **Label**: The human-readable name used in code (e.g., `High`, `Medium`, `Low`).
- **Integer Value**: The underlying numeric value stored in the database.

### 2.3. Syncing Metadata
Click **"Sync Metadata"** to persist your definitions to the project repository. These definitions are immediately available to the **Entity Designer** and **Code Generator**.

---

## 3. Integration with Entity Designer

Once an enum is saved, it can be used as a field type:
1. Open the **Entity Designer**.
2. Select an Entity (e.g., `Task`).
3. Add a field (e.g., `Status`).
4. In the **Type** field, type the name of your Enum exactly as defined (e.g., `PriorityLevel`).

The system will automatically:
- Generate the C# `enum` definition.
- Set the field type in the C# class to your Enum type.
- Configure Entity Framework to handle the mapping.

---

## 4. Technical Implementation details

- **Metadata Storage**: JSON within `ArtifactType.Enum`.
- **Backend Generator**: `EnumGenerator.cs` uses `Enum.scriban` to emit C# code.
- **Frontend Compatibility**: Enums are emitted as part of the shared model definitions for Angular components.
