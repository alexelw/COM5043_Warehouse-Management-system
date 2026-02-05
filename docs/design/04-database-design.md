# 04 — Database Design

This document defines the relational database design for the WMS using **MySQL**.
The database stores operational data for suppliers, inventory, purchasing, customer orders,
and financial records. The schema is designed to map cleanly onto the domain model while
keeping referential integrity and supporting the required use-cases (UC01–UC17).

---

## 1. Database Approach

- Database engine: **MySQL 8**
- Persistence approach: **EF Core with explicit configuration mappings**
- Strategy:
  - Domain model remains independent of the database
  - Persistence models / configurations map domain entities to tables
  - All cross-entity lookups use foreign keys, except polymorphic `reference_type/reference_id`
    pairs in `stock_movements` and `financial_transactions` (validated in the application)

---

## 2. Entity-to-Table Mapping

| Domain Concept | Table(s) |
|---|---|
| Supplier | `suppliers` |
| Product | `products` |
| PurchaseOrder | `purchase_orders`, `purchase_order_lines` |
| GoodsReceipt | `goods_receipts`, `goods_receipt_lines` |
| CustomerOrder | `customers`, `customer_orders`, `customer_order_lines` |
| StockMovement | `stock_movements` |
| FinancialTransaction | `financial_transactions` |
| ReportExport | `report_exports` |
| User/Role | `users` |

---

## 3. Keys, Relationships, and Integrity

### Core rules enforced by schema
- Line tables must reference a valid parent (FK constraints)
- Quantity fields must be positive (CHECK constraints where supported), except stock adjustments
  which allow non-zero negative quantities
- Deleting a parent should not silently delete history:
  - Use **RESTRICT** for orders/receipts/transactions
  - Use **CASCADE** only for child line records where appropriate

### Key relationships
- `products.supplier_id` → `suppliers.supplier_id`
- `purchase_orders.supplier_id` → `suppliers.supplier_id`
- `purchase_order_lines.purchase_order_id` → `purchase_orders.purchase_order_id`
- `goods_receipts.purchase_order_id` → `purchase_orders.purchase_order_id`
- `goods_receipt_lines.goods_receipt_id` → `goods_receipts.goods_receipt_id`
- `customer_orders.customer_id` → `customers.customer_id`
- `customer_order_lines.customer_order_id` → `customer_orders.customer_order_id`
- `stock_movements.product_id` → `products.product_id`
- `stock_movements.reference_id` and `financial_transactions.reference_id` are polymorphic
  references to business events (validated in application logic)
- `financial_transactions.reversal_of_transaction_id` → `financial_transactions.transaction_id` (optional)

---

## 4. Table Definitions (Logical)

The detailed column-level schema is maintained in:
- `docs/design/db/schema.md`

The ERD is maintained in:
- `docs/design/db/erd.puml`

---

## 5. Indexing Strategy (Minimal but useful)

Recommended indexes:
- `products.sku` (unique)
- `purchase_orders.supplier_id`
- `purchase_order_lines.product_id`
- `customer_order_lines.product_id`
- `stock_movements.product_id, occurred_at`
- `financial_transactions.occurred_at`

---

## 6. Notes on Calculated Stock

Stock availability can be:
- **Derived** by summing `stock_movements` for a product, or
- **Cached** as a `products.quantity_on_hand` field and kept consistent via application logic.

For this project, the system will store `products.quantity_on_hand` to keep the UI/API simple,
while still recording `stock_movements` as an audit log.

---

## 7. Export Records

The system records report exports in `report_exports` including:
- report type
- format (TXT/JSON)
- generated timestamp
- output file path
- optional date range

This provides traceability for UC11 (Export Report).
