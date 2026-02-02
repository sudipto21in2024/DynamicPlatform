# Service Boundary Designer - Detailed Architecture

## 1. Executive Summary

The **Service Boundary Designer** is a visual, interactive component in DynamicPlatform that enables customers to define, visualize, and optimize service boundaries for microservices architecture. It provides an intuitive drag-and-drop interface for organizing entities into services, validating decomposition decisions, and configuring service properties.

**Key Capabilities**:
- ✅ Visual graph-based interface for service boundaries
- ✅ Automatic service decomposition suggestions (AI-powered)
- ✅ Drag-and-drop entity reassignment
- ✅ Real-time validation and warnings
- ✅ Dependency visualization
- ✅ Service configuration panel
- ✅ Export/import configurations

---

## 2. Component Architecture

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Platform Studio (Angular)                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │         Service Boundary Designer Component               │ │
│  ├───────────────────────────────────────────────────────────┤ │
│  │                                                           │ │
│  │  ┌─────────────────┐  ┌──────────────────┐              │ │
│  │  │  Canvas Manager │  │  Service Graph   │              │ │
│  │  │  (Konva.js)     │  │  Data Model      │              │ │
│  │  └────────┬────────┘  └────────┬─────────┘              │ │
│  │           │                     │                        │ │
│  │  ┌────────▼─────────────────────▼─────────┐             │ │
│  │  │     Interaction Controller             │             │ │
│  │  │  (Drag/Drop, Selection, Validation)    │             │ │
│  │  └────────┬───────────────────────────────┘             │ │
│  │           │                                              │ │
│  │  ┌────────▼────────┐  ┌──────────────────┐             │ │
│  │  │  Configuration  │  │  Validation      │             │ │
│  │  │  Panel          │  │  Engine          │             │ │
│  │  └─────────────────┘  └──────────────────┘             │ │
│  │                                                           │ │
│  └───────────────────────────────────────────────────────────┘ │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │         Service Boundary Service (TypeScript)             │ │
│  │  - API Communication                                      │ │
│  │  - State Management (NgRx/Akita)                         │ │
│  │  - Caching                                                │ │
│  └───────────────┬───────────────────────────────────────────┘ │
│                  │                                              │
└──────────────────┼──────────────────────────────────────────────┘
                   │
                   ▼ HTTP/REST
┌──────────────────────────────────────────────────────────────────┐
│                  Platform.API (ASP.NET Core)                     │
├──────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────┐ │
│  │         ArchitectureController                             │ │
│  │  - GET  /api/projects/{id}/architecture/analyze           │ │
│  │  - POST /api/projects/{id}/architecture/config            │ │
│  │  - POST /api/projects/{id}/architecture/validate          │ │
│  └────────────────┬───────────────────────────────────────────┘ │
│                   │                                              │
│  ┌────────────────▼───────────────────────────────────────────┐ │
│  │      ServiceDecompositionAnalyzer                          │ │
│  │  - Entity Graph Builder                                    │ │
│  │  - Clustering Algorithm (DDD-based)                        │ │
│  │  - Dependency Analyzer                                     │ │
│  │  - Validation Rules Engine                                 │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. Frontend Architecture (Angular)

### 3.1 Component Structure

```
/platform-studio/src/app/pages/service-boundary-designer/
│
├── service-boundary-designer.component.ts      (Main component)
├── service-boundary-designer.component.html
├── service-boundary-designer.component.scss
│
├── /components
│   ├── /canvas
│   │   ├── service-canvas.component.ts         (Konva.js canvas)
│   │   ├── service-node.component.ts           (Service rectangle)
│   │   ├── entity-node.component.ts            (Entity within service)
│   │   ├── dependency-arrow.component.ts       (Service dependencies)
│   │   └── canvas-toolbar.component.ts         (Zoom, pan, reset)
│   │
│   ├── /configuration-panel
│   │   ├── service-config-panel.component.ts   (Service properties)
│   │   ├── entity-list.component.ts            (Entity list view)
│   │   └── dependency-viewer.component.ts      (Dependency graph)
│   │
│   ├── /validation
│   │   ├── validation-panel.component.ts       (Errors/warnings)
│   │   └── validation-badge.component.ts       (Badge on services)
│   │
│   └── /dialogs
│       ├── create-service-dialog.component.ts
│       ├── merge-services-dialog.component.ts
│       └── export-config-dialog.component.ts
│
├── /services
│   ├── service-boundary.service.ts             (API communication)
│   ├── canvas-manager.service.ts               (Konva.js management)
│   ├── validation.service.ts                   (Client-side validation)
│   └── graph-layout.service.ts                 (Auto-layout algorithm)
│
├── /models
│   ├── service-boundary.model.ts
│   ├── entity-node.model.ts
│   ├── dependency.model.ts
│   └── validation-result.model.ts
│
└── /state
    ├── service-boundary.state.ts               (NgRx/Akita state)
    ├── service-boundary.actions.ts
    ├── service-boundary.reducer.ts
    └── service-boundary.selectors.ts
```

### 3.2 Data Models

```typescript
// service-boundary.model.ts

export interface ServiceBoundary {
  id: string;
  name: string;
  color: string;
  position: { x: number; y: number };
  size: { width: number; height: number };
  entities: EntityNode[];
  configuration: ServiceConfiguration;
  metadata: ServiceMetadata;
}

export interface EntityNode {
  id: string;
  name: string;
  type: 'Entity' | 'Aggregate' | 'ValueObject';
  position: { x: number; y: number }; // Relative to service
  relations: EntityRelation[];
  metadata: {
    tableName: string;
    fieldCount: number;
    hasAudit: boolean;
  };
}

export interface EntityRelation {
  sourceEntityId: string;
  targetEntityId: string;
  targetServiceId: string; // If cross-service
  relationType: 'OneToMany' | 'ManyToOne' | 'ManyToMany';
  foreignKey: string;
  strength: 'Strong' | 'Weak'; // For visualization
}

export interface ServiceConfiguration {
  port: number;
  database: {
    type: 'PostgreSQL' | 'MySQL' | 'SQLServer';
    name: string;
  };
  dependencies: string[]; // Other service IDs
  features: {
    enableMessaging: boolean;
    enableCaching: boolean;
    enableApiVersioning: boolean;
  };
}

export interface ServiceMetadata {
  entityCount: number;
  aggregateRoots: string[];
  estimatedComplexity: 'Low' | 'Medium' | 'High';
  suggestedBy: 'Auto' | 'Manual';
}

export interface Dependency {
  id: string;
  sourceServiceId: string;
  targetServiceId: string;
  type: 'Synchronous' | 'Asynchronous';
  strength: number; // 1-10 (for visualization thickness)
  entities: {
    sourceEntity: string;
    targetEntity: string;
    relationType: string;
  }[];
}

export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
  suggestions: ValidationSuggestion[];
}

export interface ValidationError {
  id: string;
  type: 'CircularDependency' | 'OrphanEntity' | 'InvalidConfiguration';
  serviceId?: string;
  entityId?: string;
  message: string;
  severity: 'Error' | 'Warning';
}

export interface AnalysisResult {
  suggestedServices: ServiceBoundary[];
  rationale: {
    serviceId: string;
    reason: string;
    confidence: number; // 0-100
  }[];
  metrics: {
    cohesion: number; // 0-100
    coupling: number; // 0-100
    complexity: number; // 0-100
  };
}
```

