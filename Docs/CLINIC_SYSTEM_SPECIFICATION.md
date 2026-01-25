# Domain Specification: Multi-Doctor Clinic Appointment System

This document outlines the metadata, business rules, and workflows required to implement a comprehensive Multi-Doctor Clinic Appointment System using the DynamicPlatform low-code engine.

## 1. System Overview
The system enables a clinic to manage multiple doctors, patient records, appointment scheduling, and automated notifications. It supports three primary roles: **Admin**, **Doctor**, and **Patient**.

## 2. Entity Model (Metadata)

### A. Specialization
Defines the medical fields available in the clinic.
*   `Name` (String, Required) - e.g., Cardiology, Pediatrics.
*   `Description` (String)

### B. Doctor
*   `FullName` (String, Required)
*   `Email` (String, Required, Regex: Email)
*   `Phone` (String, Required)
*   `SpecializationId` (Guid, Relation: ManyToOne -> Specialization)
*   `ConsultationFee` (Decimal, Required)
*   `IsActive` (Boolean, Default: true)

### C. Patient
*   `FullName` (String, Required)
*   `Email` (String, Required, Unique)
*   `Phone` (String, Required)
*   `DateOfBirth` (DateTime, Required)
*   `MedicalHistorySummary` (String)

### D. Appointment
*   `PatientId` (Guid, Relation: ManyToOne -> Patient)
*   `DoctorId` (Guid, Relation: ManyToOne -> Doctor)
*   `AppointmentDate` (DateTime, Required)
*   `Status` (String, Default: 'Pending') - Options: Pending, Confirmed, Completed, Cancelled.
*   `Notes` (String)
*   `FeeAmount` (Decimal) - Set during creation based on Doctor's fee.

---

## 3. Business Rules & Validations

| Entity | Field | Rule Type | Logic / Message |
| :--- | :--- | :--- | :--- |
| Doctor | Email | Regex | Standard email pattern validation. |
| Appointment | AppointmentDate | Range | Date must be in the future. |
| Appointment | Status | Range | Must be one of: Pending, Confirmed, Completed, Cancelled. |

---

## 4. Workflows (Elsa)

### Workflow 1: Appointment Booking Notification
*   **Trigger**: `OnCreated<Appointment>`
*   **Steps**:
    1.  **Read Context**: Fetch Doctor and Patient details.
    2.  **Notification**: Send internal "Signal" to Dashboard.
    3.  **External**: (Future) Call Email Connector to notify patient.

### Workflow 3: Appointment Overlap Detection (Validation)
*   **Trigger**: `OnCreating<Appointment>` (Pre-persistence Hook)
*   **Steps**:
    1.  **Context**: Read `DoctorId`, `AppointmentDate`, and calculated `EndDate` (e.g., +30 mins).
    2.  **Query**: Search for existing `Appointments` for the same `Doctor` where:
        *   `Status` is 'Confirmed' OR 'Pending'.
        *   Times overlap with the new slot.
    3.  **Decision**: 
        *   If count > 0: **Fail** the workflow and return a "Validation Alert" to the UI.
        *   If count == 0: **Proceed** to creation.

---

## 5. Security & Roles

| Role | Permissions |
| :--- | :--- |
| **Admin** | Full CRUD on Doctors, Patients, Specializations, and Appointments. |
| **Doctor** | Read all Patients; Read/Update own Appointments. |
| **Patient** | Read/Update own Profile; Read/Create own Appointments. |

---

## 6. End-to-End Test Scenario (E2E)

1.  **Setup**: Create "Cardiology" Specialization.
2.  **Setup**: Create Doctor "Dr. Smith" assigned to Cardiology.
3.  **Action**: Register Patient "John Doe".
4.  **Action**: Book an Appointment for John Doe with Dr. Smith for next Tuesday.
5.  **Verification**: 
    *   Check if `Status` is 'Pending'.
    *   Verify `FeeAmount` matches Dr. Smith's fee via Workflow.
    *   Verify Appointment appears in Dr. Smith's schedule.
