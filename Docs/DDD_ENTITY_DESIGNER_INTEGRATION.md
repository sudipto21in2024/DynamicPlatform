# Integrating DDD with Entity Designer

## Overview

This document shows how to enhance your existing **Entity Designer** to support Domain-Driven Design principles, enabling users to create rich domain models visually.

---

## 1. Current Entity Designer Analysis

### 1.1 Existing Capabilities

Let me first review your current Entity Designer to understand what's already there:

**Current Features** (Based on `entity-designer.ts`):
- ✅ Create entities with properties
- ✅ Define relationships between entities
- ✅ Set property types and constraints
- ✅ Visual canvas for designing

**What's Missing for DDD**:
- ❌ Distinguish between Entities and Value Objects
- ❌ Define Aggregate boundaries
- ❌ Add business methods (behavior)
- ❌ Define domain events
- ❌ Specify invariants/validations

---

## 2. Enhanced Entity Designer - DDD Mode

### 2.1 New UI Components

#### Component 1: Entity Type Selector

```typescript
// entity-designer/components/entity-type-selector.component.ts

export enum EntityClassification {
  Entity = 'entity',
  ValueObject = 'value-object',
  AggregateRoot = 'aggregate-root'
}

@Component({
  selector: 'app-entity-type-selector',
  template: `
    <div class="entity-type-selector">
      <h3>Entity Classification</h3>
      
      <mat-radio-group [(ngModel)]="selectedType" (change)="onTypeChange()">
        <mat-radio-button [value]="EntityClassification.AggregateRoot">
          <div class="type-option">
            <mat-icon>account_tree</mat-icon>
            <div>
              <strong>Aggregate Root</strong>
              <p>Entry point to an aggregate, has identity</p>
            </div>
          </div>
        </mat-radio-button>
        
        <mat-radio-button [value]="EntityClassification.Entity">
          <div class="type-option">
            <mat-icon>badge</mat-icon>
            <div>
              <strong>Entity</strong>
              <p>Has identity, part of an aggregate</p>
            </div>
          </div>
        </mat-radio-button>
        
        <mat-radio-button [value]="EntityClassification.ValueObject">
          <div class="type-option">
            <mat-icon>label</mat-icon>
            <div>
              <strong>Value Object</strong>
              <p>Immutable, defined by values</p>
            </div>
          </div>
        </mat-radio-button>
      </mat-radio-group>
      
      <div *ngIf="selectedType === EntityClassification.AggregateRoot" class="aggregate-info">
        <mat-icon color="primary">info</mat-icon>
        <p>This entity will be the root of an aggregate. Other entities can be added to this aggregate.</p>
      </div>
    </div>
  `
})
export class EntityTypeSelectorComponent {
  EntityClassification = EntityClassification;
  @Input() selectedType: EntityClassification;
  @Output() typeChange = new EventEmitter<EntityClassification>();
  
  onTypeChange() {
    this.typeChange.emit(this.selectedType);
  }
}
```

#### Component 2: Behavior Designer

