# Granular Implementation Plan

This document breaks down the Low-Code Platform construction into small, verifiable tasks.

## Phase 1: Project Scaffolding (Foundation)

### 1.1 Solution Init
*   [x] Create blank Solution `DynamicPlatform.sln`.
*   [x] Create `src` folder.
*   [x] Create `tests` folder.
*   [x] Create `global.json` pinning .NET SDK version.

### 1.2 Core Libraries Setup
*   [x] Create Class Library `Platform.Core`.
    *   [x] Add `Domain/Entities/Tenant.cs`.
    *   [x] Add `Domain/Entities/Project.cs`.
    *   [x] Add `Domain/Entities/Artifact.cs`.
    *   [x] Add `Common/Result.cs` (Result Pattern wrapper).
*   [x] Create Class Library `Platform.Runtime`.
    *   [x] Add generic `IRepository<T>` interface.
    *   [x] Add `Entity` base class (Audit fields).

### 1.3 Database Infrastructure
*   [x] Create Class Library `Platform.Infrastructure`.
*   [x] Add generic `PlatformDbContext` inheriting `DbContext`.
*   [x] Configure Entity configs (Fluent API) for Tenants/Projects/Artifacts.
*   [x] Implement `ArtifactRepository` (Key method: `GetArtifactsByProject(guid)`).

## Phase 2: The Code Engine (Minimum Viable Generator)

### 2.1 Engine Project
*   [x] Create Console App (later Lib) `Platform.Engine`.
*   [x] Install NuGet: `Scriban`, `Microsoft.CodeAnalysis.CSharp`.

### 2.2 Template System
*   [x] Create folder `Templates/Backend`.
*   [x] Create `Entity.scriban` (Basic C# class template).
*   [x] Create `DbContext.scriban` (DbSet registration template).

### 2.3 Generator Logic
*   [x] Create `MetadataLoader`: Reads JSON from `Artifact` object.
*   [x] Create `EntityGenerator`:
    *   [x] Input: `EntityMetadata`.
    *   [x] Logic: Maps JSON types ("string" -> "string", "guid" -> "Guid").
    *   [x] Process: Renders `Entity.scriban`.
    *   [x] Output: `FileResult` (FileName, Content).

## Phase 3: The Platform API (Backend)

### 3.1 API Setup
*   [x] Create Web API `Platform.API`.
*   [x] Add reference to `Platform.Core` and `Platform.Infrastructure`.
*   [x] Setup Dependency Injection (DbContext, Repositories).
*   [x] Configure Swagger/OpenAPI.

### 3.2 Metadata Management Endpoints
*   [x] Create `ProjectsController`: (CRUD for Projects).
*   [x] Create `EntitiesController`:
    *   [x] `POST /projects/{id}/entities`: Application logic to save JSON to `Artifacts` table.
    *   [x] `GET /projects/{id}/entities`: Retrieve list.

### 3.3 Generation Trigger
*   [x] Create `BuildController`.
*   [x] Endpoint `POST /projects/{id}/build`.
*   [x] Logic: Fetch all artifacts -> Call `Engine.Generate()` -> Zip results -> Return URL.

## Phase 4: The Studio (Frontend Foundation)

### 4.1 Angular Init
*   [x] Run `ng new platform-studio`.
*   [x] Install `TailwindCSS`.
*   [x] Install `Akita`.

### 4.2 Application Layout
*   [x] Create `DashboardLayout` (Sidebar: Projects, Models, KPIs).
*   [x] Create `ProjectList` page.

### 4.3 Entity Designer (The Key Feature)
*   [x] Integrated `Konva.js`.
*   [x] Create `CanvasComponent` (Integrated in EntityDesigner).
*   [x] Implement Drag: Add Node (Entity) to Canvas.
*   [x] Implement Property Panel (Right Sidebar): Edit Entity Name, Fields.
*   [x] Implement Save: Converts Canvas Model -> JSON -> `POST /entities`.

## Phase 5: End-to-End Validation (The "Dry Run") ✅

*   [x] Start API and Studio.
*   [x] Create Project "DemoApp".
*   [x] Create Entity "Customer" (Name: String, CreatedAt: DateTime).
*   [x] Click "Build" and verify persistence.
*   [x] Download ZIP and verify generated C# files (Entities & DbContext).
*   [x] Fixed Scriban property case sensitivity and circular reference issues.

## Phase 6: Core Extensions

*   [x] **Many-to-Many**: Update Engine to detect M:N and generate Middle Entity.
*   [x] **Repository Gen**: Add `Repository.scriban` to engine.
*   [x] **API Gen**: Add `Controller.scriban`.
*   [x] **Frontend Gen**: Add `AngularComponent.scriban`.

## Phase 7: Advanced Features

*   [x] **Rules Engine**: Added Backend Model, Generator support, and Frontend UI (Validation Rules).
*   [x] **Integrations**: Added `IIntegrationConnector` interfaces in Core.
*   [x] **Workflow**: Integrate `Elsa`.
*   [x] **AI**: Add `GeminiService` to API and `AiController`.

---

**Guidance for Agents**:
When picking a task, e.g., "1.2 Core Libraries Setup", always proactively create the files using `write_to_file`. Verify compilation using `dotnet build` before marking complete.
