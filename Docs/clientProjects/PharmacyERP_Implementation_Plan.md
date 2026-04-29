# Pharmacy Chain ERP - Implementation Plan

## Goal Description
Implement a comprehensive **Pharmacy Chain ERP System** using the DynamicPlatform. This system will support multi-store operations, inventory management, prescription validation, POS billing, and online orders. The solution will leverage the platform's Entity Designer, Elsa Workflows, and Connectivity Hub.

## User Review Required
> [!NOTE]
> **Online-Only POS**: As per requirements, all POS terminals will be online. The Offline Sync Engine and local database complexities are excluded from this plan.

> [!NOTE]
> **External Integrations**: Specific API details for Insurance and Government Reporting are placeholders. These will be implemented as configurable Connectors.

## Proposed Changes

### 1. Domain Modeling (Core & Infrastructure)
We will define the following Domain Entities using the **Entity Designer**.

#### [NEW] `src/Core/Domain/StoreContext`
*   **Store**: `Id`, `Name`, `Location`, `LicenseNumber`, `IsOnlineEnabled`.
*   **Staff**: `Id`, `Name`, `Role` (Pharmacist, Cashier, Manager), `StoreId`.

#### [NEW] `src/Core/Domain/InventoryContext`
*   **Medicine**: `Id`, `Name`, `GenericName`, `Manufacturer`, `IsPrescriptionRequired`, `IsControlledSubstance`.
*   **Batch**: `Id`, `MedicineId`, `BatchNumber`, `ExpiryDate`, `MRP`, `PurchasePrice`, `StoreId`, `Quantity`.
*   **StockMovement**: `Id`, `Type` (Sale, Purchase, Expiry), `Quantity`, `ReferenceId`.

#### [NEW] `src/Core/Domain/SalesContext`
*   **Customer**: `Id`, `Name`, `Phone`, `LoyaltyPoints`, `InsuranceProviderId`.
*   **Prescription**: `Id`, `CustomerId`, `DoctorName`, `Status`, `ImageUrl`.
*   **Sale**: `Id`, `StoreId`, `CustomerId`, `TotalAmount`, `TaxAmount`, `Status` (Paid, Refunded).
*   **SaleItem**: `Id`, `SaleId`, `MedicineId`, `BatchId`, `Quantity`, `Price`.

#### [NEW] `src/Core/Domain/ProcurementContext`
*   **Supplier**: `Id`, `Name`, `ContactInfo`, `GstNumber`.
*   **PurchaseOrder**: `Id`, `SupplierId`, `StoreId`, `Status`, `ExpectedDate`.

### 2. Business Logic & Workflows (Elsa 3.1)
We will implement the following workflows using the **Workflow Designer**.

#### [NEW] `src/Workflows/PrescriptionValidation.json`
*   **Trigger**: `EntityCreated<Prescription>`
*   **Logic**:
    1.  Check `Medicine.IsControlledSubstance`.
    2.  If True -> **UserTask** (Pharmacist Approval).
    3.  If Valid -> Update Status "Verified".
    4.  If Invalid -> Send Notification (SMS/Email) to Customer.

#### [NEW] `src/Workflows/InventoryReplenishment.json`
*   **Trigger**: `StockLevelChanged` (when `Quantity < Threshold`)
*   **Logic**:
    1.  Calculate Reorder Quantity (`AvgDailySale * LeadTime`).
    2.  Find Preferred Supplier.
    3.  Create `PurchaseOrder` (Draft).
    4.  Notify Store Manager.

#### [NEW] `src/Workflows/ExpiryAlert.json`
*   **Trigger**: `Cron(Daily)`
*   **Logic**:
    1.  Query Batches expiring in < 30 days.
    2.  Email Report to Manager.
    3.  If < 7 days -> Apply "Near Expiry Discount" tag.

### 3. User Interfaces (Angular SPA)
We will generate three distinct portals using the **Page Designer**.

#### [NEW] `frontend/apps/store-pos`
*   **POS Screen**: Fast billing UI, Barcode Scanner integration (USB/Bluetooth), Batch Selection modal.
*   **Prescription Queue**: View Pending/Verified prescriptions.
*   **Stock Lookup**: Real-time inventory check across stores.

