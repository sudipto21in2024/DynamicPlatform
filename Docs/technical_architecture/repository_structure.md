# Repository and Project Structure

This document outlines the directory structure for the Low-Code Platform "DynamicPlatform". It follows a Modular Monolith approach initially, with distinct boundaries to facilitate future microservices extraction.

## Root Structure

```text
/DynamicPlatform
├── /src                    # Source code for the platform components
│   ├── /Platform.API       # REST API for the Visual Studio / Metadata Management
│   ├── /Platform.Core      # Domain Models, Interfaces, Enums (Shared Kernel)
│   ├── /Platform.Engine    # The Code Generation Engine (Transpiler)
│   ├── /Platform.Runtime   # Base libraries/frameworks injected into generated apps
│   ├── /Platform.Studio    # The Visual Frontend (Angular/React)
│   └── /Templates          # Scriban templates for code generation
├── /generated_apps         # Output directory for generated applications (Local Dev)
├── /tests                  # Unit and Integration Tests
│   ├── /Platform.Tests.Core
│   ├── /Platform.Tests.Engine
│   └── /Platform.Tests.Integration
├── /docs                   # Documentation
├── /deploy                 # Kubernetes/Docker compose files
└── .gitignore
```

## detailed Component Breakdown

### 1. Platform.API (ASP.NET Core Web API)
The strict backend for the "Studio". It does not run the generated apps.
- **Controllers**: `ProjectsController`, `EntitiesController`, `PagesController`.
- **Services**: `GitIntegrationService`, `MetadataPersistenceService`.
- **Infrastructure**: EF Core context for the *Platform Metadata Database*.

```text
/Platform.API
├── /Controllers
├── /DTOs               # Data Transfer Objects for the UI
├── /Services
├── /Hubs               # SignalR Hubs for collaborative editing
└── Program.cs
```

### 2. Platform.Engine (Class Library)
The core "Compiler" of the system.
- **Inputs**: JSON Metadata.
- **Outputs**: String content (Source Code).
- **Dependencies**: `Scriban`, `Microsoft.CodeAnalysis` (Roslyn) for validation.

```text
/Platform.Engine
├── /Generators
│   ├── EntityGenerator.cs
│   ├── ApiGenerator.cs
│   └── UiComponentGenerator.cs
├── /Parsers            # Validates and normalizes JSON metadata
├── /Compilers          # Roslyn wrappers to compile generated code (optional)
└── /IO                 # File system writers
```

### 3. Platform.Studio (Frontend - Angular)
The visual IDE used by developers to build apps.
- **Framework**: Angular 17+
- **Libs**: `Konva.js` (Canvas), `Monaco Editor` (Code), `RxJS` (State).

```text
/Platform.Studio
├── /src
│   ├── /app
│   │   ├── /core           # Auth, Interceptors
│   │   ├── /features
│   │   │   ├── /designer   # The Drag-and-Drop Canvas
│   │   │   ├── /data-model # Entity Designer (ERD)
│   │   │   └── /flows      # Workflow Editor
│   │   └── /shared         # UI Components
│   └── /assets
└── package.json
```

### 4. Platform.Runtime (NuGet Packages)
Libraries that `generated apps` will reference. This keeps generated code thin.
- `DynamicPlatform.Runtime.Auth`: Standard JWT/OIDC wrappers.
- `DynamicPlatform.Runtime.Data`: Generic Repositories, Audit Logging.
- `DynamicPlatform.Runtime.Workflow`: State machine executors.

### 5. Templates
The "Source of Truth" for how code looks.
- Grouped by technology (Backend vs Frontend).

```text
/Templates
├── /Backend
│   ├── Entity.scriban
│   ├── Repository.scriban
│   ├── Controller.scriban
│   └── DbContext.scriban
└── /Frontend
    ├── Component_Ts.scriban
    ├── Component_Html.scriban
    └── Service.scriban
```

## Solution Files (.sln)
- **Platform.sln**: Includes API, Engine, Core, Runtime.
- **Studio.code-workspace**: VS Code workspace for the frontend.
