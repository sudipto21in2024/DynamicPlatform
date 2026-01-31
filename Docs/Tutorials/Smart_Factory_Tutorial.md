# Tutorial: Building a Smart Factory (IoT) Monitoring Dashboard

In a manufacturing plant, "Downtime is Death." This tutorial covers monitoring thousands of sensors and predicting failures.

## 🌡️ Phase 1: High-Volume Data Intake
1. Create a `SensorReading` entity: `MachineId`, `Temperature`, `Vibration`, `Timestamp`.
2. This table will grow into millions of rows.

## 📊 Phase 2: Dynamic Aggregations
You can't plot 10 million dots. You need summaries.
1. Create a **Data Operation**:
   - `Type`: *Aggregate*.
   - `Fields`: `MIN(Temp)`, `MAX(Temp)`, `AVG(Temp)`.
   - `GroupBy`: *Hour* of `Timestamp`.
2. Bind this query to a **Line Chart Widget**.

## 🔴 Phase 3: The "Predictive Maintenance" Workflow
Detect anomalies before the machine breaks:
1. **Trigger**: *EntityCreated* on `SensorReading`.
2. **Action**: *ExecuteDataQuery* to get the average vibration of this machine over the last 24 hours.
3. **Logic**: If `CurrentVibration > AVG(Vibration) * 1.5`:
    - **Action**: *NotifyUser* "Warning: Machine B12 showing unusual vibration."
    - **Action**: Create an automated `MaintenanceTask` entity.

## ⚙️ Phase 4: Control Hub Page
Design a high-density "Operations Page":
1. Use **Interactive Widgets** that allow the manager to click a machine on a floor plan and see its live sensor data (via **External API** connection to the PLC).
2. Integrate **Micro-Animations** to show machine status (Green Pulse = Running, Red Blink = Warning).

## 🧬 Phase 5: Zero-Downtime Evolution
You need to change the data type of `Vibration` from a *Low-Precision Int* to a *High-Precision Decimal*.
1. Use the **Entity Designer** to change the type.
2. The **Delta Engine** performs a safe type migration with data conversion logic.
3. The **Metadata Virtualization Middleware** ensures that the old "Alerting Rules" (which expect an Int) don't crash when they receive a Decimal, by performing on-the-fly casting.