#### [NEW] `frontend/apps/admin-portal`
*   **Dashboard**: Sales Trends, Low Stock Alerts, Expiry Warnings.
*   **Master Management**: Medicine Catalog, Supplier List, Store Configuration.
*   **Reports**: GST Sales, Controlled Substance Audit.

#### [NEW] `frontend/apps/online-pharmacy`
*   **Catalog**: Search Medicines (Generic/Brand).
*   **Upload Prescription**: Drag-and-drop UI.
*   **Cart & Checkout**: Integrated with Payment Gateway.

### 4. Integration & Connectivity
We will implement custom connectors using the **Connectivity Hub**.

#### [NEW] `src/Connectors/PaymentGateway.cs`
*   Wraps Stripe/Razorpay API.
*   Methods: `InitializeTransaction`, `VerifyPayment`, `ProcessRefund`.

#### [NEW] `src/Connectors/GovtCompliance.cs`
*   Mock implementation for "Drug Control Department" reporting.
*   Methods: `SubmitControlledSubstanceReport`, `SubmitDailySales`.

### 7. User Roles & Security (RBAC)
We will implement a granular **Role-Based Access Control** system.

#### Roles & Responsibilities
1.  **Super Admin**: Full system access. Configure global settings, tax rules, and onboard new stores.
2.  **Regional Manager**: Read-only access to sales/inventory reports for stores in their assigned region.
3.  **Store Manager**: Full access to their specific store. Manage staff, approve stock requests, override discounts.
4.  **Pharmacist**: Validate prescriptions, dispense controlled substances, view customer medical history. (Requires license verification).
5.  **Cashier**: Billing & POS access only. Cannot modify stock or approve refunds without authorization.
6.  **Inventory Specialist**: Manage procurement, receive goods (GRN), and handle expiry stock destruction.
7.  **Delivery Agent**: Mobile-view access to assigned orders map and status updates.
8.  **Compliance Officer**: Read-only access to "Drug Control Reports" and "Audit Logs".
9.  **Customer**: Online portal access for ordering, tracking, and viewing prescription history.

#### Security Implementation
*   **Identity Provider**: Platform's built-in Identity or Azure AD integration.
*   **Role Claims**: Claims-based authorization (e.g., `[Authorize(Policy = "CanDispenseMedicine")]`).
*   **Audit Logging**: Track every critical action (especially by Pharmacists and Managers) with UserID and Timestamp.

### 8. Key Usage Scenarios (Role-Based Flows)
To ensure the system handles real-world complexity, we will implement support for these specific scenarios:

#### A. Pharmacist: Generic Substitution & Validation
*   **Scenario**: Customer presents a prescription for costly Brand X. Pharmacist suggests generic equivalent.
*   **System Feature**: POS "Substitute" button queries `InventoryContext` for medicines with same `GenericName`. Logs the switch for audit.

#### B. Regional Manager: Inter-Store Stock Transfer
*   **Scenario**: Store A is out of Insulin; Store B (5km away) has excess.
*   **System Feature**: New `StockTransferRequest` entity. Manager approves transfer. Workflow triggers `StockMovement` (Out from B, In to A) only upon receipt confirmation.

#### C. Inventory Specialist: Damaged Goods Receipt (GRN)
*   **Scenario**: 100 boxes arrive, 5 are crushed.
*   **System Feature**: GRN (Goods Receipt Note) screen allows marking specific quantity as "Damaged". Triggers automatic "Claim Workflow" with Supplier and writes to `StockMovement` (Type: Damaged).

#### D. Delivery Agent: COD Settlement
*   **Scenario**: Agent collects ₹5,000 cash for 3 orders. Returns to hub.
*   **System Feature**: Agent App shows "Cash in Hand". Cashier uses "Settlement Screen" to accept cash and mark orders as "Settled".

#### E. Customer: Partial Fulfillment
*   **Scenario**: Customer needs 10 strips, Store has 5.
*   **System Feature**: POS creates "Partial Sale". Remaining 5 are added to a "Pending Fulfillment" list associated with the Customer, triggering a `PurchaseOrder` for replenishment.

