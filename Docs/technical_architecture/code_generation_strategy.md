# Code Generation Strategy & Architecture

The "Heart" of the platform is the **Platform.Engine**. It transforms JSON metadata into compiling source code.

## 1. Core Principles

1.  **Idempotency**: Running the generator twice on the same metadata must produce the exact same output.
2.  **Generation Gap**: Never overwrite user-modified files. We use the **Partial Class** pattern for C# and **Inheritance/Composition** patterns for TypeScript/Angular.
3.  **Compile-Ready**: The output of the generator must be a valid, compilable solution without manual intervention.

## 2. Generator Pipeline (The "Compiler")

The pipeline consists of sequential phases:

```mermaid
graph TD
    A[Load Metadata] --> B[Validator]
    B --> C[Normalizer]
    C --> D[Backend Generator]
    C --> E[Frontend Generator]
    D --> F[Write to Disk]
    E --> F
    F --> G[Post-Process (Prettier/Roslyn Format)]
```

### Phase 1: Validator
Checks for logical errors before trying to generate code.
- "Does Entity A reference Entity B which doesn't exist?"
- "Are there duplicate routes in pages?"
- "Are restricted keywords used?" (e.g., naming a table `Select`).

### Phase 2: Normalizer / Context Builder
Transforms the "Storage Metadata" into "Template Models".
- Resolves all Foreign Keys.
- Calculates derived naming conventions (e.g., Entity `User` -> Table `App_Users`, API `POST /api/users`).
- Injests "System Types" (Auditable fields, ID fields).

### Phase 3: Template Rendering
Uses **Scriban** to merge the Context Model with `.scriban` templates.

## 3. Backend Generation Strategy (ASP.NET Core)

### 3.1 Entities
- **File**: `Domain/Entities/{EntityName}.Generated.cs`
- **Pattern**: `partial class`
```csharp
// AUTO-GENERATED
public partial class Customer {
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```
- **User Extension**: `Domain/Entities/{EntityName}.cs`
```csharp
public partial class Customer {
    public bool IsVIP() => this.TotalSpend > 1000;
}
```

### 3.2 DTOs (Data Transfer Objects)
Automated mapping generation.
- `Create{Entity}Dto`
- `Update{Entity}Dto`
- `List{Entity}Dto`

### 3.3 API Controllers
- **File**: `Controllers/{Entity}Controller.Generated.cs`
- **Base Class**: `GeneratedControllerBase<T>`
- **Customization**: Feature folders or distinct Controller classes. *We do not support partial controllers well, better to use partial Services.*

### 3.4 Services / Logic
- **Interface**: `I{Entity}Service.Generated.cs`
- **Implementation**: `partial class {Entity}Service`
- **Hooks**:
    - `OnBeforeCreate(entity)`
    - `OnAfterCreate(entity)`
    - Users implement these partial methods in their own file to inject logic.

## 4. Frontend Generation Strategy (Angular)

### 4.1 Components
- **HTML**: Fully generated. User modifications are overwritten.
- **TS**:
    - `customer-list.generated.component.ts` (Abstract class)
    - `customer-list.component.ts` (Extends generated class)
    - **Logic**: Users override methods in the subclass.

### 4.2 Services
- `services/{entity}.service.ts`: Fully generated proxy to the API.

## 5. Template Management

Templates are stored in the `Templates/` directory and loaded into memory at startup.

**Example `Entity.scriban`:**
```scriban
using System;
using System.ComponentModel.DataAnnotations;

namespace {{ namespace }}.Domain
{
    public partial class {{ name }}
    {
        {{ for field in fields }}
        {{ if field.is_key }}[Key]{{ end }}
        public {{ field.csharp_type }} {{ field.name }} { get; set; }
        {{ end }}
    }
}
```

## 6. Conflict Resolution
If a user modifies a *PROTECTED* file (Generated), the CLI/Engine checks the file hash.
- If changed: Throw Error "File modified manually. Please revert or move logic to partial class."
- Alternatively: Force overwrite (with backup).