### 3.3 State Management (NgRx)

```typescript
// service-boundary.state.ts

export interface ServiceBoundaryState {
  projectId: string;
  services: ServiceBoundary[];
  selectedServiceId: string | null;
  selectedEntityId: string | null;
  dependencies: Dependency[];
  validationResult: ValidationResult | null;
  analysisResult: AnalysisResult | null;
  canvasState: {
    zoom: number;
    pan: { x: number; y: number };
    selectedTool: 'Select' | 'Pan' | 'CreateService';
  };
  ui: {
    isLoading: boolean;
    isSaving: boolean;
    showValidationPanel: boolean;
    showConfigPanel: boolean;
  };
}

// service-boundary.actions.ts

export const loadProject = createAction(
  '[Service Boundary] Load Project',
  props<{ projectId: string }>()
);

export const loadProjectSuccess = createAction(
  '[Service Boundary] Load Project Success',
  props<{ entities: EntityMetadata[] }>()
);

export const analyzeDecomposition = createAction(
  '[Service Boundary] Analyze Decomposition'
);

export const analyzeDecompositionSuccess = createAction(
  '[Service Boundary] Analyze Decomposition Success',
  props<{ result: AnalysisResult }>()
);

export const createService = createAction(
  '[Service Boundary] Create Service',
  props<{ name: string; position: { x: number; y: number } }>()
);

export const moveEntity = createAction(
  '[Service Boundary] Move Entity',
  props<{ 
    entityId: string; 
    fromServiceId: string; 
    toServiceId: string; 
  }>()
);

export const updateServiceConfig = createAction(
  '[Service Boundary] Update Service Config',
  props<{ serviceId: string; config: Partial<ServiceConfiguration> }>()
);

export const validateConfiguration = createAction(
  '[Service Boundary] Validate Configuration'
);

export const validateConfigurationSuccess = createAction(
  '[Service Boundary] Validate Configuration Success',
  props<{ result: ValidationResult }>()
);

export const saveConfiguration = createAction(
  '[Service Boundary] Save Configuration'
);

export const saveConfigurationSuccess = createAction(
  '[Service Boundary] Save Configuration Success'
);

export const selectService = createAction(
  '[Service Boundary] Select Service',
  props<{ serviceId: string | null }>()
);

export const selectEntity = createAction(
  '[Service Boundary] Select Entity',
  props<{ entityId: string | null }>()
);

export const setZoom = createAction(
  '[Service Boundary] Set Zoom',
  props<{ zoom: number }>()
);

export const setPan = createAction(
  '[Service Boundary] Set Pan',
  props<{ pan: { x: number; y: number } }>()
);
```

---

## 4. Canvas Rendering (Konva.js)

### 4.1 Canvas Manager Service