#### [NEW] `src/Connectors/EmailNotification.cs`
*   Wraps SMTP/SendGrid/AWS SES service.
*   Methods: `SendOrderConfirmation`, `SendLowStockAlert`, `SendPrescriptionStatus`.

### 5. Logistics Module (Online Orders)
We will implement the **Smart Delivery Partner Architecture** as defined in the requirements.

#### [NEW] `src/Core/Domain/LogisticsContext`
*   **OnlineOrder**: `Id`, `Status` (Created, Packed, OutForDelivery), `DeliveryAddress`, `Pincode`, `AssignedPartnerId`.
*   **DeliveryPartner**: `Id`, `Name`, `ApiEndpoint`, `IsExternal`.
*   **DeliveryCoverageZone**: `Id`, `PartnerId`, `Pincode`, `Priority` (for auto-selection).
*   **DeliveryAssignment**: `Id`, `OrderId`, `PartnerId`, `Status`, `TrackingNumber`.

#### [NEW] `src/Workflows/LogisticsWorkflows.json`
*   **OnlineOrderProcessing**: Triggered on `OrderCreated`. Validates stock -> Payment -> Invokes `DeliverySelection` logic -> Assigns Partner.
*   **DeliveryStatusSync**: Triggered by Webhook. Updates `DeliveryAssignment` status -> Notifies Customer via Email/SMS.

#### [NEW] `src/Connectors/LogisticsConnector.cs`
*   Wraps external delivery APIs (e.g., Dunzo, Shadowfax).
*   Methods: `CreateShipment`, `CancelShipment`, `TrackShipment`.

### 6. Discount & Pricing Module
We will implement a flexible **Discount Engine** to support various promotion types.

#### [NEW] `src/Core/Domain/PricingContext`
*   **DiscountRule**: `Id`, `Name`, `Type` (Percentage, FlatOff, BuyXGetY), `Value`, `StartDate`, `EndDate`, `MinPurchaseAmount`.
*   **ApplicableScope**: `Id`, `DiscountRuleId`, `TargetType` (Medicine, Category, Brand, WholeCart), `TargetId`.
*   **CouponCode**: `Id`, `Code`, `DiscountRuleId`, `UsageLimit`, `UsageCount`.

#### Logic & Integration
*   **Auto-Apply**: High-priority discounts (e.g., "Generic Medicine 15% Off") apply automatically at POS.
*   **Manual Override**: Store Manager can apply a discretionary "Goodwill Discount" (logged for audit).
*   **Loyalty Redemption**: Convert `Customer.LoyaltyPoints` to currency discount (e.g., 100 Points = ₹10).
*   **Expiry Clearance**: Nightly job applies "30% Off" rule to Batches expiring in < 30 days.

### 7. Data Seeding Strategy
To fully test the system with large data volumes, we will create a **Data Seeder**.

#### [NEW] `src/Infrastructure/DataSeeding/Seeder.cs`
*   **Customers**: Generate 100+ dummy customers with realistic names/addresses.
*   **Inventory**: Populate 50 Stores with 2,000 Medicines each (varying stock levels to trigger workflows).
*   **Orders**: Simulate 5,000 past orders to populate sales history and dashboards.
*   **Logistics**: Create 5 Delivery Partners and map 30 Pincodes to test auto-selection logic.

## Verification Plan

### Automated Tests
1.  **Domain Tests**: Unit tests for `StockMovement` logic (ensuring Batch quantity updates correctly).
2.  **Workflow Tests**: Integration tests triggering `PrescriptionCreated` and asserting that a UserTask is created for controlled substances.
3.  **Connector Tests**: Mocked tests for Payment Gateway to ensure correct error handling.

### Manual Verification
1.  **POS Flow**:
    *   Open POS UI.
    *   Scan an item (simulate barcode input).
    *   Complete Sale.
    *   Verify Stock reduced in `InventoryContext`.
2.  **Workflow Execution**:
    *   Upload a dummy prescription for a controlled substance.
    *   Log in as "Pharmacist".
    *   Verify an "Approval Task" appears in the dashboard.
    *   Approve and verify Prescription status changes.
