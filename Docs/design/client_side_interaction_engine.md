# Client-Side Interaction & Flow Designer

This document defines the architecture for the **Client-Side Logic Engine**, enabling users to define dynamic UI behavior (e.g., "On Click -> Show Modal -> If Confirmed -> Call API") without writing JavaScript/TypeScript.

## 1. Core Concept: Event-Driven Flows

Frontend logic is modeled as a chain of actions triggered by UI Events.

```mermaid
graph LR
    Event[UI Event (Click/Change)] --> Flow[Client Flow]
    Flow --> Step1[Action: Validate]
    Step1 --> Step2[Decision: Is Valid?]
    Step2 -- Yes --> Step3[Action: Call API]
    Step2 -- No --> Step4[Action: Show Toast Error]
```

## 2. The Visual Designer (Interaction Builder)

We will implement a **Low-Code Interaction Panel** within the Page Designer.

### 2.1 UI Experience
1.  **Select a Widget**: User clicks on a Button ("Submit Order").
2.  **Events Tab**: Shows available events (e.g., `OnClick`, `OnHover`).
3.  **Flow Editor**:
    *   **Simple Mode**: A linear list of steps (Action 1 -> Action 2).
    *   **Advanced Mode**: A Node-based canvas (using React Flow / Konva) for branching logic.

### 2.2 Available Actions (The Standard Library)
The platform comes with built-in client actions:
*   **UI Manipulation**: `SetVisible`, `SetEnable`, `AddClass`, `SetFocus`.
*   **Navigation**: `NavigateToPage`, `OpenModal`, `CloseModal`.
*   **Data**: `CallDataSource` (API), `SetVariable`, `ClearForm`.
*   **Feedback**: `ShowToast`, `ShowAlert`.
*   **Logic**: `If/Else`, `Loop`, `Wait` (Debounce).

## 3. Metadata Structure

Logic is serialized into the Page Metadata JSON.

```json
{
  "componentId": "btn_submit",
  "events": {
    "onClick": {
      "flowId": "flow_submit_order",
      "steps": [
        {
          "type": "ValidateForm",
          "formId": "form_order"
        },
        {
          "type": "CallApi",
          "apiId": "CreateOrder",
          "onSuccess": [
             { "type": "ShowToast", "msg": "Order Created!" },
             { "type": "Navigate", "route": "/orders" }
          ],
          "onError": [
             { "type": "ShowToast", "msg": "Failed", "level": "error" }
          ]
        }
      ]
    }
  }
}
```

## 4. Code Generation Strategy (TypeScript)

We do **not** run an interpreter in the browser for performance; we generate **TypeScript code**.

### 4.1 Generating the Flow
The generator converts the JSON flow into an `async` method in the Component Class.

**Generated Component (`order.generated.component.ts`):**

```typescript
export abstract class OrderGeneratedComponent {
    
    // Wire up the template event
    // <button (click)="handle_btn_submit_click()">Submit</button>

    async handle_btn_submit_click() {
        try {
            // Step 1: Validate
            if (!this.form_order.valid) {
                 return; 
            }

            // Step 2: Call API
            const result = await lastValueFrom(this.orderService.create(this.formModel));

            // Step 3a: Success
            this.notificationService.success("Order Created!");
            this.router.navigate(['/orders']);

        } catch (error) {
            // Step 3b: Error
            this.notificationService.error("Failed");
        }
    }
}
```

## 5. Client-Side Rules (Reactive Logic)

Beyond imperative flows (do X then Y), we need **Reactive Rules** (e.g., "Button is disabled IF Total < 50").

### 5.1 Expression Engine
We use **Reactive Binding** in the template generation.

*   **Designer Input**: User sets `Disabled` property to Expression: `TotalAmount < 50`.
*   **Generated HTML**:

```html
<button [disabled]="(totalAmount$ | async) < 50">Checkout</button>
```

### 5.2 Complex Visibility Rules
For complex visibility logic (e.g., "Show 'Discount Code' input only if 'HasVoucher' is checked AND 'User.IsVIP' is true"):

*   **Generator** creates a getter in TypeScript:
```typescript
get isDiscountInputVisible(): boolean {
    return this.hasVoucher && this.user?.isVip;
}
```
*   **Template**:
```html
<input *ngIf="isDiscountInputVisible" ... />
```

## 6. Security Sandbox

Unlike Server-Side rules, Client-Side logic runs in the user's browser.
*   **Principle**: Client logic is for **UX Only**, never for Security.
*   **Warning**: The generator will include comments warning developers: *"Validation logic here is for user feedback. Ensure API validates data independently."*

## 7. State Management (Variables)

Users can create "Page Variables" (Client-only state).
*   **UI**: "Variables" panel in Page Designer.
*   **Gen**: Creates simple properties on the Component:
    ```typescript
    // Page Variable: ShowDetailsModal
    showDetailsModal: boolean = false;
    ```
*   **Usage**: The Flow Designer action `SetVariable` simply generates `this.showDetailsModal = true;`.
