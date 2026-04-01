# 05 — Validation Rules and Invariants

Lists the business rules that must always be enforced by the system.
Rules are grouped by domain area and include where enforcement will occur.

Legend:
- Domain = entity/aggregate methods enforce rule
- Application = use-case service enforces workflow rule
- API = request shape validation (required fields, types)

---

## 1. Supplier Rules

### VR-SUP-01: Supplier must have required details
- Rule: Supplier name is required; at least one contact method should exist (email/phone/address).
- Enforced in: Domain (constructor/factory) + API (request validation)

### VR-SUP-02: Supplier must be unique
- Rule: Supplier is uniquely identified by `supplier_id` (system-generated); names are not required to be unique.
- Enforced in: DB (PK)

---

## 2. Product & Inventory Rules

### VR-PROD-01: SKU must be unique
- Rule: SKU is unique across products.
- Enforced in: Application + DB unique constraint

### VR-INV-01: Stock must never be negative
- Rule: Quantity on hand cannot go below 0.
- Enforced in: Domain (`DecreaseStock`) + Application (pre-check)

### VR-INV-02: Stock movement quantity must be valid
- Rule: Receipt/Issue quantities must be > 0; Adjustment quantity must be a non-zero integer (sign indicates increase/decrease).
- Enforced in: Domain + API

### VR-INV-03: Low stock threshold must be >= 0
- Rule: ReorderThreshold cannot be negative.
- Enforced in: Domain

### VR-INV-04: Stock adjustments must include a reason
- Rule: Manual adjustments require a non-empty reason and create an Adjustment stock movement.
- Enforced in: Application + API

---

## 3. Purchase Order Rules

### VR-PO-01: Purchase order must have at least one line
- Rule: A PO cannot be created/ordered without lines.
- Enforced in: Domain (aggregate validation) + Application

### VR-PO-02: PO line quantity must be > 0
- Enforced in: Domain + API

### VR-PO-03: PO status transitions must be valid
- Rule: Pending -> Partially Received -> Completed (Cancel allowed from Pending/Partially Received).
- Enforced in: Domain

### VR-PO-04: Goods receipt must reference an existing PO
- Enforced in: Application (repository lookup) + DB FK constraint

### VR-PO-05: Received quantities must not exceed ordered quantities
- Enforced in: Domain (PO apply receipt) + Application

### VR-PO-06: Partial delivery updates status correctly
- Rule: A delivery that does not complete all quantities sets status to Partially Received.
- Enforced in: Domain + Application

---

## 4. Customer Order Rules

### VR-CO-01: Customer order must have at least one line
- Enforced in: Domain + Application

### VR-CO-02: Order line quantity must be > 0
- Enforced in: Domain + API

### VR-CO-03: Order cannot be confirmed if stock is insufficient
- Enforced in: Application (check stock) + Domain (confirm rules)

### VR-CO-04: Customer order status transitions must be valid
- Rule: Draft -> Confirmed (Cancel allowed from Draft or Confirmed).
- Enforced in: Domain

---

## 5. Finance Rules

### VR-FIN-01: Transaction amount must be > 0
- Enforced in: Domain (Money/Transaction)

### VR-FIN-02: Currency is fixed to GBP
- Rule: All money values use GBP (product costs, order prices, transactions).
- Enforced in: Domain (Money) + DB defaults/constraints

### VR-FIN-03: Transactions must reference a business event
- Rule: Each transaction has ReferenceType + ReferenceId linked to PO/Order.
- Enforced in: Application

### VR-FIN-04: Transactions can be voided or reversed
- Rule: Void marks status as Voided and excludes the transaction from reports; reverse creates an offsetting transaction linked to the original.
- Enforced in: Application + Domain

### VR-FIN-05: Purchase orders create pending expenses
- Rule: Creating a PO records a Pending expense; receipts post the expense for received items; cancelling a PO voids remaining pending expense.
- Enforced in: Application

---

## 6. Report Export Rules

### VR-REP-01: Export format must be TXT or JSON
- Enforced in: Application + Domain (enum)

### VR-REP-02: Export event must be logged
- Rule: Every export produces a ReportExport record with timestamp and file path.
- Enforced in: Application

---

## 7. Mapping to Tests (initial plan)

The table is used as the basis for unit tests later.

| Rule ID | Suggested test type |
|---|---|
| VR-SUP-01 | API validation test supplier create requires name + contact |
| VR-SUP-02 | DB schema/integration test supplier_id uniqueness (PK) |
| VR-PROD-01 | DB unique constraint test SKU |
| VR-INV-01 | Unit test Product stock decrease non-negative |
| VR-INV-02 | Unit test StockMovement quantity rules (Adjustment may be negative) |
| VR-INV-03 | Unit test Product reorder threshold >= 0 |
| VR-INV-04 | API/service test stock adjustment requires reason |
| VR-PO-01 | Unit test PurchaseOrder requires at least one line |
| VR-PO-02 | API/domain test PO line quantity > 0 |
| VR-PO-03 | Unit test PO status transitions |
| VR-PO-04 | Application service test receipt references existing PO |
| VR-PO-05 | Unit test PO receipt application not exceed ordered |
| VR-PO-06 | Application test partial receipt sets Partially Received |
| VR-CO-01 | Unit test CustomerOrder requires at least one line |
| VR-CO-02 | API/domain test order line quantity > 0 |
| VR-CO-03 | Application service test place order insufficient stock |
| VR-CO-04 | Unit test order status transitions |
| VR-FIN-01 | Unit test transaction amount > 0 |
| VR-FIN-02 | Unit test Money currency fixed to GBP + DB default check |
| VR-FIN-03 | Service test transaction references business event |
| VR-FIN-04 | Service test void/reversal updates status and links |
| VR-FIN-05 | Service test PO creates pending expense; receipt posts |
| VR-REP-01 | Unit test ReportExport format enum (TXT/JSON) |
| VR-REP-02 | Application service test export logs ReportExport |