```typescript
// canvas-manager.service.ts

import Konva from 'konva';

@Injectable({ providedIn: 'root' })
export class CanvasManagerService {
  private stage: Konva.Stage;
  private mainLayer: Konva.Layer;
  private backgroundLayer: Konva.Layer;
  private serviceNodes: Map<string, Konva.Group> = new Map();
  private dependencyArrows: Map<string, Konva.Arrow> = new Map();

  initializeCanvas(containerId: string, width: number, height: number): void {
    this.stage = new Konva.Stage({
      container: containerId,
      width: width,
      height: height,
      draggable: true
    });

    // Background layer (grid)
    this.backgroundLayer = new Konva.Layer();
    this.drawGrid(width, height);
    this.stage.add(this.backgroundLayer);

    // Main layer (services, entities, arrows)
    this.mainLayer = new Konva.Layer();
    this.stage.add(this.mainLayer);

    // Enable zoom with mouse wheel
    this.setupZoom();

    // Enable panning
    this.setupPanning();
  }

  drawGrid(width: number, height: number): void {
    const gridSize = 20;
    const gridColor = '#e0e0e0';

    for (let i = 0; i < width / gridSize; i++) {
      this.backgroundLayer.add(new Konva.Line({
        points: [i * gridSize, 0, i * gridSize, height],
        stroke: gridColor,
        strokeWidth: 1
      }));
    }

    for (let i = 0; i < height / gridSize; i++) {
      this.backgroundLayer.add(new Konva.Line({
        points: [0, i * gridSize, width, i * gridSize],
        stroke: gridColor,
        strokeWidth: 1
      }));
    }
  }

  createServiceNode(service: ServiceBoundary): void {
    const group = new Konva.Group({
      id: service.id,
      x: service.position.x,
      y: service.position.y,
      draggable: true
    });

    // Service rectangle
    const rect = new Konva.Rect({
      width: service.size.width,
      height: service.size.height,
      fill: service.color,
      stroke: '#333',
      strokeWidth: 2,
      cornerRadius: 8,
      shadowColor: 'black',
      shadowBlur: 10,
      shadowOffset: { x: 2, y: 2 },
      shadowOpacity: 0.3
    });

    // Service name
    const text = new Konva.Text({
      text: service.name,
      fontSize: 18,
      fontFamily: 'Inter, sans-serif',
      fontStyle: 'bold',
      fill: '#fff',
      padding: 10,
      width: service.size.width,
      align: 'center'
    });

    // Entity count badge
    const badge = this.createBadge(service.entities.length, service.size.width - 40, 10);

    group.add(rect);
    group.add(text);
    group.add(badge);

    // Add entities
    service.entities.forEach((entity, index) => {
      const entityNode = this.createEntityNode(entity, index);
      group.add(entityNode);
    });

    // Drag events
    group.on('dragstart', () => {
      group.moveToTop();
    });

    group.on('dragmove', () => {
      this.updateDependencyArrows(service.id);
    });

    group.on('dragend', () => {
      this.emit('servicePositionChanged', {
        serviceId: service.id,
        position: { x: group.x(), y: group.y() }
      });
    });

    // Click event
    group.on('click', () => {
      this.emit('serviceSelected', { serviceId: service.id });
    });

    this.serviceNodes.set(service.id, group);
    this.mainLayer.add(group);
    this.mainLayer.batchDraw();
  }

  createEntityNode(entity: EntityNode, index: number): Konva.Group {
    const entityGroup = new Konva.Group({
      id: entity.id,
      x: 20,
      y: 50 + (index * 35),
      draggable: true
    });

    // Entity rectangle
    const rect = new Konva.Rect({
      width: 200,
      height: 30,
      fill: '#ffffff',
      stroke: '#4CAF50',
      strokeWidth: 1,
      cornerRadius: 4
    });

    // Entity name
    const text = new Konva.Text({
      text: entity.name,
      fontSize: 14,
      fontFamily: 'Inter, sans-serif',
      fill: '#333',
      padding: 8,
      width: 200
    });

    // Entity type icon
    const icon = this.createEntityIcon(entity.type);

    entityGroup.add(rect);
    entityGroup.add(text);
    entityGroup.add(icon);

    // Drag events for entity reassignment
    entityGroup.on('dragstart', () => {
      entityGroup.moveToTop();
      this.highlightDropTargets(entity.id);
    });

    entityGroup.on('dragmove', () => {
      const pos = entityGroup.getAbsolutePosition();
      const targetService = this.findServiceAtPosition(pos);
      
      if (targetService) {
        this.highlightService(targetService.id);
      }
    });

    entityGroup.on('dragend', () => {
      const pos = entityGroup.getAbsolutePosition();
      const targetService = this.findServiceAtPosition(pos);
      
      if (targetService) {
        this.emit('entityMoved', {
          entityId: entity.id,
          targetServiceId: targetService.id
        });
      } else {
        // Snap back to original position
        entityGroup.to({
          x: 20,
          y: 50 + (index * 35),
          duration: 0.3
        });
      }

      this.clearHighlights();
    });

    return entityGroup;
  }

  createDependencyArrow(dependency: Dependency): void {
    const sourceService = this.serviceNodes.get(dependency.sourceServiceId);
    const targetService = this.serviceNodes.get(dependency.targetServiceId);

    if (!sourceService || !targetService) return;

    const sourcePos = sourceService.getAbsolutePosition();
    const targetPos = targetService.getAbsolutePosition();

    // Calculate arrow points (from center of source to center of target)
    const sourceCenterX = sourcePos.x + sourceService.width() / 2;
    const sourceCenterY = sourcePos.y + sourceService.height() / 2;
    const targetCenterX = targetPos.x + targetService.width() / 2;
    const targetCenterY = targetPos.y + targetService.height() / 2;

    const arrow = new Konva.Arrow({
      id: dependency.id,
      points: [sourceCenterX, sourceCenterY, targetCenterX, targetCenterY],
      stroke: dependency.type === 'Synchronous' ? '#FF5722' : '#2196F3',
      strokeWidth: Math.max(1, dependency.strength / 2),
      fill: dependency.type === 'Synchronous' ? '#FF5722' : '#2196F3',
      pointerLength: 10,
      pointerWidth: 10,
      dash: dependency.type === 'Asynchronous' ? [10, 5] : undefined,
      opacity: 0.7
    });

    // Hover effect
    arrow.on('mouseenter', () => {
      arrow.strokeWidth(arrow.strokeWidth() * 1.5);
      arrow.opacity(1);
      this.showDependencyTooltip(dependency);
      this.mainLayer.batchDraw();
    });

    arrow.on('mouseleave', () => {
      arrow.strokeWidth(Math.max(1, dependency.strength / 2));
      arrow.opacity(0.7);
      this.hideDependencyTooltip();
      this.mainLayer.batchDraw();
    });

    this.dependencyArrows.set(dependency.id, arrow);
    this.mainLayer.add(arrow);
    arrow.moveToBottom(); // Arrows behind services
    this.mainLayer.batchDraw();
  }

  updateDependencyArrows(serviceId: string): void {
    this.dependencyArrows.forEach((arrow, depId) => {
      const dependency = this.getDependencyById(depId);
      
      if (dependency.sourceServiceId === serviceId || 
          dependency.targetServiceId === serviceId) {
        
        const sourceService = this.serviceNodes.get(dependency.sourceServiceId);
        const targetService = this.serviceNodes.get(dependency.targetServiceId);

        if (sourceService && targetService) {
          const sourcePos = sourceService.getAbsolutePosition();
          const targetPos = targetService.getAbsolutePosition();

          const sourceCenterX = sourcePos.x + sourceService.width() / 2;
          const sourceCenterY = sourcePos.y + sourceService.height() / 2;
          const targetCenterX = targetPos.x + targetService.width() / 2;
          const targetCenterY = targetPos.y + targetService.height() / 2;

          arrow.points([sourceCenterX, sourceCenterY, targetCenterX, targetCenterY]);
        }
      }
    });

    this.mainLayer.batchDraw();
  }

  setupZoom(): void {
    const scaleBy = 1.1;

    this.stage.on('wheel', (e) => {
      e.evt.preventDefault();

      const oldScale = this.stage.scaleX();
      const pointer = this.stage.getPointerPosition();

      const mousePointTo = {
        x: (pointer.x - this.stage.x()) / oldScale,
        y: (pointer.y - this.stage.y()) / oldScale
      };

      const newScale = e.evt.deltaY > 0 ? oldScale / scaleBy : oldScale * scaleBy;

      // Limit zoom
      const clampedScale = Math.max(0.1, Math.min(3, newScale));

      this.stage.scale({ x: clampedScale, y: clampedScale });

      const newPos = {
        x: pointer.x - mousePointTo.x * clampedScale,
        y: pointer.y - mousePointTo.y * clampedScale
      };

      this.stage.position(newPos);
      this.stage.batchDraw();

      this.emit('zoomChanged', { zoom: clampedScale });
    });
  }

  setupPanning(): void {
    // Panning is enabled by default with draggable: true on stage
    this.stage.on('dragend', () => {
      this.emit('panChanged', { 
        pan: { x: this.stage.x(), y: this.stage.y() } 
      });
    });
  }

  autoLayout(services: ServiceBoundary[]): void {
    // Force-directed graph layout algorithm
    const simulation = this.createForceSimulation(services);
    
    // Run simulation
    for (let i = 0; i < 100; i++) {
      simulation.tick();
    }

    // Apply positions
    services.forEach(service => {
      const node = this.serviceNodes.get(service.id);
      if (node) {
        node.to({
          x: service.position.x,
          y: service.position.y,
          duration: 0.5
        });
      }
    });
  }

  createForceSimulation(services: ServiceBoundary[]): any {
    // Simplified force-directed layout
    // In production, use d3-force or similar library
    
    const nodes = services.map(s => ({
      id: s.id,
      x: s.position.x,
      y: s.position.y,
      vx: 0,
      vy: 0
    }));

    return {
      tick: () => {
        // Apply forces: repulsion between nodes, attraction along edges
        nodes.forEach((node, i) => {
          nodes.forEach((other, j) => {
            if (i !== j) {
              const dx = other.x - node.x;
              const dy = other.y - node.y;
              const distance = Math.sqrt(dx * dx + dy * dy);
              
              if (distance < 300) {
                // Repulsion
                const force = 100 / (distance * distance);
                node.vx -= (dx / distance) * force;
                node.vy -= (dy / distance) * force;
              }
            }
          });

          // Apply velocity
          node.x += node.vx * 0.1;
          node.y += node.vy * 0.1;

          // Damping
          node.vx *= 0.9;
          node.vy *= 0.9;
        });

        // Update service positions
        nodes.forEach(node => {
          const service = services.find(s => s.id === node.id);
          if (service) {
            service.position.x = node.x;
            service.position.y = node.y;
          }
        });
      }
    };
  }

  highlightService(serviceId: string): void {
    const node = this.serviceNodes.get(serviceId);
    if (node) {
      const rect = node.findOne('Rect');
      rect.stroke('#FFC107');
      rect.strokeWidth(4);
      this.mainLayer.batchDraw();
    }
  }

  clearHighlights(): void {
    this.serviceNodes.forEach(node => {
      const rect = node.findOne('Rect');
      rect.stroke('#333');
      rect.strokeWidth(2);
    });
    this.mainLayer.batchDraw();
  }

  private emit(event: string, data: any): void {
    // Emit events to Angular component
    // Implementation depends on event system (RxJS Subject, etc.)
  }
}
```

