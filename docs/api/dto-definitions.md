# API DTO Definitions

Defines the Data Transfer Objects (DTOs) used at the API boundary.
DTOs decouple transport from the domain model and provide explicit validation
contracts between frontend and backend.

Domain entities are never exposed directly.

---

## 1. General DTO Conventions

- DTOs live in `Wms.Contracts`
- DTOs are immutable where practical
- Validation attributes are applied at the API boundary
- Monetary values use explicit amount + currency fields
- IDs are represented as GUIDs
- Currency is fixed to `GBP`

---

## 2. Supplier DTOs

### CreateSupplierRequest
```json
{
  "name": "string",
  "email": "string?",
  "phone": "string?",
  "address": "string?"
}
```

Rules
- `name` is required
- At least one contact field must be provided

### UpdateSupplierRequest
```json
{
  "name": "string",
  "email": "string?",
  "phone": "string?",
  "address": "string?"
}
```

Rules
- `name` is required
- At least one contact field must be provided

### SupplierResponse
```json
{
  "supplierId": "guid",
  "name": "string",
  "email": "string?",
  "phone": "string?",
  "address": "string?"
}
```

---

## 3. Product & Inventory DTOs

### CreateProductRequest
```json
{
  "sku": "string",
  "name": "string",
  "supplierId": "guid",
  "reorderThreshold": 10,
  "unitCost": {
    "amount": 12.50,
    "currency": "GBP"
  }
}
```

Rules
- `supplierId` is required
- `sku` must be unique
- `reorderThreshold` >= 0
- `unitCost.amount` > 0
- `unitCost.currency` must be `GBP`

### UpdateProductRequest
```json
{
  "sku": "string",
  "name": "string",
  "supplierId": "guid",
  "reorderThreshold": 10,
  "unitCost": {
    "amount": 12.50,
    "currency": "GBP"
  }
}
```

Rules
- `supplierId` is required
- `reorderThreshold` >= 0
- `unitCost.amount` > 0
- `unitCost.currency` must be `GBP`

### ProductResponse
```json
{
  "productId": "guid",
  "sku": "string",
  "name": "string",
  "supplierId": "guid",
  "reorderThreshold": 10,
  "quantityOnHand": 100,
  "unitCost": {
    "amount": 12.50,
    "currency": "GBP"
  }
}
```

### StockLevelResponse
```json
{
  "productId": "guid",
  "sku": "string",
  "name": "string",
  "quantityOnHand": 100
}
```

### AdjustStockRequest
```json
{
  "quantity": -5,
  "reason": "Stock count correction"
}
```

Rules
- `quantity` must be a non-zero integer (negative reduces stock)
- `reason` is required

---

## 4. Purchase Order DTOs

### CreatePurchaseOrderRequest
```json
{
  "supplierId": "guid",
  "lines": [
    {
      "productId": "guid",
      "quantity": 20,
      "unitCost": {
        "amount": 8.75,
        "currency": "GBP"
      }
    }
  ]
}
```

Rules
- `supplierId` is required
- At least one line required
- each line `productId` is required
- Line quantity > 0
- `unitCost.currency` must be `GBP`

### PurchaseOrderResponse
```json
{
  "purchaseOrderId": "guid",
  "supplierId": "guid",
  "status": "Pending",
  "createdAt": "2026-02-01T10:15:00Z",
  "lines": [
    {
      "productId": "guid",
      "quantityOrdered": 20,
      "unitCost": {
        "amount": 8.75,
        "currency": "GBP"
      }
    }
  ]
}
```

Status values
- `Pending`
- `PartiallyReceived`
- `Completed`
- `Cancelled`

### ReceiveDeliveryRequest
```json
{
  "lines": [
    {
      "productId": "guid",
      "quantityReceived": 20
    }
  ]
}
```

Rules
- Quantities must not exceed outstanding PO quantities
- Quantities must be > 0

### CancelPurchaseOrderRequest
```json
{
  "reason": "Supplier cancelled order"
}
```

Rules
- Allowed only for `Pending` or `PartiallyReceived` orders

---

## 5. Customer Order DTOs

### CreateCustomerOrderRequest
```json
{
  "customer": {
    "name": "string",
    "email": "string?",
    "phone": "string?"
  },
  "lines": [
    {
      "productId": "guid",
      "quantity": 5,
      "unitPrice": {
        "amount": 15.00,
        "currency": "GBP"
      }
    }
  ]
}
```

Rules
- Stock must be available for all lines
- Quantity > 0
- `unitPrice.currency` must be `GBP`

### CustomerOrderResponse
```json
{
  "customerOrderId": "guid",
  "status": "Confirmed",
  "createdAt": "2026-02-01T11:30:00Z",
  "lines": [
    {
      "productId": "guid",
      "quantity": 5,
      "unitPrice": {
        "amount": 15.00,
        "currency": "GBP"
      }
    }
  ],
  "totalAmount": {
    "amount": 75.00,
    "currency": "GBP"
  }
}
```

Status values
- `Draft`
- `Confirmed`
- `Cancelled`

### CancelCustomerOrderRequest
```json
{
  "reason": "Customer cancelled"
}
```

Rules
- Allowed only for `Draft` or `Confirmed` orders

---

## 6. Financial DTOs

### FinancialTransactionResponse
```json
{
  "transactionId": "guid",
  "type": "Sale",
  "status": "Posted",
  "amount": {
    "amount": 75.00,
    "currency": "GBP"
  },
  "occurredAt": "2026-02-01T11:30:00Z",
  "referenceType": "CustomerOrder",
  "referenceId": "guid",
  "reversalOfTransactionId": "guid?"
}
```

Status values
- `Pending`
- `Posted`
- `Voided`
- `Reversed`

### VoidOrReverseTransactionRequest
```json
{
  "action": "Void",
  "reason": "Duplicate entry"
}
```

Rules
- `action` must be `Void` or `Reverse`
- `reason` required for `Void`

### FinancialReportResponse
```json
{
  "from": "2026-01-01",
  "to": "2026-01-31",
  "totalSales": {
    "amount": 1200.00,
    "currency": "GBP"
  },
  "totalExpenses": {
    "amount": 800.00,
    "currency": "GBP"
  }
}
```

### ExportFinancialReportRequest
```json
{
  "format": "TXT",
  "from": "2026-01-01",
  "to": "2026-01-31"
}
```

Rules
- `format` must be `TXT` or `JSON`
- `from` must be on or before `to` when both are supplied

### ReportExportResponse
```json
{
  "exportId": "guid",
  "reportType": "FinancialSummary",
  "format": "TXT",
  "generatedAt": "2026-02-01T12:00:00Z",
  "filePath": "exports/report-2026-02-01.txt"
}
```

---

## 7. Shared DTOs

### MoneyDto
```json
{
  "amount": 12.50,
  "currency": "GBP"
}
```

---

## 8. Notes on Validation

- Structural validation occurs at the API boundary
- Business rule validation occurs in Application/Domain layers
- Validation errors return `400 Bad Request`
- Business conflicts return `409 Conflict`
