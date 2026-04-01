# 01 — System Requirements (Use-Cases)

Defines the functional behaviour of the Warehouse Management System (WMS).
Each use-case represents a system operation. It informs class methods and validation rules; test cases derive from it.

---

## 1. Actors and Roles

The system supports predefined user roles. Roles determine available operations.
Authentication mechanisms are out of scope.

| Role            | Responsibilities                                                |
|-----------------|-----------------------------------------------------------------|
| Warehouse Staff | Manage stock movements, deliveries, and customer orders         |
| Manager         | Manage suppliers, purchase orders, and monitor stock activity   |
| Administrator   | Manage financial records, reporting, and data export            |
| System          | Automatically records derived financial transactions            |

---

## 2. Use-Case Overview

| ID   | Use Case                          | Role            | Goal                                              |
|------|-----------------------------------|-----------------|---------------------------------------------------|
| UC01 | Create Supplier                   | Manager         | Register supplier details                         |
| UC02 | Update Supplier                   | Manager         | Edit supplier details                             |
| UC03 | Delete Supplier                   | Manager         | Remove a supplier record                          |
| UC04 | Create Purchase Order             | Manager         | Request stock from supplier                       |
| UC05 | Receive Delivery                  | Warehouse Staff | Receive full delivery and increase stock          |
| UC06 | View Stock Levels                 | Warehouse Staff | Check product availability                        |
| UC07 | Detect Low Stock                  | Manager         | Identify items needing reorder                    |
| UC08 | Create Customer Order             | Warehouse Staff | Sell products to customer                         |
| UC09 | Record Financial Transaction      | System          | Track income and expenses automatically           |
| UC10 | Generate Financial Report         | Administrator   | View summary business data                        |
| UC11 | Export Report to File             | Administrator   | Save readable report output                       |
| UC12 | View Order History                | Manager         | Review past supplier and customer orders          |
| UC13 | Cancel Customer Order             | Warehouse Staff | Cancel an unfulfilled customer order              |
| UC14 | Cancel Purchase Order             | Manager         | Cancel a pending purchase order                   |
| UC15 | Partial Delivery / Backorder      | Warehouse Staff | Receive part of a purchase order                  |
| UC16 | Void / Reverse Financial Transaction | Administrator| Remove or offset a financial transaction         |
| UC17 | Adjust Stock                      | Warehouse Staff | Correct inventory levels manually                 |

---

## 3. Detailed Use-Cases

---

### UC01 — Create Supplier
**Role:** Manager

**Preconditions:**
- Supplier does not already exist

**Main Flow:**
1. Manager enters supplier details
2. System validates required fields
3. System stores supplier record

**Postconditions:**
- Supplier available for purchase orders

**Alternate Flows:**
- Duplicate supplier → reject creation

---

### UC02 — Update Supplier
**Role:** Manager

**Preconditions:**
- Supplier exists

**Main Flow:**
1. Manager selects supplier
2. Manager edits supplier details
3. System validates required fields
4. System updates supplier record

**Postconditions:**
- Supplier details updated

**Alternate Flows:**
- Supplier not found → error

---

### UC03 — Delete Supplier
**Role:** Manager

**Preconditions:**
- Supplier exists
- No open or partially received purchase orders

**Main Flow:**
1. Manager selects supplier
2. Manager confirms deletion
3. System removes supplier record

**Postconditions:**
- Supplier removed from active records

**Alternate Flows:**
- Supplier not found → error
- Open purchase orders exist → reject deletion

---

### UC04 — Create Purchase Order
**Role:** Manager

**Preconditions:**
- Supplier exists
- Products exist

**Main Flow:**
1. Manager selects supplier
2. Manager adds product quantities
3. System validates quantities
4. System creates purchase order
5. System records pending expense

**Postconditions:**
- Purchase order created with status *Pending*

**Alternate Flows:**
- Invalid quantity → reject entry

---

### UC05 — Receive Delivery (Full)
**Role:** Warehouse Staff

**Preconditions:**
- Purchase order exists
- Purchase order status is *Pending*

**Main Flow:**
1. Staff selects purchase order
2. System validates outstanding quantities
3. Staff confirms received items
4. System records goods receipt
5. System increases stock levels
6. System records stock movement
7. System updates purchase order status to *Completed*
8. System records financial expense

**Postconditions:**
- Inventory updated
- Purchase order completed

**Alternate Flows:**
- Quantity mismatch → reject delivery
- Purchase order not found → error

