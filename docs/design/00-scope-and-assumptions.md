# 00 — Scope and Assumptions

## 1. System Purpose
The system provides a warehouse management platform for accurate stock records and order workflows. It records financial transactions related to inventory movement.

Its primary objective is to improve stock visibility and reduce manual processing errors. It also provides basic financial reporting for operational decision-making.

---

## 2. Actors

### Warehouse Staff
Warehouse staff can view stock levels and record incoming deliveries. They also process outgoing orders.

### Manager
Managers monitor low stock alerts and review supplier and customer order history.

### Administrator
Administrators access financial records and generate summary reports.

---

## 3. Functional Scope (In Scope)

### Supplier Management
Maintain supplier records and create or track purchase orders. Record supplier transaction history.

### Inventory Management
Track stock levels and receive stock deliveries. Detect low stock conditions.

### Order Processing
Create customer orders and update inventory after sale.

### Financial Tracking
Record sales and purchase transactions. Generate summary financial reports.

---

## 4. Out of Scope
- Multi-warehouse support
- Authentication and permissions system
- Real payment processing
- Full customer returns/refunds workflow (beyond financial void/reversal)
- External integrations or supplier APIs
- Cloud deployment
- Barcode scanning hardware support
- Performance optimisation for large-scale usage

---

## 5. Assumptions

### Operational
Assume a single warehouse location. Orders cannot exceed available stock. Deliveries may be partial; outstanding quantities are tracked until completed or cancelled.

### Data
Each product has one supplier. Stock quantities are whole numbers. Fixed currency (GBP). No tax or discount rules.

### Usage
Single active user at a time. Local environment usage only.

---

## 6. Non-Functional Constraints
Strongly typed domain model (C#). User interface separated from business logic. Business rules implemented in testable services/entities. Modular and maintainable architecture. Design supports future system expansion.
