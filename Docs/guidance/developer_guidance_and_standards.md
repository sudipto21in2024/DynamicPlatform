# Developer Guidance & Project Context

**Target Audience**: AI Agents & Developers contributing to "DynamicPlatform".
**Purpose**: Use this document as the "Source of Truth" for architectural patterns, coding standards, and project structure.

---

## 1. Project Identity
*   **Name**: TypeGravity / DynamicPlatform
*   **Core Philosophy**: **Metadata-First**. The runtime behavior is derived 100% from JSON definitions. Code generation is our transpiler.
*   **Architecture**: Modular Monolith (Server) + SPA (Studio) + Generated Code (Output).

## 2. Technology Stack
*   **Backend & Engine**: .NET 8 (ASP.NET Core), EF Core 8.
*   **Code Generation**: Scriban (Templating), Roslyn (Analysis).
*   **Frontend (Studio)**: Angular 17+, NgRx (State), Konva.js (Canvas), Monaco Editor.
*   **Database**: SQL Server / Azure SQL (supports JSON columns).
*   **Workflows**: Elsa Workflows 3.x.

## 3. Repository Structure (Map)
*   `src/Platform.Core`: Shared Kernel (Domain classes, Interfaces `IGenerator`). **No external dependencies**.
*   `src/Platform.API`: The Management API for the Studio. controls `Artifacts` table.
*   `src/Platform.Engine`: The "Compiler". Reads `Artifacts` -> Writes `.cs/.ts` files.
*   `src/Platform.Runtime`: The "Base Class Library" that generated apps reference.

## 4. Key Architectural Patterns

### 4.1. The "Generation Gap" Pattern (Critical)
When generating code for Users, we **NEVER** overwrite their custom logic.
*   **Generated File**: `MyEntity.Generated.cs` (Partial Class, Do not touch).
*   **User File**: `MyEntity.cs` (Partial Class, Safe to edit).
*   **Rule**: If a feature requires user logic, generate a `partial method OnSomething();` hook.

### 4.2. Metadata Storage (Hybrid)
We do not create 100 relational tables for the metadata.
*   **Table**: `Artifacts`
*   **Column**: `Content` (JSON)
*   **Pattern**: Serialize the specific domain model (e.g., `EntityDef`) into this JSON column. This allows strictly typed C# processing but schema-less storage.

### 4.3. Explicit Join Entities
For Many-to-Many relationships, we **always** generate the middle entity explicitly (e.g., `StudentCourse` instead of hidden `SkipNavigation`).

## 5. Coding Standards

### 5.1. Backend (C#)
*   **Dependency Injection**: Use Constructor Injection everywhere.
*   **Validation**: Use `FluentValidation` for all Request DTOs.
*   **Results**: Use the `Result<T>` pattern. Do not throw Exceptions for control flow (e.g., "Validation failed").
*   **Async**: All I/O bound operations must be `async/await`.

### 5.2. Frontend (Angular)
*   **Smart/Dumb Components**:
    *   *Pages/Containers*: Smart. Talk to Stores/Services.
    *   *Components*: Dumb. Take `@Input`, emit `@Output`.
*   **State**: Use Signals for local state, NgRx/Service-with-Subject for global state.
*   **No "Any"**: Utilize TypeScript strict typing.

## 6. How to Implement a New Feature
1.  **Define Metadata**: Create the JSON Schema in `Docs/design`.
2.  **Update Engine**: Create a `Scriban` template for the new feature.
3.  **Update Studio**: Add the widget/panel to the Angular UI to allow users to edit that JSON.
4.  **Verify Runtime**: Ensure the generated code compiles and runs.

## 7. Reference Documents
*   [Repository Structure](../technical_architecture/repository_structure.md)
*   [DB Schema](../technical_architecture/platform_database_schema.md)
*   [Code Gen Strategy](../technical_architecture/code_generation_strategy.md)