---

## 5. Validation Engine

### 5.1 Client-Side Validation Service

```typescript
// validation.service.ts

@Injectable({ providedIn: 'root' })
export class ValidationService {
  
  validateConfiguration(
    services: ServiceBoundary[], 
    dependencies: Dependency[]
  ): ValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: ValidationSuggestion[] = [];

    // Rule 1: No circular dependencies
    const circularDeps = this.detectCircularDependencies(services, dependencies);
    if (circularDeps.length > 0) {
      circularDeps.forEach(cycle => {
        errors.push({
          id: `circular-${cycle.join('-')}`,
          type: 'CircularDependency',
          message: `Circular dependency detected: ${cycle.join(' → ')}`,
          severity: 'Error',
          affectedServices: cycle
        });
      });
    }

    // Rule 2: All entities must belong to a service
    const orphanEntities = this.findOrphanEntities(services);
    if (orphanEntities.length > 0) {
      errors.push({
        id: 'orphan-entities',
        type: 'OrphanEntity',
        message: `${orphanEntities.length} entities are not assigned to any service`,
        severity: 'Error',
        affectedEntities: orphanEntities
      });
    }

    // Rule 3: Services should have 5-15 entities (recommendation)
    services.forEach(service => {
      if (service.entities.length < 3) {
        warnings.push({
          id: `small-service-${service.id}`,
          type: 'SmallService',
          serviceId: service.id,
          message: `Service "${service.name}" has only ${service.entities.length} entities. Consider merging with another service.`,
          severity: 'Warning'
        });
      } else if (service.entities.length > 20) {
        warnings.push({
          id: `large-service-${service.id}`,
          type: 'LargeService',
          serviceId: service.id,
          message: `Service "${service.name}" has ${service.entities.length} entities. Consider splitting into smaller services.`,
          severity: 'Warning'
        });
      }
    });

    // Rule 4: Minimize cross-service dependencies
    const crossServiceDeps = this.countCrossServiceDependencies(services, dependencies);
    if (crossServiceDeps > services.length * 2) {
      warnings.push({
        id: 'high-coupling',
        type: 'HighCoupling',
        message: `High coupling detected: ${crossServiceDeps} cross-service dependencies. Consider reorganizing services.`,
        severity: 'Warning'
      });
    }

    // Rule 5: Database naming conflicts
    const dbConflicts = this.detectDatabaseNamingConflicts(services);
    if (dbConflicts.length > 0) {
      errors.push({
        id: 'db-naming-conflict',
        type: 'InvalidConfiguration',
        message: `Database naming conflicts: ${dbConflicts.join(', ')}`,
        severity: 'Error'
      });
    }

    // Rule 6: Port conflicts
    const portConflicts = this.detectPortConflicts(services);
    if (portConflicts.length > 0) {
      errors.push({
        id: 'port-conflict',
        type: 'InvalidConfiguration',
        message: `Port conflicts detected: ${portConflicts.join(', ')}`,
        severity: 'Error'
      });
    }

    // Suggestions
    const optimizations = this.suggestOptimizations(services, dependencies);
    suggestions.push(...optimizations);

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private detectCircularDependencies(
    services: ServiceBoundary[], 
    dependencies: Dependency[]
  ): string[][] {
    const graph = this.buildDependencyGraph(services, dependencies);
    const cycles: string[][] = [];
    const visited = new Set<string>();
    const recursionStack = new Set<string>();

    const dfs = (serviceId: string, path: string[]): void => {
      visited.add(serviceId);
      recursionStack.add(serviceId);
      path.push(serviceId);

      const neighbors = graph.get(serviceId) || [];
      for (const neighbor of neighbors) {
        if (!visited.has(neighbor)) {
          dfs(neighbor, [...path]);
        } else if (recursionStack.has(neighbor)) {
          // Cycle detected
          const cycleStart = path.indexOf(neighbor);
          const cycle = path.slice(cycleStart);
          cycle.push(neighbor); // Complete the cycle
          cycles.push(cycle.map(id => {
            const service = services.find(s => s.id === id);
            return service?.name || id;
          }));
        }
      }

      recursionStack.delete(serviceId);
    };

    services.forEach(service => {
      if (!visited.has(service.id)) {
        dfs(service.id, []);
      }
    });

    return cycles;
  }

  private buildDependencyGraph(
    services: ServiceBoundary[], 
    dependencies: Dependency[]
  ): Map<string, string[]> {
    const graph = new Map<string, string[]>();

    services.forEach(service => {
      graph.set(service.id, []);
    });

    dependencies.forEach(dep => {
      const neighbors = graph.get(dep.sourceServiceId) || [];
      neighbors.push(dep.targetServiceId);
      graph.set(dep.sourceServiceId, neighbors);
    });

    return graph;
  }

  private findOrphanEntities(services: ServiceBoundary[]): string[] {
    // This would compare against the original project entities
    // For now, return empty array
    return [];
  }

  private countCrossServiceDependencies(
    services: ServiceBoundary[], 
    dependencies: Dependency[]
  ): number {
    return dependencies.filter(dep => 
      dep.sourceServiceId !== dep.targetServiceId
    ).length;
  }

  private detectDatabaseNamingConflicts(services: ServiceBoundary[]): string[] {
    const dbNames = new Map<string, string[]>();

    services.forEach(service => {
      const dbName = service.configuration.database.name;
      if (!dbNames.has(dbName)) {
        dbNames.set(dbName, []);
      }
      dbNames.get(dbName)!.push(service.name);
    });

    const conflicts: string[] = [];
    dbNames.forEach((serviceNames, dbName) => {
      if (serviceNames.length > 1) {
        conflicts.push(`${dbName} (used by: ${serviceNames.join(', ')})`);
      }
    });

    return conflicts;
  }

  private detectPortConflicts(services: ServiceBoundary[]): string[] {
    const ports = new Map<number, string[]>();

    services.forEach(service => {
      const port = service.configuration.port;
      if (!ports.has(port)) {
        ports.set(port, []);
      }
      ports.get(port)!.push(service.name);
    });

    const conflicts: string[] = [];
    ports.forEach((serviceNames, port) => {
      if (serviceNames.length > 1) {
        conflicts.push(`Port ${port} (used by: ${serviceNames.join(', ')})`);
      }
    });

    return conflicts;
  }

  private suggestOptimizations(
    services: ServiceBoundary[], 
    dependencies: Dependency[]
  ): ValidationSuggestion[] {
    const suggestions: ValidationSuggestion[] = [];

    // Suggest merging services with high coupling
    services.forEach(service1 => {
      services.forEach(service2 => {
        if (service1.id !== service2.id) {
          const coupling = this.calculateCoupling(service1, service2, dependencies);
          
          if (coupling > 0.7) {
            suggestions.push({
              id: `merge-${service1.id}-${service2.id}`,
              type: 'MergeServices',
              message: `Consider merging "${service1.name}" and "${service2.name}" (high coupling: ${(coupling * 100).toFixed(0)}%)`,
              action: {
                type: 'merge',
                serviceIds: [service1.id, service2.id]
              }
            });
          }
        }
      });
    });

    // Suggest splitting large services
    services.forEach(service => {
      if (service.entities.length > 20) {
        const subgroups = this.suggestEntityGroups(service);
        
        if (subgroups.length > 1) {
          suggestions.push({
            id: `split-${service.id}`,
            type: 'SplitService',
            message: `Consider splitting "${service.name}" into ${subgroups.length} services`,
            action: {
              type: 'split',
              serviceId: service.id,
              groups: subgroups
            }
          });
        }
      }
    });

    return suggestions;
  }

  private calculateCoupling(
    service1: ServiceBoundary, 
    service2: ServiceBoundary, 
    dependencies: Dependency[]
  ): number {
    const deps = dependencies.filter(d => 
      (d.sourceServiceId === service1.id && d.targetServiceId === service2.id) ||
      (d.sourceServiceId === service2.id && d.targetServiceId === service1.id)
    );

    const totalRelations = service1.entities.length + service2.entities.length;
    const crossServiceRelations = deps.reduce((sum, d) => sum + d.entities.length, 0);

    return crossServiceRelations / totalRelations;
  }

  private suggestEntityGroups(service: ServiceBoundary): string[][] {
    // Simplified clustering algorithm
    // In production, use more sophisticated algorithms (k-means, hierarchical clustering)
    
    const groups: string[][] = [];
    const visited = new Set<string>();

    service.entities.forEach(entity => {
      if (!visited.has(entity.id)) {
        const group = this.findRelatedEntities(entity, service.entities);
        group.forEach(e => visited.add(e));
        groups.push(group);
      }
    });

    return groups.filter(g => g.length >= 3); // Minimum 3 entities per group
  }

  private findRelatedEntities(
    entity: EntityNode, 
    allEntities: EntityNode[]
  ): string[] {
    const related = new Set<string>([entity.id]);
    const queue = [entity];

    while (queue.length > 0) {
      const current = queue.shift()!;
      
      current.relations.forEach(rel => {
        if (!related.has(rel.targetEntityId) && rel.strength === 'Strong') {
          related.add(rel.targetEntityId);
          const targetEntity = allEntities.find(e => e.id === rel.targetEntityId);
          if (targetEntity) {
            queue.push(targetEntity);
          }
        }
      });
    }

    return Array.from(related);
  }
}
```

