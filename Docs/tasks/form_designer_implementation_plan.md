# Form Designer Implementation Plan

This document outlines the step-by-step tasks required to implement the **Form Designer** feature as specified in `Docs/FORM_DESIGNER_GUIDE.md`.

## Phase 1: Platform Engine (Backend Core)
**Goal:** Enable the Platform Engine to understand Form Metadata and generate code from it.

### Task 1.1: Define Metadata Models
*   **File**: `src/Platform.Engine/Domain/Metadata/FormMetadata.cs`
*   **Models**:
    *   `FormMetadata`: Root object (Name, EntityTarget, Sections list).
    *   `FormSection`: Grouping (Title, FieldNames list).
    *   `FormField`: Controls (Name, Type, Label, ValidationRules).
    *   `FormLayout`: Enum (Vertical, Horizontal).

### Task 1.2: Create Scriban Templates
*   **Backend Template**: `src/Platform.Engine/Templates/Backend/Form.scriban`
    *   Generates: `public class {{Name}}Form { ... properties with DataAnnotations ... }`
*   **Frontend Template**: `src/Platform.Engine/Templates/Frontend/FormComponent.scriban`
    *   Generates: Angular Component (`.ts`, `.html`) using Reactive Forms.

### Task 1.3: Implement Form Generator
*   **File**: `src/Platform.Engine/Generators/FormGenerator.cs`
*   **Logic**:
    *   Implement `IGenerator` interface.
    *   Load `FormMetadata` from JSON.
    *   Render both Scriban templates.
    *   Output files to `Outputs/Source/Backend/Models` and `Outputs/Source/Frontend/src/app/forms`.

### Task 1.4: Update Build Orchestrator
*   **File**: `src/Platform.Engine/Services/BuildService.cs` (or `BuildController`)
*   **Logic**: Include `FormGenerator` in the build pipeline execution list.

---

## Phase 2: Platform Studio (Visual Designer)
**Goal:** Provide a UI for users to create and edit Form Metadata.

### Task 2.1: Scaffold Module
*   **Action**: Create `platform-studio/src/app/pages/form-designer` module.
*   **Routes**: Add `/forms` and `/forms/:id` to `app.routes.ts`.

### Task 2.2: Form Explorer (List View)
*   **Component**: `FormExplorerComponent`
*   **Features**:
    *   List existing forms (fetch from API).
    *   "New Form" Modal (Input Name, Select Target Entity).
    *   Delete Form action.

### Task 2.3: Form Canvas (Editor)
*   **Component**: `FormCanvasComponent`
*   **Features**:
    *   **Drag & Drop**: Use `@angular/cdk/drag-drop`.
    *   **Section Management**: Add/Remove Sections.
    *   **Field Management**: Drag fields from "Available Fields" (derived from Entity definition) into Sections.

### Task 2.4: Field Inspector (Properties)
*   **Component**: `FieldInspectorComponent`
*   **Features**:
    *   Select a field on Canvas -> Populate Inspector.
    *   Edit Label, Placeholder, Required Checkbox, Regex Pattern.
    *   Bindings: Changes reflect immediately on Canvas.

---

## Phase 3: Integration & Testing
**Goal:** Ensure end-to-end flow from Studio -> Engine -> Generated Code.

### Task 3.1: API Endpoints
*   **File**: `src/Platform.API/Controllers/FormController.cs`
*   **Endpoints**:
    *   `GET /api/forms`: List all forms.
    *   `GET /api/forms/{id}`: Get metadata.
    *   `POST /api/forms`: Save metadata.

### Task 3.2: Verification
*   Create a sample "PatientForm".
*   Run "Build Project".
*   Verify `PatientForm.cs` exists in `Output/Src/Backend`.
*   Verify `patient-form.component.ts` exists in `Output/Src/Frontend`.
