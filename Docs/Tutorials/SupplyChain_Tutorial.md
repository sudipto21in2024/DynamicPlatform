# Tutorial: Building a Supply Chain Control Tower

This system provides a single pane of glass for multi-modal logistics, connecting ship, air, and rail data.

## 🛳️ Phase 1: External API Integration
Instead of creating internal database tables, use the **Connector Hub**:
1. Register a **REST Source**: *Maersk Shipping API*.
2. Use the **Data Mapper** to map the JSON response (`vessel_name`, `estimated_arrival`) to the platform's internal `VesselMetadata`.
3. Use the **API Data Provider** to query this in real-time.

## 📡 Phase 2: Live Tracking Dashboard
Build a dashboard using **Custom Widgets**:
1. Place a **Map Widget** on the canvas.
2. Bind the data source to the *Maersk Shipping API*.
3. Add a **Progress Widget** to show "Percentage of Voyage Complete."

## ⚠️ Phase 3: The "Exception Management" Workflow
Automate reaction to delays:
1. **Trigger**: *TimerTrigger* (every 1 hour).
2. **Action**: *ExecuteDataQuery* (API Source) to check vessel statuses.
3. **Branch**: If `ETA > PromisedDate + 24hrs`:
    - **Action**: Call *InventoryService* to check if stock will run out.
    - **Action**: If yes, trigger an "Expedited Search" for alternative suppliers.

## 🌍 Phase 4: Multi-Tenant Privacy
A Control Tower serves multiple logistics clients.
1. Use the **Tenant Isolation** feature.
2. Each client (e.g., Walmart, Target) sees only their own shipments.
3. The platform injects the `TenantId` automatically into every API request and database query.

## 🛠️ Phase 5: Schema Extension for New Regions
When expanding to the EU, you need to track `CarbonTax` per container.
1. Add the `CarbonTax` field to the `Container` entity.
2. Use **Metadata Virtualization** so that the US-based reporting team (still using the v1.0 interface) doesn't see or break because of the new data requirements.