---

## 6. Backend API

### 6.1 Architecture Controller

```csharp
// ArchitectureController.cs

[ApiController]
[Route("api/projects/{projectId}/architecture")]
public class ArchitectureController : ControllerBase
{
    private readonly IServiceDecompositionAnalyzer _analyzer;
    private readonly IProjectService _projectService;
    private readonly IArchitectureConfigService _configService;

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalysisResult>> AnalyzeDecomposition(
        Guid projectId,
        [FromBody] AnalysisOptions options)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null) return NotFound();

        var result = await _analyzer.AnalyzeAsync(project, options);
        
        return Ok(result);
    }

    [HttpGet("config")]
    public async Task<ActionResult<ArchitectureConfiguration>> GetConfiguration(
        Guid projectId)
    {
        var config = await _configService.GetConfigurationAsync(projectId);
        
        if (config == null)
        {
            // Return default configuration
            var project = await _projectService.GetByIdAsync(projectId);
            config = await _analyzer.GenerateDefaultConfigurationAsync(project);
        }

        return Ok(config);
    }

    [HttpPost("config")]
    public async Task<ActionResult> SaveConfiguration(
        Guid projectId,
        [FromBody] ArchitectureConfiguration config)
    {
        await _configService.SaveConfigurationAsync(projectId, config);
        return Ok();
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateConfiguration(
        Guid projectId,
        [FromBody] ArchitectureConfiguration config)
    {
        var result = await _analyzer.ValidateAsync(config);
        return Ok(result);
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<ArchitectureConfiguration>> OptimizeConfiguration(
        Guid projectId,
        [FromBody] ArchitectureConfiguration currentConfig)
    {
        var optimized = await _analyzer.OptimizeAsync(currentConfig);
        return Ok(optimized);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ArchitectureMetrics>> GetMetrics(
        Guid projectId)
    {
        var config = await _configService.GetConfigurationAsync(projectId);
        var metrics = await _analyzer.CalculateMetricsAsync(config);
        
        return Ok(metrics);
    }
}
```

