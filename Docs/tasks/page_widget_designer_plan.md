# Task: Implement Custom Widget & Generic Page Designer

## Phase 1: Engine Core (Backend)
- [ ] **Refactor Widget Models**:
    - [ ] Update `WidgetMetadata` in `EngineModels.cs` to replace rigid `Config` with dynamic `Dictionary<string, object> Properties`.
    - [ ] Introduce `WidgetDefinition` model (Entity for Custom Widgets).
- [ ] **Data Source Standardization**:
    - [ ] Define `WidgetDataSource` with `Provider` (Entity, API), `Source`, and `InputMapping`.
- [ ] **API Updates**:
    - [ ] Create `WidgetDefinitionController` (`CRUD`) to manage custom widgets.

## Phase 2: Custom Widget Designer (Frontend)
- [ ] **Scaffold Module**: Create `widget-designer` page in `platform-studio`.
- [ ] **Visual Builder**:
    - [ ] **Template Editor**: Simple HTML/CSS text area or block builder.
    - [ ] **Property Manager**: UI to add/edit inputs (Name, Type, Default).
    - [ ] **Preview**: Real-time rendering of the widget with mock data.
- [ ] **State Management**: Integrate with `ApiService` to save `ArtifactType.Widget`.

## Phase 3: Generic Page Designer Enhancements
- [ ] **Data Binding UI**:
    - [ ] In `PageDesigner` Property Inspector, replace fixed fields with a "Property Mapper".
    - [ ] Allow mapping Widget Props (e.g., `Title`) -> Entity Fields (e.g., `PatientName`).
- [ ] **Interaction Config**:
    - [ ] Add UI tab for "Events & Actions" (OnClick -> Navigate).

## Phase 4: Integration
- [ ] **Toolbox Update**: Load dynamic/custom widgets into the Page Designer toolbox.
- [ ] **Code Generation**: Update `DashboardGenerator` (or `PageGenerator`) to render custom widget templates and bind properties dynamically.