---

### UC06 — View Stock Levels
**Role:** Warehouse Staff

**Main Flow:**
1. User requests stock list
2. System displays product quantities

**Postconditions:**
- None (read-only)

---

### UC07 — Detect Low Stock
**Role:** Manager

**Main Flow:**
1. System evaluates stock levels against reorder thresholds
2. System identifies low stock items
3. System displays alert list

**Postconditions:**
- Manager informed of reorder requirement

---

### UC08 — Create Customer Order
**Role:** Warehouse Staff

**Preconditions:**
- Sufficient stock available

**Main Flow:**
1. Staff selects products and quantities
2. System validates stock availability
3. System reduces inventory
4. System records sale transaction
5. System stores customer order

**Postconditions:**
- Order stored
- Stock updated

**Alternate Flows:**
- Insufficient stock → reject order

---

### UC09 — Record Financial Transaction
**Role:** System

**Trigger:**
- Automatically triggered by sales, purchases, returns, or adjustments

**Main Flow:**
1. System determines transaction type (Income or Expense)
2. System stores financial record with timestamp and reference

**Postconditions:**
- Transaction available for reporting

---

### UC10 — Generate Financial Report
**Role:** Administrator

**Main Flow:**
1. Administrator selects report type and date range
2. System aggregates transaction data
3. System displays totals and summaries

**Postconditions:**
- Report generated

---

### UC11 — Export Report to File
**Role:** Administrator

**Main Flow:**
1. Administrator selects export option
2. System generates readable file (TXT or JSON)
3. System saves file locally

**Postconditions:**
- File created containing report data

---

### UC12 — View Order History
**Role:** Manager

**Main Flow:**
1. Manager selects order history
2. System retrieves supplier and customer orders
3. System displays historical records

**Postconditions:**
- Historical records available

---

### UC13 — Cancel Customer Order
**Role:** Warehouse Staff

**Preconditions:**
- Customer order exists
- Order status is *Draft* or *Confirmed*

**Main Flow:**
1. Staff selects customer order
2. Staff confirms cancellation
3. System updates order status to *Cancelled*
4. System restores inventory quantities
5. System records stock movement
6. System voids or reverses linked financial transaction (UC16)

**Postconditions:**
- Order cancelled
- Inventory and financials corrected

**Alternate Flows:**
- Order already completed → reject cancellation

---

### UC14 — Cancel Purchase Order
**Role:** Manager

**Preconditions:**
- Purchase order exists
- Purchase order status is *Pending* or *Partially Received*

**Main Flow:**
1. Manager selects purchase order
2. Manager confirms cancellation
3. System cancels remaining quantities
4. System updates purchase order status to *Cancelled*
5. System voids pending financial transactions (UC16)

**Postconditions:**
- Purchase order cancelled

**Alternate Flows:**
- Purchase order completed → reject cancellation

---

### UC15 — Partial Delivery / Backorder
**Role:** Warehouse Staff

**Preconditions:**
- Purchase order exists
- Outstanding quantities remain

**Main Flow:**
1. Staff selects purchase order
2. Staff enters received quantities
3. System validates quantities
4. System updates inventory for received items
5. System records stock movement
6. System updates outstanding quantities
7. System sets order status to *Partially Received*
8. System records partial financial expense

**Postconditions:**
- Inventory partially updated
- Purchase order remains open

**Alternate Flows:**
- Received quantity exceeds outstanding → reject entry

---

### UC16 — Void / Reverse Financial Transaction
**Role:** Administrator

**Preconditions:**
- Financial transaction exists
- Transaction not already voided

**Main Flow (Void):**
1. Administrator selects transaction
2. Administrator provides reason
3. System marks transaction as *Voided*
4. System excludes transaction from reports

**Main Flow (Reverse):**
1. Administrator selects transaction
2. System creates offsetting reversal transaction
3. System links reversal to original transaction

**Postconditions:**
- Financial totals corrected
- Audit trail preserved

---

### UC17 — Adjust Stock
**Role:** Warehouse Staff

**Preconditions:**
- Product exists
- Adjustment reason provided

**Main Flow:**
1. Staff selects product
2. Staff enters adjustment quantity and reason
3. System validates resulting stock level
4. System updates inventory
5. System records stock movement (Adjustment)
6. System optionally records financial impact

**Postconditions:**
- Inventory corrected
- Adjustment fully auditable

**Alternate Flows:**
- Adjustment would result in negative stock → reject