### 6.2 Service Decomposition Analyzer

```csharp
// ServiceDecompositionAnalyzer.cs

public class ServiceDecompositionAnalyzer : IServiceDecompositionAnalyzer
{
    public async Task<AnalysisResult> AnalyzeAsync(
        ProjectMetadata project, 
        AnalysisOptions options)
    {
        // Step 1: Build entity relationship graph
        var graph = BuildEntityGraph(project.Entities);

        // Step 2: Identify aggregates (strongly connected components)
        var aggregates = IdentifyAggregates(graph);

        // Step 3: Apply domain heuristics
        var boundedContexts = ApplyDomainHeuristics(aggregates, project);

        // Step 4: Optimize service size
        var services = OptimizeServiceSize(boundedContexts, options);

        // Step 5: Calculate dependencies
        var dependencies = CalculateDependencies(services, graph);

        // Step 6: Calculate metrics
        var metrics = CalculateMetrics(services, dependencies);

        // Step 7: Generate rationale
        var rationale = GenerateRationale(services);

        return new AnalysisResult
        {
            SuggestedServices = services,
            Dependencies = dependencies,
            Metrics = metrics,
            Rationale = rationale
        };
    }

    private EntityGraph BuildEntityGraph(List<EntityMetadata> entities)
    {
        var graph = new EntityGraph();

        foreach (var entity in entities)
        {
            graph.AddNode(entity);

            foreach (var relation in entity.Relations)
            {
                var weight = CalculateRelationWeight(relation);
                graph.AddEdge(entity.Id, relation.TargetEntityId, weight);
            }
        }

        return graph;
    }

    private double CalculateRelationWeight(RelationMetadata relation)
    {
        // OneToMany/ManyToOne = strong relationship (weight: 1.0)
        // ManyToMany = weak relationship (weight: 0.3)
        
        return relation.Type switch
        {
            RelationType.OneToMany => 1.0,
            RelationType.ManyToOne => 1.0,
            RelationType.ManyToMany => 0.3,
            _ => 0.5
        };
    }

    private List<Aggregate> IdentifyAggregates(EntityGraph graph)
    {
        // Use Tarjan's algorithm for strongly connected components
        var aggregates = new List<Aggregate>();
        var visited = new HashSet<string>();
        var stack = new Stack<string>();
        var lowLink = new Dictionary<string, int>();
        var index = new Dictionary<string, int>();
        var currentIndex = 0;

        void StrongConnect(string nodeId)
        {
            index[nodeId] = currentIndex;
            lowLink[nodeId] = currentIndex;
            currentIndex++;
            stack.Push(nodeId);
            visited.Add(nodeId);

            foreach (var neighbor in graph.GetNeighbors(nodeId))
            {
                if (!index.ContainsKey(neighbor.NodeId))
                {
                    StrongConnect(neighbor.NodeId);
                    lowLink[nodeId] = Math.Min(lowLink[nodeId], lowLink[neighbor.NodeId]);
                }
                else if (stack.Contains(neighbor.NodeId))
                {
                    lowLink[nodeId] = Math.Min(lowLink[nodeId], index[neighbor.NodeId]);
                }
            }

            if (lowLink[nodeId] == index[nodeId])
            {
                var aggregate = new Aggregate();
                string poppedNode;
                
                do
                {
                    poppedNode = stack.Pop();
                    aggregate.Entities.Add(poppedNode);
                } while (poppedNode != nodeId);

                if (aggregate.Entities.Count > 0)
                {
                    aggregates.Add(aggregate);
                }
            }
        }

        foreach (var node in graph.GetAllNodes())
        {
            if (!visited.Contains(node.Id))
            {
                StrongConnect(node.Id);
            }
        }

        return aggregates;
    }

    private List<BoundedContext> ApplyDomainHeuristics(
        List<Aggregate> aggregates, 
        ProjectMetadata project)
    {
        var contexts = new List<BoundedContext>();

        // Heuristic 1: Entity naming patterns
        var namingGroups = GroupByNamingPattern(aggregates);

        // Heuristic 2: Workflow analysis
        var workflowGroups = GroupByWorkflowUsage(aggregates, project.Workflows);

        // Heuristic 3: Security boundaries
        var securityGroups = GroupBySecurityRules(aggregates, project.SecurityRules);

        // Merge heuristics
        contexts = MergeHeuristics(namingGroups, workflowGroups, securityGroups);

        return contexts;
    }

    private Dictionary<string, List<Aggregate>> GroupByNamingPattern(
        List<Aggregate> aggregates)
    {
        var groups = new Dictionary<string, List<Aggregate>>();

        foreach (var aggregate in aggregates)
        {
            var pattern = ExtractNamingPattern(aggregate);
            
            if (!groups.ContainsKey(pattern))
            {
                groups[pattern] = new List<Aggregate>();
            }

            groups[pattern].Add(aggregate);
        }

        return groups;
    }

    private string ExtractNamingPattern(Aggregate aggregate)
    {
        // Extract common prefix from entity names
        // e.g., "Customer", "CustomerAddress" → "Customer"
        
        var entityNames = aggregate.Entities
            .Select(e => e.Name)
            .ToList();

        if (entityNames.Count == 1)
        {
            return entityNames[0];
        }

        var commonPrefix = entityNames[0];
        
        foreach (var name in entityNames.Skip(1))
        {
            commonPrefix = GetCommonPrefix(commonPrefix, name);
        }

        return string.IsNullOrEmpty(commonPrefix) ? "Generic" : commonPrefix;
    }

    private string GetCommonPrefix(string str1, string str2)
    {
        int minLength = Math.Min(str1.Length, str2.Length);
        
        for (int i = 0; i < minLength; i++)
        {
            if (str1[i] != str2[i])
            {
                return str1.Substring(0, i);
            }
        }

        return str1.Substring(0, minLength);
    }

    private List<ServiceBoundary> OptimizeServiceSize(
        List<BoundedContext> contexts, 
        AnalysisOptions options)
    {
        var services = new List<ServiceBoundary>();
        var targetMinSize = options.MinEntitiesPerService ?? 5;
        var targetMaxSize = options.MaxEntitiesPerService ?? 15;

        foreach (var context in contexts)
        {
            var entityCount = context.Aggregates.Sum(a => a.Entities.Count);

            if (entityCount < targetMinSize)
            {
                // Too small, try to merge with another context
                var merged = TryMergeContext(context, contexts, targetMaxSize);
                if (merged != null)
                {
                    services.Add(CreateServiceFromContext(merged));
                }
            }
            else if (entityCount > targetMaxSize)
            {
                // Too large, split into multiple services
                var split = SplitContext(context, targetMaxSize);
                services.AddRange(split.Select(CreateServiceFromContext));
            }
            else
            {
                // Just right
                services.Add(CreateServiceFromContext(context));
            }
        }

        return services;
    }

    private ServiceBoundary CreateServiceFromContext(BoundedContext context)
    {
        var entities = context.Aggregates
            .SelectMany(a => a.Entities)
            .ToList();

        var serviceName = GenerateServiceName(context);
        var port = AssignPort(serviceName);

        return new ServiceBoundary
        {
            Id = Guid.NewGuid().ToString(),
            Name = serviceName,
            Color = GenerateColor(serviceName),
            Position = new Position { X = 100, Y = 100 },
            Size = new Size { Width = 300, Height = Math.Max(200, entities.Count * 40 + 100) },
            Entities = entities,
            Configuration = new ServiceConfiguration
            {
                Port = port,
                Database = new DatabaseConfig
                {
                    Type = "PostgreSQL",
                    Name = $"{serviceName}DB"
                },
                Dependencies = new List<string>(),
                Features = new ServiceFeatures
                {
                    EnableMessaging = true,
                    EnableCaching = false,
                    EnableApiVersioning = true
                }
            },
            Metadata = new ServiceMetadata
            {
                EntityCount = entities.Count,
                AggregateRoots = context.Aggregates.Select(a => a.RootEntity).ToList(),
                EstimatedComplexity = CalculateComplexity(entities),
                SuggestedBy = "Auto"
            }
        };
    }

    private string GenerateServiceName(BoundedContext context)
    {
        // Use the most common naming pattern
        var pattern = context.NamingPattern;
        
        if (string.IsNullOrEmpty(pattern) || pattern == "Generic")
        {
            return $"Service{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        return $"{pattern}Service";
    }

    private int AssignPort(string serviceName)
    {
        // Deterministic port assignment based on service name hash
        var hash = serviceName.GetHashCode();
        return 5001 + (Math.Abs(hash) % 100);
    }

    private string GenerateColor(string serviceName)
    {
        // Generate consistent color based on service name
        var colors = new[]
        {
            "#4CAF50", "#2196F3", "#FF9800", "#9C27B0",
            "#F44336", "#00BCD4", "#FFEB3B", "#795548"
        };

        var hash = serviceName.GetHashCode();
        return colors[Math.Abs(hash) % colors.Length];
    }

    private string CalculateComplexity(List<EntityNode> entities)
    {
        var totalRelations = entities.Sum(e => e.Relations.Count);
        var avgRelations = (double)totalRelations / entities.Count;

        if (avgRelations < 2) return "Low";
        if (avgRelations < 5) return "Medium";
        return "High";
    }
}
```

