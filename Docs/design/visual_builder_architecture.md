# Visual Builder (Studio) Architecture

The **Platform Studio** is the client-side SPA (Angular) where developers build applications. It acts as a specialized JSON editor with a visual layer.

## 1. High-Level Architecture

```text
[ Browser ]
    |
    |-- [ Angular App (Studio) ]
          |-- [ State Store (NgRx/Akita) ] <--- Single Source of Truth
          |-- [ Canvas Engine (Konva/HTML) ] <--- Renders State
          |-- [ Property Panel ] <--- Mutates State
    |
[ API ]
    |-- [ MetadataController ] <--- Saves/Loads JSON
```

## 2. State Management Strategy

The "Application State" inside the Studio is a mirror of the Metadata JSON, but decorated with UI state (selected, hovering, invalid).

**Store Structure:**
```typescript
interface StudioState {
    project: {
        id: string;
        name: string;
    };
    currentModule: Module;
    // The active editor context
    activeContext: {
        type: 'Page' | 'Entity' | 'Flow';
        id: string;
        // The working copy of the artifact
        artifact: PageDefinition | EntityDefinition;
        // Undo/Redo stack for this specific artifact
        history: HistoryStack;
        // Selection
        selectedElementIds: string[];
    };
    ui: {
        isSidebarOpen: boolean;
        currentTool: 'Pointer' | 'DrawContainer' | 'DrawButton';
    }
}
```

## 3. The Canvas Renderer (Page Builder)

We use a **Hybrid Approach**:
1.  **Structure**: The DOM structure mimics the generated app's structure (DIVs, Inputs).
2.  **Interactivity**: A transparent Overlay (SVG/Canvas) handles the Drag-and-Drop events, Selection outlines, and Resizing handles.
    *   *Why?* Using raw DOM for the components ensures the "Design" looks exactly like "Runtime".

**Component Hosting:**
- Dynamic Component Loader (`ViewContainerRef`).
- Each widget in the palette maps to a `StudioWrapperComponent` which wraps the actual `RuntimeComponent` but suppresses its click events to allow selection.

## 4. Entity Designer (ERD)

For the Data Model, we use **React Flow** (wrapped in Angular) or **JointJS**.
- Nodes: Entities.
- Edges: Relationships.
- UX: Dragging a connection from "Customer" to "Order" opens the "Relationship Wizard" Modal.

## 5. Metadata Sync Protocol

1.  **Optimistic Updates**: UI updates immediately on Drop.
2.  **Debounced Save**: Every 2 seconds of inactivity, or on "Save" click, the JSON is sent to the backend.
3.  **Locking**: When opening an artifact, the frontend requests a pseudo-lock from the API to prevent concurrent edits by other devs.

## 6. Plugin System (Future)

To allow new widgets:
- `WidgetRegistry`: A singleton identifying available components.
- `metadata`: Describes valid props for the Property Panel.

```typescript
export const ButtonWidgetDef = {
    type: 'Button',
    component: ButtonComponent,
    props: {
        label: { type: 'text', default: 'Submit' },
        variant: { type: 'select', options: ['primary', 'secondary'] }
    }
}
```
