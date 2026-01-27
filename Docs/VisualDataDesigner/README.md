# Visual Data Designer Documentation

This folder contains all documentation related to the Visual Data Designer system.

## 📚 Documentation Files

### 1. [ARCHITECTURE.md](./ARCHITECTURE.md)
Comprehensive architecture document covering:
- 10+ core scenarios (Simple Queries, Joins, Aggregations, Complex Filtering, etc.)
- 10+ edge cases (Circular Joins, Schema Mismatch, Performance, Security, etc.)
- Generic Wrapper API design
- Metadata schemas and models
- Execution engine architecture
- Output generator design

### 2. [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)
Detailed implementation plan with:
- Phase-by-phase breakdown (8 phases)
- File-by-file implementation details
- Completion status tracking (✅ Complete, ⏳ Pending, 🔄 Partial)
- Verification plan with test scenarios
- User review requirements
- Questions for user decisions

### 3. [ELSA_WORKFLOW_INTEGRATION_GUIDE.md](./ELSA_WORKFLOW_INTEGRATION_GUIDE.md)
Developer guide for Elsa Workflow integration:
- Custom Elsa activities (ExecuteDataQuery, GenerateReportOutput, UploadToStorage, NotifyUser)
- Workflow definitions with error handling
- Job tracking service implementation
- Monitoring and debugging strategies
- Scheduling and retry patterns
- Complete code examples

### 4. [BACKEND_IMPLEMENTATION_WALKTHROUGH.md](./BACKEND_IMPLEMENTATION_WALKTHROUGH.md)
Summary of completed backend implementation:
- Metadata models and interfaces
- Core services (DynamicQueryBuilder, EntityDataProvider, DataExecutionEngine)
- Output generators (CSV, Excel, PDF, JSON)
- NuGet packages installed
- Next steps and integration notes

## 🎯 Implementation Status

### ✅ Completed Phases
- **Phase 1**: Backend Foundation (Core Complete)
- **Phase 2**: Execution Engine (Complete)
- **Phase 5**: Output Generators (Complete)

### 🔄 Partial Completion
- **Phase 7**: Additional Providers (Static done, API/Workflow pending)
- **Phase 8**: Edge Cases & Polish (RLS & circular join detection done)

### ⏳ Pending Phases
- **Phase 3**: Frontend Visual Designer
- **Phase 4**: Advanced Query Features
- **Phase 6**: Long-Running Jobs with Elsa Workflow
- **Database Migrations**
- **Unit & Integration Tests**

## 🏗️ Architecture Overview

```
User Request → DataExecutionEngine
                    ↓
        [Quick Job] ← → [Long-Running Job]
             ↓                    ↓
    EntityDataProvider    Elsa Workflow
             ↓                    ↓
    DynamicQueryBuilder   Custom Activities
             ↓                    ↓
        LINQ Query          Chunked Processing
             ↓                    ↓
    Output Generators      Blob Storage
             ↓                    ↓
    CSV/Excel/PDF/JSON    User Notification
```

## 🔑 Key Design Decisions

1. **Elsa Workflow Integration**: Using existing Elsa infrastructure for long-running jobs instead of Hangfire
2. **ClosedXML for Excel**: Free, MIT-licensed library
3. **QuestPDF for PDF**: Community license
4. **Automatic RLS Injection**: Row-Level Security filters automatically applied to all queries
5. **Metadata-Driven**: All operations defined through metadata, not code
6. **Provider Pattern**: Abstraction supports Entity, API, Workflow, and Static data sources

## 📦 Dependencies

### NuGet Packages
- **ClosedXML** - Excel generation
- **QuestPDF** - PDF generation
- **Elsa.Core** - Workflow orchestration (already installed)
- **Microsoft.EntityFrameworkCore** - Data access

### Platform Components
- Elsa Workflow Engine
- Platform.Engine (core services)
- Platform.API (controllers)
- Platform Studio (frontend)

## 🚀 Quick Start

1. Review [ARCHITECTURE.md](./ARCHITECTURE.md) to understand the system design
2. Check [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) for current status
3. Follow [ELSA_WORKFLOW_INTEGRATION_GUIDE.md](./ELSA_WORKFLOW_INTEGRATION_GUIDE.md) for Elsa integration
4. Reference [BACKEND_IMPLEMENTATION_WALKTHROUGH.md](./BACKEND_IMPLEMENTATION_WALKTHROUGH.md) for completed work

## 📞 Support

For questions or clarifications, refer to the "Questions for User" section in the Implementation Plan.
