# Database Schema (Logical)

## suppliers
- supplier_id (PK)
- name (required)
- email (nullable)
- phone (nullable)
- address (nullable)

## products
- product_id (PK)
- sku (unique, required)
- name (required)
- supplier_id (FK -> suppliers)
- reorder_threshold (required)
- unit_cost_amount (required)
- unit_cost_currency (required, default GBP, fixed to GBP)
- quantity_on_hand (required, >= 0)

## purchase_orders
- purchase_order_id (PK)
- supplier_id (FK -> suppliers)
- status (required)
- created_at (required)

## purchase_order_lines
- purchase_order_line_id (PK)
- purchase_order_id (FK -> purchase_orders, ON DELETE CASCADE)
- product_id (FK -> products)
- quantity_ordered (>0)
- unit_cost_amount
- unit_cost_currency (default GBP, fixed to GBP)

## goods_receipts
- goods_receipt_id (PK)
- purchase_order_id (FK -> purchase_orders)
- received_at (required)

## goods_receipt_lines
- goods_receipt_line_id (PK)
- goods_receipt_id (FK -> goods_receipts, ON DELETE CASCADE)
- product_id (FK -> products)
- quantity_received (>0)

## customers
- customer_id (PK)
- name (required)
- email (nullable)
- phone (nullable)
- address (nullable)

## customer_orders
- customer_order_id (PK)
- customer_id (FK -> customers)
- status (required)
- created_at (required)

## customer_order_lines
- customer_order_line_id (PK)
- customer_order_id (FK -> customer_orders, ON DELETE CASCADE)
- product_id (FK -> products)
- quantity (non-zero integer; Adjustment may be negative)
- unit_price_amount
- unit_price_currency (default GBP, fixed to GBP)

## stock_movements
- stock_movement_id (PK)
- product_id (FK -> products)
- type (required, Receipt|Issue|Adjustment)
- quantity (>0)
- occurred_at (required)
- reference_type (required, enum of business events)
- reference_id (required, polymorphic reference)
- reason (nullable, required for Adjustment)

## financial_transactions
- transaction_id (PK)
- type (required)
- amount (required)
- currency (required, default GBP, fixed to GBP)
- status (required, Pending|Posted|Voided|Reversed)
- occurred_at (required)
- reference_type (required, enum of business events)
- reference_id (required, polymorphic reference)
- reversal_of_transaction_id (nullable, FK -> financial_transactions)

## report_exports
- report_export_id (PK)
- report_type (required)
- format (required)
- generated_at (required)
- file_path (required)
- range_from (nullable)
- range_to (nullable)

## users
- user_id (PK)
- display_name (required)
- role (required)
