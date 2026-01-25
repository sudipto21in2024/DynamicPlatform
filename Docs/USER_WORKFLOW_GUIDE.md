# Professional Guide to Workflow Automation (Elsa 3.1)

This guide provides an in-depth look at implementing business logic automation within the DynamicPlatform. By leveraging the **Elsa 3.1** engine, you can move away from static data entry and create dynamic, "living" applications that respond to real-world events.

---

## 1. Core Architecture: How it Works
When you define a workflow in the Studio, it is saved as a **Declaration**. Upon project publication, this declaration is embedded into your application's binary. At runtime, the application "Listens" for specific triggers and executes the sequence of nodes defined in your design.

### 1.1 Data Flow & Variables
Workflows use a "Contextual Memory" system. 
- **Trigger Data**: When an entity event occurs (e.g., Order Created), the entire Order object is passed into the workflow as a variable named `Order`.
- **Output Mapping**: Every node can produce an output (like an API result or a calculation). You can name these outputs and reference them in later nodes using the syntax `[NodeName.Output]`.

---

## 2. Granular Node Library

### 2.1 Triggers (The "When")
- **Entity Events**: `Created`, `Updated`, `Deleted`. These are high-performance hooks into the Database.
- **HTTP Webhook**: Generates a unique URL. When called with a POST request, it starts the workflow and passes the JSON body as input.
- **Timer/Cron**: Executes on a schedule (e.g., "Daily at 9:00 AM").
- **Manual Signal**: Triggers when a user clicks a custom button in the UI.

### 2.2 Logic & Flow (The "Think")
- **Condition (If/Else)**: Evaluate expressions (e.g., `Value > 500`).
- **Switch**: Multiple branches based on a value (e.g., Status: "Open", "Closed", "Pending").
- **Parallel Choice**: Run multiple paths simultaneously and wait for all (or one) to complete.
- **Loop**: Iterate over a collection (e.g., "For each LineItem in the Order").

### 2.3 Actions (The "Do")
- **Entity Action**: Create, Update, or Delete records in ANY entity.
- **Custom Connector**: Invoke your own logic (e.g., `StripePayment`, `SendGridEmail`).
- **HTTP Request**: Call an external 3rd party API.
- **Scripting**: Write a small snippet of JavaScript or Liquid to transform data.

---

## 3. Deep Dive: The "Order Fulfillment" Process

Let's break down exactly how to configure the most common business scenario.

### Step-by-Step Configuration:
1.  **Placement**: Open **Workflows** -> **New Workflow** -> `GlobalOrderHandler`.
2.  **Trigger Setup**: Add `EntityCreatedTrigger<Order>`.
    *   This node automatically registers itself. Any time an Order is saved to the DB (via API or UI), this workflow wakes up.
3.  **Validation**: Add an **HTTP Activity** node to call a "Fraud Check" service.
    *   Input: Pass `Order.CustomerEmail`.
    *   Output: Name it `FraudResult`.
4.  **Decision**: Add an **If** node.
    *   Condition: `FraudResult.Score < 0.5`.
5.  **Branching (False/High Risk)**:
    *   Action: Update `Order.Status = "Flagged"`.
    *   Action: Send email to security team.
6.  **Branching (True/Safe)**:
    *   Action: Call `PaymentGatewayConnector`.
    *   Action: Update `Order.Status = "Paid"`.
    *   Action: Trigger another workflow `ShippingManifestGen`.

---

## 4. Complex Enterprise Scenarios (Technical Breakdown)

### 4.1 Scenario: The "Smart" JIT (Just-In-Time) Inventory
Traditional JIT is static. **Smart JIT** predicts based on multiple data points.
- **Trigger**: `InventoryItem.StockLevel` Changed.
- **Stage 1 (Analysis)**: Query last 30 days of `OrderHistory` for this item.
- **Stage 2 (Logic)**: Use a **JavaScript Node** to calculate `BurnRate`. 
    *   *Formula: `(TotalSold / 30) * LeadTimeDays`.*
- **Stage 3 (Decision)**: If `StockLevel < (BurnRate + Buffer)`, proceed to reorder.
- **Stage 4 (Action)**: Automated Purchase Order creation and email dispatch to the optimal supplier.

### 4.2 Scenario: Escalating Approval Flow
**Case**: Employee Expense Reimbursement.
- **Level 1**: Manager has 24 hours to approve.
- **Wait Node**: Use a **Delay** activity or a **Signal** activity.
- **Escalation Logic**: If the `Delay` expires (Timed Out), automatically bypass the Manager and send the request to the Department Head.
- **Notification**: Send a Slack message to the Manager: *"Your approval for Request #123 has timed out and was escalated."*

### 4.3 Scenario: Data Enrichment Pipeline
**Case**: A new Lead enters the system via a website form.
- **Trigger**: `Lead` Created.
- **Action 1**: Call **Clearbit API** to find the company's size and industry based on email domain.
- **Action 2**: Call **Apollo API** to find the Lead's LinkedIn profile.
- **Action 3**: Use **Gemini AI Connector** to summarize the findings and categorize the lead (e.g., "High Intent - Tech Sector").
- **Result**: Update `Lead` record with all enriched data before the Sales rep even opens the CRM.

---

## 6. Blending Rules with Workflows (Advanced)

DynamicPlatform allows you to "blend" your **Validation Rules** with **Workflows** to create intelligent, self-correcting data pipelines.

### 6.1. Workflow-Triggered Validation
Instead of just showing an error to the user when a rule fails (like "Invalid Email Format"), the repository can trigger a **ValidationFailed** workflow. 

**Example**:
-   **Trigger**: `OrderValidationFailed`.
-   **Context**: Passes the invalid Order object and the list of exceeded rules.
-   **Action**: Automatically flag the Customer's account and send a Slack notification to the Support team with a "Manual Correction" link.

### 6.2. Evaluating Rules inside a Workflow
You can also run your rules manually inside any workflow node. This is useful for **Scheduled Reports** where you want to scan existing data for rule violations that might have occurred due to manual database edits or legacy imports.

---

## 7. Troubleshooting & Monitoring

### 5.1 The Workflow Journal
Every published app includes a hidden administrative UI for monitoring.
- **Execution Log**: See every node the workflow passed through.
- **Variable Inspection**: See the exact value of the Order or API result at any step of the process.
- **Fault Handling**: If a 3rd party API (like Stripe) is down, the workflow will enter a `Faulted` state. You can "Retry" from that specific node once the service is back up.

### 5.2 Best Design Practices
1.  **Idempotency**: Ensure that if a workflow runs twice (e.g., after a manual retry), it doesn't charge the customer twice. 
2.  **Naming**: Name your nodes clearly (e.g., `NotifyCustomer` vs `Email1`). 
3.  **Comments**: Use the "Notes" property on complex nodes to explain the business rationale behind the logic.