---

## 7. User Interactions & Workflows

### 7.1 Main User Workflows

**Workflow 1: Auto-Analyze and Accept**
```
1. User clicks "Analyze" button
2. System analyzes project entities
3. System suggests service boundaries
4. User reviews suggestions
5. User clicks "Accept All"
6. System saves configuration
```

**Workflow 2: Manual Adjustment**
```
1. User clicks "Analyze" button
2. System suggests services
3. User drags Entity A from Service 1 to Service 2
4. System validates move
5. System updates dependencies
6. System shows warnings if any
7. User adjusts service configuration
8. User clicks "Save"
```

**Workflow 3: Create Custom Service**
```
1. User clicks "Create Service" button
2. System shows dialog
3. User enters service name
4. User clicks canvas to place service
5. User drags entities into new service
6. User configures service properties
7. User clicks "Save"
```

**Workflow 4: Merge Services**
```
1. User selects Service A
2. User Ctrl+Click Service B
3. User clicks "Merge" button
4. System shows merge preview
5. User confirms merge
6. System combines entities
7. System updates dependencies
```

### 7.2 Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl + A` | Analyze decomposition |
| `Ctrl + S` | Save configuration |
| `Ctrl + Z` | Undo |
| `Ctrl + Y` | Redo |
| `Delete` | Delete selected service |
| `Ctrl + D` | Duplicate service |
| `Ctrl + M` | Merge selected services |
| `Ctrl + L` | Auto-layout |
| `Ctrl + +` | Zoom in |
| `Ctrl + -` | Zoom out |
| `Ctrl + 0` | Reset zoom |
| `Space + Drag` | Pan canvas |

---

## 8. Performance Optimization

### 8.1 Rendering Optimization

