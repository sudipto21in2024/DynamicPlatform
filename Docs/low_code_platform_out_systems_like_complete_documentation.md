# Product Requirements Document (PRD)

## 1. Overview
This document defines the product requirements for building a Low-Code Application Development Platform similar to OutSystems, targeted at enterprise and internal business applications.

## 2. Vision
Enable developers and tech-savvy business users to rapidly build, deploy, and maintain full-stack web applications using visual modeling and metadata-driven generation.

## 3. Goals
- Reduce application development time by 60–70%
- Enable rapid CRUD + workflow-based app creation
- Provide full code ownership and extensibility
- Be cloud-native and Azure-first

## 4. Target Users
- Enterprise Developers
- Solution Architects
- Internal IT Teams

## 5. In-Scope Features
- Visual App Builder (UI, Data, Workflow)
- Metadata-driven application definition
- Automated code generation (Frontend + Backend)
- Authentication & RBAC
- Single-tenant (MVP)

## 6. Out of Scope (MVP)
- Mobile app generation
- Marketplace
- AI-assisted development
- Multi-language support

## 7. Non-Functional Requirements
- Scalability: Horizontal scaling
- Availability: 99.9%
- Security: OAuth2 / OpenID Connect
- Performance: <200ms API response

---

# Business Requirements Document (BRD)

## 1. Business Objective
Accelerate internal application delivery and reduce dependency on large development teams.

## 2. Business Drivers
- Faster time-to-market
- Reduced development cost
- Standardized architecture

## 3. Stakeholders
- CTO
- Engineering Managers
- Enterprise Architects

## 4. Success Metrics
- Number of apps generated
- Reduction in dev effort
- User adoption

---

# System Architecture Document

## 1. Architecture Style
- Modular Monolith (MVP)
- Microservices-ready

## 2. High-Level Components
- Visual Builder (Frontend)
- Metadata Service
- Code Generation Engine
- Runtime Platform
- Identity & Security

## 3. Deployment Architecture
- Azure App Service / AKS
- Azure SQL Database
- Azure Blob Storage

---

# High-Level Design (HLD)

## 1. Component Diagram

```
[UI Builder]
     |
[Metadata API]
     |
[Code Generator]
     |
[Runtime Engine]
```

## 2. Technology Stack
- Frontend: Angular
- Backend: ASP.NET Core
- DB: Azure SQL
- Auth: Azure AD / IdentityServer

## 3. Data Flow
1. User designs app visually
2. Metadata saved as JSON
3. Generator produces code
4. Runtime executes app

---

# Low-Level Design (LLD)

## 1. Metadata Schema (Example)
```json
{
  "entity": "Customer",
  "fields": [
    { "name": "Id", "type": "guid" },
    { "name": "Name", "type": "string" }
  ]
}
```

## 2. Code Generator Modules
- UI Generator
- API Generator
- DB Migration Generator

## 3. Runtime Components
- Dynamic Controller Factory
- Policy Engine
- Workflow Executor

---

# Integration Document

## 1. External Integrations
- REST APIs
- Webhooks
- OAuth Providers

## 2. Integration Patterns
- Request/Response
- Event-driven

## 3. API Standards
- OpenAPI 3.0
- JSON payloads

## 4. Security
- JWT Tokens
- Role-based access control

---

# Step-by-Step Execution Plan (End-to-End)

---

## Step 1: Problem Definition & Scope Finalization

### Objective
Define *exactly* what kind of low-code platform is being built and for whom.

### Activities
- Identify target users (Developers / Power Users / Enterprises)
- Identify application types (CRUD, workflow, admin portals)
- Decide deployment model (Single-tenant initially)

### Deliverables
- Finalized PRD scope
- Feature inclusion/exclusion list

---

## Step 2: Platform Capability Breakdown

### Objective
Break the platform into independently buildable capabilities.

### Capabilities
- Visual UI Builder
- Data Modeling Engine
- Workflow Modeling Engine
- Metadata Management
- Code Generation Engine
- Runtime Execution Platform

### Deliverables
- Capability map
- Dependency matrix

