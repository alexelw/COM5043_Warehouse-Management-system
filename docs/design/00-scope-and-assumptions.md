# 00 — Scope and Assumptions

## 1. System Purpose
The system provides a warehouse management platform that maintains accurate stock records, supports supplier and customer order workflows, and records financial transactions related to inventory movement.

Its primary objective is to improve stock visibility, reduce manual processing errors, and provide basic financial reporting for operational decision-making.

---

## 2. Actors

### Warehouse Staff
- View stock levels
- Record incoming deliveries
- Process outgoing orders

### Manager
- Monitor low stock alerts
- Review supplier and customer order history

### Administrator
- Access financial records
- Generate summary reports

---

## 3. Functional Scope (In Scope)

### Supplier Management
- Maintain supplier records
- Create and track purchase orders
- Record supplier transaction history

### Inventory Management
- Track stock levels
- Receive stock deliveries
- Detect low stock conditions

### Order Processing
- Create customer orders
- Update inventory after sale

### Financial Tracking
- Record sales and purchase transactions
- Generate summary financial reports

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
- Single warehouse location
- Orders cannot exceed available stock
- Deliveries may be partial; outstanding quantities are tracked until completed or cancelled

### Data
- Each product has one supplier
- Stock quantities are whole numbers
- Fixed currency (GBP)
- No tax or discount rules

### Usage
- Single active user at a time
- Local environment usage only

---

## 6. Non-Functional Constraints
- Strongly typed domain model (C#)
- User interface separated from business logic
- Business rules implemented in testable services/entities
- Modular and maintainable architecture
- Design supports future system expansion
