# 06 — Traceability Matrix

Maps system requirements (use-cases) to design artefacts,
implementation components, and testing activities.

Traceability evidence is provided for LO1.

---

## Use-Case Traceability

| Use Case | Description | Application Service | Domain Entities | API Endpoint | Tests |
|--------|-------------|---------------------|----------------|--------------|-------|
| UC01 | Create Supplier | SupplierService | Supplier | POST /api/suppliers | Unit + API |
| UC02 | Update Supplier | SupplierService | Supplier | PUT /api/suppliers/{supplierId} | Unit + API |
| UC03 | Delete Supplier | SupplierService | Supplier, PurchaseOrder | DELETE /api/suppliers/{supplierId} | Unit + API |
| UC04 | Create Purchase Order | PurchaseOrderService | PurchaseOrder, PurchaseOrderLine, Supplier, Product | POST /api/purchase-orders | Unit + API |
| UC05 | Receive Delivery | PurchaseOrderService | PurchaseOrder, GoodsReceipt, GoodsReceiptLine, StockMovement, Product | POST /api/purchase-orders/{purchaseOrderId}/receipts | Unit + Integration |
| UC06 | View Stock Levels | InventoryService | Product, StockMovement | GET /api/products/stock | Unit |
| UC07 | Detect Low Stock | InventoryService | Product | GET /api/products/low-stock | Unit |
| UC08 | Create Customer Order | OrderService | CustomerOrder, CustomerOrderLine, StockMovement, FinancialTransaction, Product, Customer | POST /api/customer-orders | Unit + API |
| UC09 | Record Financial Transaction | FinanceService | FinancialTransaction | Automatic (no direct endpoint) | Unit |
| UC10 | Generate Financial Report | ReportingService | FinancialTransaction | GET /api/reports/financial | Unit |
| UC11 | Export Report to File | ReportingService | ReportExport | POST /api/reports/financial/export | Unit + Integration |
| UC12 | View Order History | Query Services | PurchaseOrder, CustomerOrder | GET /api/purchase-orders; GET /api/customer-orders; GET /api/suppliers/{supplierId}/purchase-orders | Unit |
| UC13 | Cancel Customer Order | OrderService | CustomerOrder, StockMovement, FinancialTransaction | POST /api/customer-orders/{customerOrderId}/cancel | Unit + API |
| UC14 | Cancel Purchase Order | PurchaseOrderService | PurchaseOrder, FinancialTransaction | POST /api/purchase-orders/{purchaseOrderId}/cancel | Unit + API |
| UC15 | Partial Delivery / Backorder | PurchaseOrderService | PurchaseOrder, GoodsReceipt, GoodsReceiptLine, StockMovement, Product | POST /api/purchase-orders/{purchaseOrderId}/receipts | Unit + Integration |
| UC16 | Void / Reverse Financial Transaction | FinanceService | FinancialTransaction | POST /api/transactions/{transactionId}/void-or-reverse | Unit + API |
| UC17 | Adjust Stock | InventoryService | Product, StockMovement | POST /api/products/{productId}/adjust-stock | Unit + API |

---

## Design Artefact Coverage

| Artefact | Covers |
|--------|--------|
| Scope & Assumptions | System boundaries |
| Requirements | UC01–UC17 |
| Architecture | Layering, dependencies |
| Domain Model | Core business logic |
| Database Design | Persistence mapping |
| Validation Rules | Business invariants |
| Behaviour Diagrams | Workflow behaviour |
| API Endpoints | External contract |
| DTO Definitions | Data contracts |
| ADRs | Design rationale |

---

## Testing Alignment

- Domain rules tested via unit tests
- Use-case workflows tested via application service tests
- Persistence tested via integration tests
- API behaviour tested via controller tests