---

## Step 3: Domain & Metadata Modeling

### Objective
Design metadata-first architecture.

### Metadata Domains
- Application Metadata
- UI Metadata
- Entity & Relationship Metadata
- Workflow Metadata
- Security Metadata

### Deliverables
- JSON schema definitions
- Versioned metadata contracts

---

## Step 4: Visual App Builder Design

### Objective
Enable visual creation of apps.

### Subsystems
- Drag-and-drop UI designer
- Entity designer
- Workflow designer

### Technology
- Angular
- Konva.js / React Flow
- Monaco Editor

### Deliverables
- UI wireframes
- Component palette definition

---

## Step 5: Backend Metadata Services

### Objective
Persist and manage metadata.

### Components
- Metadata API
- Validation engine
- Versioning engine

### Technology
- ASP.NET Core Web API
- Azure SQL

### Deliverables
- REST API contracts
- DB schema

---

## Step 6: Code Generation Engine

### Objective
Convert platform metadata into fully executable, maintainable, and extensible application code.

---

# Metadata Schemas (Authoritative Models)

> All applications in the platform are **pure metadata**. Code is a derived artifact.

---

## 1. Application Metadata Schema

```json
{
  "appId": "guid",
  "name": "CustomerManagement",
  "version": "1.0.0",
  "createdBy": "userId",
  "createdOn": "datetime",
  "settings": {
    "authentication": true,
    "authorization": "rbac",
    "database": "sql"
  }
}
```

---

## 2. Entity (Data Model) Metadata Schema

```json
{
  "entityId": "guid",
  "name": "Customer",
  "tableName": "Customers",
  "fields": [
    {
      "name": "Id",
      "type": "Guid",
      "isPrimaryKey": true,
      "isNullable": false
    },
    {
      "name": "Name",
      "type": "String",
      "maxLength": 200
    },
    {
      "name": "Email",
      "type": "String",
      "isUnique": true
    }
  ],
  "audit": true
}
```

---

## 3. Relationship Metadata Schema

```json
{
  "sourceEntity": "Customer",
  "targetEntity": "Order",
  "relationshipType": "OneToMany",
  "foreignKey": "CustomerId",
  "cascadeDelete": false
}
```

---

## 4. UI Page Metadata Schema

```json
{
  "pageId": "guid",
  "name": "CustomerList",
  "route": "/customers",
  "layout": "grid",
  "components": [
    {
      "type": "DataTable",
      "bindEntity": "Customer",
      "actions": ["Create", "Edit", "Delete"]
    }
  ]
}
```

---

## 5. Component Metadata Schema

```json
{
  "componentType": "InputText",
  "bindField": "Customer.Name",
  "validation": {
    "required": true,
    "minLength": 3
  }
}
```

---

## 6. Workflow Metadata Schema

```json
{
  "workflowId": "guid",
  "name": "CustomerApproval",
  "trigger": "OnCreate",
  "steps": [
    {
      "type": "UserTask",
      "role": "Manager"
    },
    {
      "type": "Decision",
      "condition": "Approved == true"
    }
  ]
}
```

---

## 7. Security Metadata Schema

```json
{
  "role": "Admin",
  "permissions": [
    "Customer.Read",
    "Customer.Write"
  ]
}
```

---

# Code Generator Design

---

## 1. Generator Architecture

```
Metadata Store
      ↓
Metadata Validator
      ↓
Model Normalizer
      ↓
Generator Pipeline
      ↓
Source Code Output
```

---

## 2. Generator Pipeline Stages

### Stage 1: Validation
- JSON schema validation
- Version compatibility checks

### Stage 2: Normalization
- Resolve relationships
- Expand defaults

### Stage 3: Template Mapping

| Metadata | Output |
|--------|--------|
| Entity | EF Core Entity |
| Page | Angular Component |
| Workflow | Workflow Definition |

---

## 3. Template Strategy

### Template Types
- Static templates (boilerplate)
- Dynamic templates (metadata-driven)

### Tools
- Scriban / Handlebars
- Roslyn for C# AST

---

