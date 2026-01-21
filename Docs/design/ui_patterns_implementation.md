# UI Patterns: Cascading Dropdowns & Conditional Visibility

This document details the technical implementation of common dynamic form patterns using our Client-Side Interaction Engine.

## 1. Conditional Visibility (Show/Hide Blocks)

**Scenario**: A form has a checkbox "Is Married?". If checked, a "Spouse Details" section appears.

### 1.1 Metadata Definition (The "Low-Code" View)

The "Spouse Details" container (a Group or Card widget) has a property `VisibilityExpression`.

**Metadata JSON:**
```json
{
  "type": "Container",
  "id": "container_spouse_info",
  "properties": {
    "visible": "model.isMarried === true" // JavaScript Expression
  },
  "children": [ ... ]
}
```

### 1.2 Code Generation (The "Output")

The generator transpiles this into an Angular `*ngIf` directive.

**Generated HTML (`user-form.component.html`):**
```html
<!-- Checkbox bound to the model -->
<checkbox [(ngModel)]="model.isMarried" label="Is Married?"></checkbox>

<!-- Conditional Container -->
<div *ngIf="model.isMarried" class="spouse-card">
    <h3>Spouse Details</h3>
    <input [(ngModel)]="model.spouseName" placeholder="Spouse Name" />
</div>
```

**Result**: Because Angular's Change Detection is automatic, checking the box immediately renders the DOM elements. No manual event handlers are needed.

---

## 2. Cascading Dropdowns (Dependent Selects)

**Scenario**: 
1.  Dropdown A: Select "Country".
2.  Dropdown B: Select "State" (Content depends on Country).

### 2.1 The Architecture: Reactive Data Sources

Instead of imperatively writing "On Country Change -> Clear State -> Call API -> Fill State", we use **Reactive Parameters**.

The "State" dropdown is bound to a `DataSource` that requires a parameter. That parameter is bound to the `value` of the "Country" dropdown.

### 2.2 Metadata Definition

**Dropdown: Country**
```json
{
  "id": "ddl_country",
  "bind": "model.countryId",
  "dataSource": "GetAllCountries" // Simple API call
}
```

**Dropdown: State**
```json
{
  "id": "ddl_state",
  "bind": "model.stateId",
  "dataSource": {
    "api": "GetStatesByCountry",
    "params": {
      "countryId": "{model.countryId}" // <-- BINDING
    }
  },
  "properties": {
    "disabled": "!model.countryId" // Disable if no country selected
  }
}
```

### 2.3 Code Generation Strategy (RxJS Chains)

We generate a robust RxJS stream in the component.

**Generated TypeScript (`location-form.component.ts`):**

```typescript
export class LocationFormComponent implements OnInit {
    
    // 1. Define Observables for the data
    countries$: Observable<CountryDto[]>;
    states$: Observable<StateDto[]>;

    // 2. The Form Model
    model = { countryId: null, stateId: null };

    // 3. Subject to track parameter changes
    private countryChange$ = new BehaviorSubject<string>(null);

    ngOnInit() {
        // Load Countries (Static)
        this.countries$ = this.api.getCountries();

        // Load States (Reactive!)
        // "Whenever Country Changes, Switch to a new API call"
        this.states$ = this.countryChange$.pipe(
            filter(id => !!id), // Don't call if null
            switchMap(id => this.api.getStatesByCountry(id))
        );
    }

    // Wiring up the Country Dropdown 'OnChange'
    onCountryChange(newId: string) {
        this.model.stateId = null; // Reset child
        this.countryChange$.next(newId); // Trigger the stream
    }
}
```

**Generated HTML:**
```html
<!-- Country Select -->
<select [ngModel]="model.countryId" (ngModelChange)="onCountryChange($event)">
    <option *ngFor="let c of countries$ | async" [value]="c.id">{{c.name}}</option>
</select>

<!-- State Select -->
<select [(ngModel)]="model.stateId" [disabled]="!model.countryId">
    <option *ngFor="let s of states$ | async" [value]="s.id">{{s.name}}</option>
</select>
```

### 2.4 Why this is better than "Standard" Events?
1.  **Race Condition Proof**: If the user changes Country from 'USA' to 'Canada' rapidly, `switchMap` cancels the pending 'USA' request automatically.
2.  **Less Code**: No manual array clearing or loading state management logic is needed; `| async` pipe handles subscriptions.

## 3. Summary of Implementation

| Pattern | Low-Code Mechanism | Generated Tech |
| :--- | :--- | :--- |
| **Conditional Visibility** | Expression Binding (`model.prop == X`) | `*ngIf="expr"` |
| **Cascading Dropdowns** | Parameter Binding on Data Source | `BehaviorSubject` + `switchMap` |