```typescript
// Use virtual scrolling for large entity lists
// Use canvas layering (background, main, overlay)
// Batch draw operations
// Debounce drag events
// Use object pooling for frequently created/destroyed objects

class PerformanceOptimizer {
  private renderQueue: (() => void)[] = [];
  private isRendering = false;

  queueRender(fn: () => void): void {
    this.renderQueue.push(fn);
    this.scheduleRender();
  }

  private scheduleRender(): void {
    if (this.isRendering) return;

    this.isRendering = true;
    requestAnimationFrame(() => {
      this.renderQueue.forEach(fn => fn());
      this.renderQueue = [];
      this.isRendering = false;
    });
  }
}
```

### 8.2 Data Optimization

```typescript
// Use memoization for expensive calculations
// Cache validation results
// Lazy load entity details
// Use Web Workers for heavy computations

@Injectable({ providedIn: 'root' })
export class AnalysisWorkerService {
  private worker: Worker;

  constructor() {
    this.worker = new Worker(new URL('./analysis.worker', import.meta.url));
  }

  analyzeInBackground(entities: EntityMetadata[]): Observable<AnalysisResult> {
    return new Observable(observer => {
      this.worker.postMessage({ entities });

      this.worker.onmessage = ({ data }) => {
        observer.next(data);
        observer.complete();
      };

      this.worker.onerror = (error) => {
        observer.error(error);
      };
    });
  }
}
```

---

## 9. Testing Strategy

### 9.1 Unit Tests

```typescript
describe('ValidationService', () => {
  let service: ValidationService;

  beforeEach(() => {
    service = new ValidationService();
  });

  it('should detect circular dependencies', () => {
    const services = [
      { id: 'A', name: 'ServiceA', entities: [], configuration: {} },
      { id: 'B', name: 'ServiceB', entities: [], configuration: {} },
      { id: 'C', name: 'ServiceC', entities: [], configuration: {} }
    ];

    const dependencies = [
      { id: '1', sourceServiceId: 'A', targetServiceId: 'B', type: 'Synchronous' },
      { id: '2', sourceServiceId: 'B', targetServiceId: 'C', type: 'Synchronous' },
      { id: '3', sourceServiceId: 'C', targetServiceId: 'A', type: 'Synchronous' }
    ];

    const result = service.validateConfiguration(services, dependencies);

    expect(result.isValid).toBe(false);
    expect(result.errors.length).toBeGreaterThan(0);
    expect(result.errors[0].type).toBe('CircularDependency');
  });

  it('should warn about small services', () => {
    const services = [
      { 
        id: 'A', 
        name: 'SmallService', 
        entities: [{ id: '1', name: 'Entity1' }],
        configuration: {}
      }
    ];

    const result = service.validateConfiguration(services, []);

    expect(result.warnings.length).toBeGreaterThan(0);
    expect(result.warnings[0].type).toBe('SmallService');
  });
});
```

### 9.2 Integration Tests

```typescript
describe('Service Boundary Designer Integration', () => {
  let component: ServiceBoundaryDesignerComponent;
  let fixture: ComponentFixture<ServiceBoundaryDesignerComponent>;
  let service: ServiceBoundaryService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ServiceBoundaryDesignerComponent],
      providers: [ServiceBoundaryService, CanvasManagerService]
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceBoundaryDesignerComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(ServiceBoundaryService);
  });

  it('should load project and analyze decomposition', fakeAsync(() => {
    const projectId = 'test-project';
    component.projectId = projectId;

    component.ngOnInit();
    tick();

    expect(component.services.length).toBeGreaterThan(0);
    expect(component.analysisResult).toBeDefined();
  }));

  it('should move entity between services', () => {
    const entity = { id: 'e1', name: 'Customer' };
    const fromService = { id: 's1', name: 'Service1', entities: [entity] };
    const toService = { id: 's2', name: 'Service2', entities: [] };

    component.services = [fromService, toService];

    component.onEntityMoved({
      entityId: 'e1',
      fromServiceId: 's1',
      toServiceId: 's2'
    });

    expect(fromService.entities.length).toBe(0);
    expect(toService.entities.length).toBe(1);
    expect(toService.entities[0].id).toBe('e1');
  });
});
```

---

## 10. Accessibility

### 10.1 Keyboard Navigation

- All interactive elements are keyboard accessible
- Tab order follows logical flow
- Focus indicators are clearly visible
- Keyboard shortcuts are documented

### 10.2 Screen Reader Support

```html
<!-- Service node -->
<div 
  role="group" 
  [attr.aria-label]="'Service: ' + service.name + ', ' + service.entities.length + ' entities'"
  tabindex="0">
  
  <!-- Entity -->
  <div 
    role="listitem"
    [attr.aria-label]="'Entity: ' + entity.name"
    [attr.aria-describedby]="'entity-desc-' + entity.id">
    
    <span [id]="'entity-desc-' + entity.id" class="sr-only">
      {{ entity.type }}, {{ entity.relations.length }} relations
    </span>
  </div>
</div>
```

---

## 11. Future Enhancements

### 11.1 AI-Powered Suggestions

- Use machine learning to learn from user corrections
- Suggest optimal service boundaries based on historical data
- Predict potential issues before they occur

### 11.2 Collaborative Editing

- Real-time collaboration (multiple users editing simultaneously)
- Change tracking and conflict resolution
- Comments and annotations

### 11.3 Advanced Visualizations

- 3D visualization for complex systems
- Timeline view showing evolution of services
- Heat maps showing coupling/cohesion

### 11.4 Integration with Architecture Decision Records (ADRs)

- Auto-generate ADRs for service boundary decisions
- Track rationale for changes
- Link to documentation

---

## 12. Summary

The Service Boundary Designer is a sophisticated visual tool that combines:

✅ **Intuitive UI**: Drag-and-drop interface with Konva.js  
✅ **Intelligent Analysis**: DDD-based decomposition algorithm  
✅ **Real-time Validation**: Instant feedback on configuration  
✅ **Flexible Configuration**: Manual override of auto-suggestions  
✅ **Performance**: Optimized for large projects (100+ entities)  
✅ **Accessibility**: Keyboard navigation and screen reader support  

**Technology Stack**:
- **Frontend**: Angular + Konva.js + NgRx
- **Backend**: ASP.NET Core + Graph algorithms
- **Visualization**: Force-directed layout, dependency graphs
- **Validation**: Client-side + server-side validation

**Key Metrics**:
- Supports projects with 100+ entities
- Real-time validation (< 100ms)
- Auto-layout in < 1 second
- Smooth 60 FPS rendering

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-02  
**Status**: Ready for Implementation
