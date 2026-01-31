# Tutorial: Building a Mortgage & Loan Origination System

Loans are long-running (15-30 years). The logic used to approve a loan in 2024 must remain valid for that specific loan, even if the bank's rules change in 2026.

## 🏦 Phase 1: The Loan Approval Flow
1. **Intake**: A **Form Designer** page that captures `ApplicantSalary`, `CreditScore`, `PropertyAddress`.
2. **Scoring Logic**: Use the **Business Rule Engine**:
   - `Rule: HighRisk` -> If `CreditScore < 600` AND `LoanToValue > 80%` THEN `Status = Rejected`.
   - `Rule: FastTrack` -> If `Salary > 200k` AND `CreditScore > 800` THEN `AutoApprove = True`.

## ⏳ Phase 2: Long-Running States (Elsa)
Mortgage approval can take weeks.
1. Use Elsa's **Wait Activity** for "External Document Upload."
2. The workflow enters a "Suspended" state.
3. When the borrower uploads their tax return, the workflow "Resumes" automatically at the exact same point.

## 🔄 Phase 3: "Version Pinning" for Compliance
In 2025, the bank changes its `InterestRate` calculation logic.
1. You publish a new **Project Snapshot** (v2.0).
2. For **Active Loans** started in 2024, the platform uses the **v1.0 Snapshot** for all calculations.
3. For **New Loans**, it uses v2.0.
4. **Metadata Virtualization** ensures the v1.0 logic sees the database correctly even after you've renamed fields in v2.0.

## 📁 Phase 4: Document Generation (ClosedXML/QuestPDF)
1. At the end of the workflow, generate the **Closing Disclosure**.
2. Map data from `Applicant`, `Property`, and `Loan` into a pre-defined Excel template.
3. Convert to **PDF** and lock it with a digital signature (integration via Connector Hub).

## 🛡️ Phase 5: Audit & Rollback
If a regulatory auditor asks why a loan was rejected:
1. Load the specific **Project Snapshot** linked to that loan application.
2. View the exact **Business Rules** and **Workflow Definition** that were active at that microsecond.
3. Roll back any property rename or logic change to "replay" the rejection exactly as it happened.
