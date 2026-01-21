# Handling Complex UI & Data Scenarios

This document analyzes three "Edge Case" scenarios that often break disparate low-code platforms and details how our architecture solves them.

---

## Scenario 1: The "Excel-Like" Bulk Editor (Batch Operations)

**The Requirement:**
A "Price Manager" needs to load 50 products, update their prices in a grid (inline editing), and click one **"Save Changes"** button.

### 1.1 The Visual Model
*   **Widget**: `DataGrid` with property `Mode: Edit`.
*   **Binding**: Binds to a **Collection** (`Product[]`), not a single item.
*   **Save Action**: Configured to call a specific Bulk API.

### 1.2 State Management Challenge
We cannot fire 50 HTTP PUT requests. We need to track "Dirty State" for the whole list.

### 1.3 Generated Solution (The `BatchManager`)
The generator creates a localized "FormArray" wrapper.

```typescript
// generated: product-grid.component.ts
productsFormArray = new FormArray([]);

// On Load
loadData() {
    this.api.getProducts().subscribe(list => {
        list.forEach(p => {
             // Create a Form Group for EACH row
             this.productsFormArray.push(this.fb.group({
                 id: [p.id],
                 price: [p.price, Validators.min(0)],
                 isDirty: [false] // Internal tracking
             }));
        });
    });
}

// On Save
saveAll() {
    // 1. Filter only changed rows
    const changes = this.productsFormArray.value.filter(x => x.isDirty);
    
    // 2. Send single Batch Payload
    this.api.batchUpdateProducts(changes).subscribe();
}
```

---

## Scenario 2: Deeply Nested Master-Detail (The "Quoting Engine")

**The Requirement:**
A "Quote" form.
*   Header (Customer)
*   Line Items (Products)
    *   *Sub-Items* (Discounts per line)
        *   *Attributes* (Notes per discount)

**The Challenge:**
Standard "Detail Views" fail at depth > 2. The ID linkage (`QuoteId` -> `ItemId` -> `DiscountId`) becomes a nightmare to manage if done sequentially.

### 2.1 The "Transient Tree" Strategy
For deep creations, we don't save row-by-row. We construct a **Composite DTO** in memory and save the entire tree in **One Transaction**.

### 2.2 Metadata Configuration
The user defines an "Aggregate Root" Entity (`Quote`).
*   Relationship: `Quote` has many `Items` (Composition).
*   Relationship: `Item` has many `Discounts` (Composition).

### 2.3 Generated API (Graph Handling)
The Backend Generator detects the "Composition" flag and generates a `DeepInsert` method.

```csharp
// QuotesController.cs
[HttpPost]
public async Task<IActionResult> CreateQuote(QuoteDeepDto dto) {
    using var trans = _db.Database.BeginTransaction();
    
    // 1. Save Header
    var quote = _mapper.Map<Quote>(dto);
    _db.Quotes.Add(quote);
    await _db.SaveChangesAsync(); // Generates Quote.Id

    // 2. Save Items (propagating Quote.Id)
    foreach(var itemDto in dto.Items) {
        var item = _mapper.Map<Item>(itemDto);
        item.QuoteId = quote.Id; // <-- The Glue
        _db.Items.Add(item);
        await _db.SaveChangesAsync(); // Generates Item.Id

        // 3. Save Deep Children
        foreach(var disc in itemDto.Discounts) {
            disc.ItemId = item.Id; // <-- Deep Glue
            _db.Discounts.Add(disc);
        }
    }
    trans.Commit();
}
```

---

## Scenario 3: Dynamic / Polymorphic Forms

**The Requirement:**
An Asset Management System.
*   Dropdown: "Asset Type" (Laptop | Car | Building).
*   **If Laptop**: Show "RAM", "CPU".
*   **If Car**: Show "Mileage", "Fuel Type".
*   **If Building**: Show "Address", "Floors".

**The Challenge:**
These are all stored in one table (or related tables) but have completely different validation rules and UI structures.

### 3.1 The "Polymorphic Panel" Widget
We introduce a `SwitchContainer` widget in the Studio.
*   **Discriminator**: Field `AssetType`.
*   **Cases**:
    *   `Laptop` -> Load Fragment `Form_Laptop`.
    *   `Car` -> Load Fragment `Form_Car`.

### 3.2 Implementation (Inheritance)
The Generated TypeScript uses **Class Inheritance**.

```typescript
// base-asset.ts
export class BaseAssetForm { 
    type: string; 
    purchaseDate: Date;
}

// laptop-asset.ts
export class LaptopForm extends BaseAssetForm {
    ram: string; // Required only here
}
```

The HTML uses `ngSwitch` to physically swap the DOM implementation.

```html
<select [(ngModel)]="model.type">...</select>

<ng-container [ngSwitch]="model.type">
    <laptop-form *ngSwitchCase="'Laptop'" [(model)]="model"></laptop-form>
    <car-form    *ngSwitchCase="'Car'"    [(model)]="model"></car-form>
</ng-container>
```

---

## Scenario 4: Cross-Component Communication (The Dashboard)

**The Requirement:**
*   **Left Pane**: A "Tree View" of Departments.
*   **Top Right**: A "Chart" of expenses.
*   **Bottom Right**: A "Grid" of employees.
*   **Action**: Clicking a Department in the Tree filters *both* the Chart and the Grid.

**The Challenge:**
These 3 widgets might be on the page, but they are isolated components.

### 4.1 The "Event Bus" (Mediator)
The platform generates a Page-Level **Mediator Service**.

### 4.2 Metadata
*   Tree View Event: `OnSelect -> Publish("DepartmentChanged", $event.id)`
*   Chart Listener: `Subscribe("DepartmentChanged") -> ReloadData(deptId)`
*   Grid Listener: `Subscribe("DepartmentChanged") -> ReloadData(deptId)`

### 4.3 Generated Code
```typescript
// page-mediator.service.ts (Scoped to the Page)
export class DashboardMediator {
    public department$ = new Subject<string>();
}

// tree.component.ts
onNodeClick(node) {
    this.mediator.department$.next(node.id);
}

// chart.component.ts
ngOnInit() {
    this.mediator.department$.subscribe(id => this.refreshChart(id));
}
```

This decouples the components perfectly.
