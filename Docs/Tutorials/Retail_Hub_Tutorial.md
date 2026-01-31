# Tutorial: Building an Omnichannel Retail Operations Hub

Retailers need to move fast. Today they sell shoes; tomorrow they add electronics. Each needs different data points.

## 👟 Phase 1: Dynamic Entity Scaffolding
Start with a base `Product` entity.
1. Use the **Entity Designer** to create `Product`.
2. Add common fields: `SKU`, `Color`, `BasePrice`.

## 📦 Phase 2: Rapid Extension (Pharma Category)
The retailer starts selling medication. You need `ExpiryDate` and `BatchNumber`.
1. Modify the `Product` entity.
2. Add the pharma-specific fields.
3. Click **Publish**.
4. The **Delta Management** system adds the columns without taking the point-of-sale system offline.

## 🎨 Phase 3: The "Inventory Commander" Dashboard
Build a workspace for store managers.
1. Use the **Page Designer**.
2. Add a **Custom Widget**: *Stock Heatmap*.
3. Bind the Map to a `DataOperationMetadata` query that uses **Aggregations** (`SUM(Quantity) GROUP BY Aisle`).

## 🛒 Phase 4: Order Orchestration
When an order is placed on the website:
1. **Workflow Trigger**: *OrderCreated*.
2. **Action**: Check `Inventory` across all physical stores.
3. **Action**: Reserve the item at the nearest store.
4. **Action**: Notify the store employee's mobile app (via *NotifyUserActivity*).

## 🛡️ Phase 5: Backward-Compatible Apps
The Point of Sale (POS) software in the stores is old and can't be updated until next year. It still expects a field called `Legacy_Price`.
1. In the **Metadata Virtualization** settings, create a mapping: `Legacy_Price` -> `Current_BasePrice`.
2. The old POS software continues to function perfectly against the new, refactored database because the **Compatibility Middleware** is virtualizing the old field name on the fly.