```typescript
// entity-designer/components/behavior-designer.component.ts

export interface BusinessMethod {
  name: string;
  description: string;
  parameters: MethodParameter[];
  returnType: string;
  validations: Validation[];
  raisesEvent?: string;
  implementation?: string;
}

export interface MethodParameter {
  name: string;
  type: string;
  isRequired: boolean;
}

export interface Validation {
  condition: string;
  errorMessage: string;
  exceptionType: string;
}

@Component({
  selector: 'app-behavior-designer',
  template: `
    <div class="behavior-designer">
      <h3>Business Methods</h3>
      
      <button mat-raised-button color="primary" (click)="addMethod()">
        <mat-icon>add</mat-icon>
        Add Method
      </button>
      
      <div class="methods-list">
        <mat-expansion-panel *ngFor="let method of methods; let i = index">
          <mat-expansion-panel-header>
            <mat-panel-title>
              <mat-icon>function</mat-icon>
              {{ method.name || 'New Method' }}
            </mat-panel-title>
            <mat-panel-description>
              {{ method.description }}
            </mat-panel-description>
          </mat-expansion-panel-header>
          
          <div class="method-editor">
            <!-- Method Name -->
            <mat-form-field>
              <mat-label>Method Name</mat-label>
              <input matInput [(ngModel)]="method.name" placeholder="e.g., PlaceOrder">
            </mat-form-field>
            
            <!-- Description -->
            <mat-form-field>
              <mat-label>Description</mat-label>
              <textarea matInput [(ngModel)]="method.description" rows="2"></textarea>
            </mat-form-field>
            
            <!-- Return Type -->
            <mat-form-field>
              <mat-label>Return Type</mat-label>
              <mat-select [(ngModel)]="method.returnType">
                <mat-option value="void">void</mat-option>
                <mat-option value="bool">bool</mat-option>
                <mat-option value="int">int</mat-option>
                <mat-option value="string">string</mat-option>
                <mat-option value="Guid">Guid</mat-option>
              </mat-select>
            </mat-form-field>
            
            <!-- Parameters -->
            <div class="parameters-section">
              <h4>Parameters</h4>
              <button mat-button (click)="addParameter(method)">
                <mat-icon>add</mat-icon>
                Add Parameter
              </button>
              
              <div *ngFor="let param of method.parameters; let j = index" class="parameter-row">
                <mat-form-field>
                  <mat-label>Name</mat-label>
                  <input matInput [(ngModel)]="param.name">
                </mat-form-field>
                
                <mat-form-field>
                  <mat-label>Type</mat-label>
                  <input matInput [(ngModel)]="param.type">
                </mat-form-field>
                
                <mat-checkbox [(ngModel)]="param.isRequired">Required</mat-checkbox>
                
                <button mat-icon-button (click)="removeParameter(method, j)">
                  <mat-icon>delete</mat-icon>
                </button>
              </div>
            </div>
            
            <!-- Validations -->
            <div class="validations-section">
              <h4>Validations</h4>
              <button mat-button (click)="addValidation(method)">
                <mat-icon>add</mat-icon>
                Add Validation
              </button>
              
              <div *ngFor="let validation of method.validations; let k = index" class="validation-row">
                <mat-form-field>
                  <mat-label>Condition</mat-label>
                  <input matInput [(ngModel)]="validation.condition" 
                         placeholder="e.g., Status != OrderStatus.Draft">
                </mat-form-field>
                
                <mat-form-field>
                  <mat-label>Error Message</mat-label>
                  <input matInput [(ngModel)]="validation.errorMessage">
                </mat-form-field>
                
                <mat-form-field>
                  <mat-label>Exception Type</mat-label>
                  <mat-select [(ngModel)]="validation.exceptionType">
                    <mat-option value="ArgumentException">ArgumentException</mat-option>
                    <mat-option value="InvalidOperationException">InvalidOperationException</mat-option>
                    <mat-option value="DomainException">DomainException</mat-option>
                  </mat-select>
                </mat-form-field>
                
                <button mat-icon-button (click)="removeValidation(method, k)">
                  <mat-icon>delete</mat-icon>
                </button>
              </div>
            </div>
            
            <!-- Domain Event -->
            <mat-checkbox [(ngModel)]="method.raisesEvent">Raises Domain Event</mat-checkbox>
            
            <mat-form-field *ngIf="method.raisesEvent">
              <mat-label>Event Name</mat-label>
              <input matInput [(ngModel)]="method.eventName" placeholder="e.g., OrderPlacedEvent">
            </mat-form-field>
            
            <!-- Implementation (Optional) -->
            <mat-form-field>
              <mat-label>Implementation (C# code)</mat-label>
              <textarea matInput [(ngModel)]="method.implementation" rows="5" 
                        placeholder="Optional: Custom implementation code"></textarea>
            </mat-form-field>
          </div>
          
          <mat-action-row>
            <button mat-button color="warn" (click)="removeMethod(i)">Delete Method</button>
          </mat-action-row>
        </mat-expansion-panel>
      </div>
    </div>
  `
})
export class BehaviorDesignerComponent {
  @Input() methods: BusinessMethod[] = [];
  @Output() methodsChange = new EventEmitter<BusinessMethod[]>();
  
  addMethod() {
    this.methods.push({
      name: '',
      description: '',
      parameters: [],
      returnType: 'void',
      validations: []
    });
    this.methodsChange.emit(this.methods);
  }
  
  removeMethod(index: number) {
    this.methods.splice(index, 1);
    this.methodsChange.emit(this.methods);
  }
  
  addParameter(method: BusinessMethod) {
    method.parameters.push({
      name: '',
      type: 'string',
      isRequired: false
    });
  }
  
  removeParameter(method: BusinessMethod, index: number) {
    method.parameters.splice(index, 1);
  }
  
  addValidation(method: BusinessMethod) {
    method.validations.push({
      condition: '',
      errorMessage: '',
      exceptionType: 'InvalidOperationException'
    });
  }
  
  removeValidation(method: BusinessMethod, index: number) {
    method.validations.splice(index, 1);
  }
}
```

#### Component 3: Aggregate Designer

```typescript
// entity-designer/components/aggregate-designer.component.ts

export interface Aggregate {
  rootEntityId: string;
  name: string;
  description: string;
  includedEntities: string[];
  boundaryColor: string;
}

@Component({
  selector: 'app-aggregate-designer',
  template: `
    <div class="aggregate-designer">
      <h3>Aggregate Boundaries</h3>
      
      <button mat-raised-button color="primary" (click)="defineAggregate()">
        <mat-icon>account_tree</mat-icon>
        Define Aggregate
      </button>
      
      <div class="aggregates-list">
        <mat-card *ngFor="let aggregate of aggregates" [style.border-left]="'4px solid ' + aggregate.boundaryColor">
          <mat-card-header>
            <mat-card-title>{{ aggregate.name }}</mat-card-title>
            <mat-card-subtitle>Root: {{ getRootEntityName(aggregate.rootEntityId) }}</mat-card-subtitle>
          </mat-card-header>
          
          <mat-card-content>
            <p>{{ aggregate.description }}</p>
            
            <div class="included-entities">
              <h4>Included Entities:</h4>
              <mat-chip-list>
                <mat-chip *ngFor="let entityId of aggregate.includedEntities" 
                          [removable]="true" 
                          (removed)="removeEntityFromAggregate(aggregate, entityId)">
                  {{ getEntityName(entityId) }}
                  <mat-icon matChipRemove>cancel</mat-icon>
                </mat-chip>
              </mat-chip-list>
              
              <button mat-button (click)="addEntityToAggregate(aggregate)">
                <mat-icon>add</mat-icon>
                Add Entity
              </button>
            </div>
          </mat-card-content>
          
          <mat-card-actions>
            <button mat-button (click)="editAggregate(aggregate)">Edit</button>
            <button mat-button color="warn" (click)="deleteAggregate(aggregate)">Delete</button>
          </mat-card-actions>
        </mat-card>
      </div>
    </div>
  `
})
export class AggregateDesignerComponent {
  @Input() entities: Entity[];
  @Input() aggregates: Aggregate[] = [];
  @Output() aggregatesChange = new EventEmitter<Aggregate[]>();
  
  defineAggregate() {
    const dialogRef = this.dialog.open(AggregateDialogComponent, {
      width: '600px',
      data: { entities: this.entities }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.aggregates.push(result);
        this.aggregatesChange.emit(this.aggregates);
      }
    });
  }
  
  getRootEntityName(entityId: string): string {
    return this.entities.find(e => e.id === entityId)?.name || 'Unknown';
  }
  
  getEntityName(entityId: string): string {
    return this.entities.find(e => e.id === entityId)?.name || 'Unknown';
  }
}
```

#### Component 4: Domain Events Designer

```typescript
// entity-designer/components/domain-events-designer.component.ts

export interface DomainEvent {
  name: string;
  description: string;
  properties: EventProperty[];
  triggeredBy: string; // Method name
}

export interface EventProperty {
  name: string;
  type: string;
  description: string;
}

@Component({
  selector: 'app-domain-events-designer',
  template: `
    <div class="domain-events-designer">
      <h3>Domain Events</h3>
      
      <button mat-raised-button color="primary" (click)="addEvent()">
        <mat-icon>event</mat-icon>
        Add Domain Event
      </button>
      
      <div class="events-list">
        <mat-expansion-panel *ngFor="let event of events; let i = index">
          <mat-expansion-panel-header>
            <mat-panel-title>
              <mat-icon>notifications</mat-icon>
              {{ event.name || 'New Event' }}
            </mat-panel-title>
          </mat-expansion-panel-header>
          
          <div class="event-editor">
            <mat-form-field>
              <mat-label>Event Name</mat-label>
              <input matInput [(ngModel)]="event.name" placeholder="e.g., OrderPlacedEvent">
            </mat-form-field>
            
            <mat-form-field>
              <mat-label>Description</mat-label>
              <textarea matInput [(ngModel)]="event.description" rows="2"></textarea>
            </mat-form-field>
            
            <div class="properties-section">
              <h4>Event Properties</h4>
              <button mat-button (click)="addEventProperty(event)">
                <mat-icon>add</mat-icon>
                Add Property
              </button>
              
              <div *ngFor="let prop of event.properties; let j = index" class="property-row">
                <mat-form-field>
                  <mat-label>Name</mat-label>
                  <input matInput [(ngModel)]="prop.name">
                </mat-form-field>
                
                <mat-form-field>
                  <mat-label>Type</mat-label>
                  <input matInput [(ngModel)]="prop.type">
                </mat-form-field>
                
                <mat-form-field>
                  <mat-label>Description</mat-label>
                  <input matInput [(ngModel)]="prop.description">
                </mat-form-field>
                
                <button mat-icon-button (click)="removeEventProperty(event, j)">
                  <mat-icon>delete</mat-icon>
                </button>
              </div>
            </div>
          </div>
          
          <mat-action-row>
            <button mat-button color="warn" (click)="removeEvent(i)">Delete Event</button>
          </mat-action-row>
        </mat-expansion-panel>
      </div>
    </div>
  `
})
export class DomainEventsDesignerComponent {
  @Input() events: DomainEvent[] = [];
  @Output() eventsChange = new EventEmitter<DomainEvent[]>();
  
  addEvent() {
    this.events.push({
      name: '',
      description: '',
      properties: [],
      triggeredBy: ''
    });
    this.eventsChange.emit(this.events);
  }
  
  removeEvent(index: number) {
    this.events.splice(index, 1);
    this.eventsChange.emit(this.events);
  }
  
  addEventProperty(event: DomainEvent) {
    event.properties.push({
      name: '',
      type: 'string',
      description: ''
    });
  }
  
  removeEventProperty(event: DomainEvent, index: number) {
    event.properties.splice(index, 1);
  }
}
```

---

## 3. Enhanced Entity Model

### 3.1 Updated Entity Interface

```typescript
// models/entity.model.ts

export interface Entity {
  id: string;
  name: string;
  description: string;
  
  // DDD Classification
  classification: EntityClassification;
  isAggregateRoot: boolean;
  aggregateId?: string;
  
  // Properties
  properties: Property[];
  
  // DDD Additions
  businessMethods: BusinessMethod[];
  domainEvents: DomainEvent[];
  invariants: Invariant[];
  
  // Relationships
  relationships: Relationship[];
  
  // Metadata
  createdAt: Date;
  updatedAt: Date;
}

export interface Property {
  id: string;
  name: string;
  type: string;
  isIdentity: boolean;
  isRequired: boolean;
  isCollection: boolean;
  defaultValue?: string;
  
  // Value Object specific
  isValueObject: boolean;
  valueObjectType?: string;
}

export interface Invariant {
  description: string;
  condition: string;
  errorMessage: string;
}
```

---

## 4. Visual Enhancements

### 4.1 Canvas Rendering with Aggregate Boundaries

```typescript
// entity-designer/services/canvas-renderer.service.ts

export class CanvasRendererService {
  renderAggregates(stage: Konva.Stage, aggregates: Aggregate[], entities: Entity[]) {
    const layer = new Konva.Layer();
    
    aggregates.forEach(aggregate => {
      // Calculate bounding box for all entities in aggregate
      const boundingBox = this.calculateAggregateBounds(aggregate, entities);
      
      // Draw aggregate boundary
      const boundary = new Konva.Rect({
        x: boundingBox.x - 20,
        y: boundingBox.y - 20,
        width: boundingBox.width + 40,
        height: boundingBox.height + 40,
        stroke: aggregate.boundaryColor,
        strokeWidth: 3,
        dash: [10, 5],
        cornerRadius: 10
      });
      
      // Add aggregate label
      const label = new Konva.Label({
        x: boundingBox.x - 10,
        y: boundingBox.y - 40
      });
      
      label.add(new Konva.Tag({
        fill: aggregate.boundaryColor,
        cornerRadius: 5
      }));
      
      label.add(new Konva.Text({
        text: `Aggregate: ${aggregate.name}`,
        fontSize: 14,
        padding: 5,
        fill: 'white'
      }));
      
      layer.add(boundary);
      layer.add(label);
    });
    
    stage.add(layer);
  }
  
  renderEntity(entity: Entity): Konva.Group {
    const group = new Konva.Group({
      draggable: true
    });
    
    // Different colors based on classification
    const colors = {
      [EntityClassification.AggregateRoot]: '#4CAF50',
      [EntityClassification.Entity]: '#2196F3',
      [EntityClassification.ValueObject]: '#FF9800'
    };
    
    const rect = new Konva.Rect({
      width: 200,
      height: this.calculateEntityHeight(entity),
      fill: 'white',
      stroke: colors[entity.classification],
      strokeWidth: entity.isAggregateRoot ? 4 : 2,
      cornerRadius: 5,
      shadowColor: 'black',
      shadowBlur: 10,
      shadowOpacity: 0.3
    });
    
    // Header with icon
    const header = new Konva.Rect({
      width: 200,
      height: 40,
      fill: colors[entity.classification],
      cornerRadius: [5, 5, 0, 0]
    });
    
    const icon = this.getEntityIcon(entity.classification);
    const iconText = new Konva.Text({
      x: 10,
      y: 10,
      text: icon,
      fontSize: 20,
      fill: 'white'
    });
    
    const nameText = new Konva.Text({
      x: 40,
      y: 12,
      text: entity.name,
      fontSize: 16,
      fontStyle: 'bold',
      fill: 'white'
    });
    
    group.add(rect, header, iconText, nameText);
    
    // Add properties section
    let yOffset = 50;
    entity.properties.forEach(prop => {
      const propText = new Konva.Text({
        x: 10,
        y: yOffset,
        text: `${prop.isIdentity ? '🔑 ' : ''}${prop.name}: ${prop.type}`,
        fontSize: 12,
        fill: '#333'
      });
      group.add(propText);
      yOffset += 20;
    });
    
    // Add methods section
    if (entity.businessMethods.length > 0) {
      const methodsHeader = new Konva.Text({
        x: 10,
        y: yOffset,
        text: 'Methods:',
        fontSize: 12,
        fontStyle: 'bold',
        fill: '#666'
      });
      group.add(methodsHeader);
      yOffset += 20;
      
      entity.businessMethods.forEach(method => {
        const methodText = new Konva.Text({
          x: 10,
          y: yOffset,
          text: `+ ${method.name}()`,
          fontSize: 11,
          fill: '#4CAF50'
        });
        group.add(methodText);
        yOffset += 18;
      });
    }
    
    return group;
  }
  
  private getEntityIcon(classification: EntityClassification): string {
    const icons = {
      [EntityClassification.AggregateRoot]: '👑',
      [EntityClassification.Entity]: '📦',
      [EntityClassification.ValueObject]: '🏷️'
    };
    return icons[classification];
  }
}
```

---

## 5. Integration Flow

### 5.1 Complete Workflow

```
User Creates Entity
       ↓
Select Classification (Aggregate Root / Entity / Value Object)
       ↓
Define Properties
       ↓
Add Business Methods (Behavior)
       ↓
Define Validations/Invariants
       ↓
Specify Domain Events
       ↓
Group into Aggregates
       ↓
Generate DDD Code
```

### 5.2 Backend Integration

```csharp
// Platform.Engine/Services/DddCodeGenerationService.cs

public class DddCodeGenerationService
{
    private readonly ITemplateEngine _templateEngine;
    
    public async Task<GeneratedProject> GenerateDddProjectAsync(DomainModel model)
    {
        var project = new GeneratedProject();
        
        // Generate aggregates
        foreach (var aggregate in model.Aggregates)
        {
            // Generate aggregate root
            var rootCode = await GenerateEntityAsync(aggregate.RootEntity);
            project.AddFile($"Domain/Aggregates/{aggregate.Name}/{aggregate.RootEntity.Name}.cs", rootCode);
            
            // Generate child entities
            foreach (var entity in aggregate.ChildEntities)
            {
                var entityCode = await GenerateEntityAsync(entity);
                project.AddFile($"Domain/Aggregates/{aggregate.Name}/{entity.Name}.cs", entityCode);
            }
            
            // Generate value objects
            foreach (var vo in aggregate.ValueObjects)
            {
                var voCode = await GenerateValueObjectAsync(vo);
                project.AddFile($"Domain/ValueObjects/{vo.Name}.cs", voCode);
            }
            
            // Generate repository
            var repoCode = await GenerateRepositoryAsync(aggregate);
            project.AddFile($"Infrastructure/Repositories/{aggregate.Name}Repository.cs", repoCode);
            
            // Generate domain events
            foreach (var evt in aggregate.DomainEvents)
            {
                var eventCode = await GenerateDomainEventAsync(evt);
                project.AddFile($"Domain/Events/{evt.Name}.cs", eventCode);
            }
        }
        
        return project;
    }
}
```

---

## 6. Example: Order Aggregate in UI

### 6.1 User Creates Order Aggregate

**Step 1**: Create Order Entity
- Classification: Aggregate Root
- Properties: OrderId (Identity), OrderNumber, CustomerId, Status, TotalAmount
- Methods: AddItem(), RemoveItem(), PlaceOrder(), Cancel()
- Events: OrderPlacedEvent, OrderCancelledEvent

**Step 2**: Create OrderItem Entity
- Classification: Entity (part of Order aggregate)
- Properties: OrderItemId, ProductId, Quantity, UnitPrice
- Methods: IncreaseQuantity(), DecreaseQuantity()

**Step 3**: Create Money Value Object
- Classification: Value Object
- Properties: Amount, Currency
- Methods: Add(), Subtract()

**Step 4**: Define Aggregate Boundary
- Root: Order
- Included: OrderItem
- Value Objects: Money, Address

**Step 5**: Generate Code
- Click "Generate DDD Code"
- Platform generates complete aggregate with repositories, events, etc.

---

## Summary

This integration enables:

✅ **Visual DDD modeling** in Entity Designer  
✅ **Aggregate boundary definition**  
✅ **Business method design**  
✅ **Domain event specification**  
✅ **Automatic code generation** following DDD principles  

**Document Version**: 1.0  
**Last Updated**: 2026-02-02