## 4. Incremental Code Generation

### Key Rule
> **Never overwrite developer custom code**

### Strategy
- Generated folders are read-only
- Partial classes for extension
- Hook methods

---

## 5. Output Folder Structure

```
/src
  /Generated
    /Domain
    /Infrastructure
    /API
  /Custom
    /Extensions
```

---

## 6. Build & Compilation Flow

1. Metadata change detected
2. Regeneration triggered
3. Code compiled
4. Tests executed
5. Artifacts deployed

---

## 7. Error Handling & Diagnostics

- Generator logs
- Metadata error mapping
- Developer-friendly messages

---


## Step 7: Runtime Execution Platform

### Objective
Execute generated applications reliably.

### Components
- API Runtime
- UI Hosting
- Workflow Runtime

### Technology
- ASP.NET Core
- Elsa / Durable Functions

### Deliverables
- Runtime services
- Execution policies

---

## Step 8: Security & Identity

### Objective
Secure platform and generated apps.

### Features
- Authentication
- RBAC
- Policy-based authorization

### Technology
- OAuth2 / OpenID Connect
- Azure AD / IdentityServer

### Deliverables
- Security model
- Role/claim mapping

---

## Step 9: DevOps & CI/CD Enablement

### Objective
Automate build, test, and deployment.

### Components
- CI pipelines
- CD pipelines
- Environment promotion

### Technology
- Azure DevOps
- Docker

### Deliverables
- YAML pipelines
- Deployment scripts

---

## Step 10: Observability & Governance

### Objective
Ensure operational excellence.

### Features
- Logging
- Monitoring
- Audit trails

### Technology
- Application Insights
- Azure Monitor

### Deliverables
- Dashboards
- Alerting rules

---

## Step 11: Extensibility & Custom Code

### Objective
Allow developers to extend generated apps.

### Features
- Custom code hooks
- Plugin architecture

### Deliverables
- Extension contracts
- SDK documentation

---

## Step 12: Multi-Tenant Readiness (Future)

### Objective
Prepare platform for SaaS scale.

### Features
- Tenant isolation
- Configurable features

### Deliverables
- Tenant model
- Isolation strategy

---

## Step 13: Testing Strategy

### Objective
Ensure platform reliability.

### Test Types
- Unit tests
- Integration tests
- Generated app tests

### Deliverables
- Test automation framework

---

## Step 14: Documentation & Enablement

### Objective
Enable adoption.

### Artifacts
- Developer guides
- Admin manuals
- Sample apps

---

# Step 15: Roadmap & Evolution

### Phase 1: MVP (Current Focus)
- Visual Entity Designer
- Workflow Integration (Elsa)
- Basic Security (RBAC)
- Code Generation (ASP.NET Core + Angular)
- ZIP Export & Basic Publishing

### Phase 2: Visual UI & Logic (Next)
- **Visual Page Designer**: Drag-and-drop WYSIWYG editor for UI components.
- **Logic Flow Designer**: Visual modeling of client-side and server-side business logic.
- **State Management**: Automated client-side data caching and state synchronization.

### Phase 3: Enterprise & AI
- **AI-Assisted Development**: Gemini-powered scaffolding and logic copilot.
- **Integration Hub**: Pre-built connectors for popular SaaS and DB platforms.
- **Advanced ALM**: Zero-downtime deployments, version rollbacks, and environment staging.

### Phase 4: Ecosystem & Scale
- **Marketplace**: Community sharing of templates and connectors.
- **Multi-Experience**: Native mobile app generation (Flutter/React Native).
- **Multi-Tenancy**: SaaS-ready isolation for global scale.

---

# Appendix: Market Analysis & Enhancement Suggestions
For a detailed analysis of market competitors and specific feature enhancement suggestions, refer to [MARKET_ANALYSIS_ENHANCEMENT_SUGGESTIONS.md](./MARKET_ANALYSIS_ENHANCEMENT_SUGGESTIONS.md).

---

# Appendix

## Risks
- Generator complexity
- Performance overhead

## Mitigations
- Incremental generation
- Profiling & caching

