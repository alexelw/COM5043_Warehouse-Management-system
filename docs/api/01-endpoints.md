# API Endpoints

Defines the HTTP API surface for the Warehouse Management System (WMS).
The API acts as a transport layer; business rules are implemented in the
Application and Domain layers.

Endpoints are grouped by resource and mapped directly to the defined use-cases
(UC01–UC17). Supporting CRUD endpoints are included even when no explicit use-case
is listed.

---

## 1. API Conventions

- Base path: `/api`
- Routes use lowercase and hyphen-separated words
- JSON request/response bodies
- DTOs only (no domain entities returned)
- Controllers remain thin and delegate to Application services
- Errors follow a standard error response model

Unless stated otherwise, all non-2xx responses return the standard error response
defined below.

### 1.1 Standard Error Responses

All error responses return a consistent JSON body.

```json
{
  "traceId": "string",
  "code": "string",
  "message": "string",
  "errors": {
    "field": [
      "message"
    ]
  }
}
```

- `traceId` is a request correlation identifier.
- `code` is a machine-readable error code.
- `message` is a user-facing summary.
- `errors` is a field-to-errors map used for validation failures.

| Status | code | When |
|---|---|---|
| 400 | `validation_failed` | Request shape or validation rules fail |
| 403 | `forbidden` | Selected role is valid but not allowed for the endpoint |
| 404 | `not_found` | Resource does not exist |
| 409 | `conflict` | Business rule conflict |
| 500 | `server_error` | Unexpected server error |

**Example: 400 Validation Error**
```json
{
  "traceId": "00-abc123",
  "code": "validation_failed",
  "message": "One or more validation errors occurred.",
  "errors": {
    "name": [
      "Name is required."
    ]
  }
}
```

**Example: 404 Not Found**
```json
{
  "traceId": "00-def456",
  "code": "not_found",
  "message": "Supplier not found.",
  "errors": {}
}
```

**Example: 409 Conflict**
```json
{
  "traceId": "00-ghi789",
  "code": "conflict",
  "message": "Cannot delete supplier with open purchase orders.",
  "errors": {}
}
```

### 1.2 Role Selection

All role-protected endpoints require an `X-Wms-Role` request header.

- Allowed values: `WarehouseStaff`, `Manager`, `Administrator`
- Missing or invalid values return `400 Bad Request`
- Valid but disallowed values return `403 Forbidden`
- Health endpoints remain public

### 1.3 Query Parameters (List Endpoints)

List endpoints support optional pagination and sorting.

- `page` (optional, default `1`) - 1-based page index.
- `pageSize` (optional, default `50`, max `200`) - number of items per page.
- `sort` (optional) - field name to sort by.
- `order` (optional, `asc|desc`) - sort direction.
- `q` (optional) - free-text search (where applicable).

---

## 2. Supplier Management

### Create Supplier  
**Use Case:** UC01  
**Method:** `POST`  
**Route:** `/api/suppliers`  
**Role:** Manager  

Creates a new supplier record.

**Responses**
- `201 Created`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### Get Suppliers  
**Method:** `GET`  
**Route:** `/api/suppliers`  
**Role:** Manager  

Returns all suppliers.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `name`, default `name`)
- `order` (optional, `asc|desc`, default `asc`)
- `q` (optional, search by name, email, phone)

**Responses**
- `200 OK`

---

### Get Supplier  
**Method:** `GET`  
**Route:** `/api/suppliers/{supplierId}`  
**Role:** Manager  

Returns a specific supplier.

**Responses**
- `200 OK`
- `404 Not Found`

---

### Update Supplier  
**Use Case:** UC02  
**Method:** `PUT`  
**Route:** `/api/suppliers/{supplierId}`  
**Role:** Manager  

Updates supplier details.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `409 Conflict`

---

### Delete Supplier  
**Use Case:** UC03  
**Method:** `DELETE`  
**Route:** `/api/suppliers/{supplierId}`  
**Role:** Manager  

Deletes (or deactivates) a supplier.

**Responses**
- `204 No Content`
- `404 Not Found`
- `409 Conflict` (supplier in use)

---

### Get Supplier Purchase Orders  
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/suppliers/{supplierId}/purchase-orders`  
**Role:** Manager  

Returns purchase order history for a supplier.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `createdAt|status`, default `createdAt`)
- `order` (optional, `asc|desc`, default `desc`)
- `status` (optional, `Pending|PartiallyReceived|Completed|Cancelled`)
- `from` (optional, `YYYY-MM-DD`)
- `to` (optional, `YYYY-MM-DD`)

**Responses**
- `200 OK`
- `404 Not Found`

---

## 3. Products & Inventory

### Create Product  
**Method:** `POST`  
**Route:** `/api/products`  
**Role:** Manager  

Creates a new product.

**Responses**
- `201 Created`
- `400 Bad Request`
- `404 Not Found`
- `404 Not Found`
- `409 Conflict`

---

### Get Products  
**Method:** `GET`  
**Route:** `/api/products`  
**Role:** Manager  

Returns product catalogue.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `name|sku|quantityOnHand|reorderThreshold`, default `name`)
- `order` (optional, `asc|desc`, default `asc`)
- `supplierId` (optional, filter by supplier)
- `q` (optional, search by name or SKU)

**Responses**
- `200 OK`

---

### Get Product  
**Method:** `GET`  
**Route:** `/api/products/{productId}`  
**Role:** Manager  

Returns product details.

**Responses**
- `200 OK`
- `404 Not Found`

---

### Update Product  
**Method:** `PUT`  
**Route:** `/api/products/{productId}`  
**Role:** Manager  

Updates product details.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### Delete Product  
**Method:** `DELETE`  
**Route:** `/api/products/{productId}`  
**Role:** Manager  

Deletes or deactivates a product.

**Responses**
- `204 No Content`
- `404 Not Found`
- `409 Conflict`

---

### Get Stock Levels  
**Use Case:** UC06  
**Method:** `GET`  
**Route:** `/api/products/stock`  
**Role:** Warehouse Staff  

Returns current stock levels.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `quantityOnHand|sku|name`, default `quantityOnHand`)
- `order` (optional, `asc|desc`, default `desc`)
- `q` (optional, search by name or SKU)

**Responses**
- `200 OK`

---

### Get Low Stock Items  
**Use Case:** UC07  
**Method:** `GET`  
**Route:** `/api/products/low-stock`  
**Role:** Manager  

Returns products at or below reorder threshold.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `quantityOnHand|reorderThreshold|name`, default `quantityOnHand`)
- `order` (optional, `asc|desc`, default `asc`)
- `q` (optional, search by name or SKU)

**Responses**
- `200 OK`

---

### Adjust Stock  
**Use Case:** UC17  
**Method:** `POST`  
**Route:** `/api/products/{productId}/adjust-stock`  
**Role:** Warehouse Staff  

Adjusts stock by a positive or negative quantity with a required reason.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

## 4. Purchase Orders

### Create Purchase Order  
**Use Case:** UC04  
**Method:** `POST`  
**Route:** `/api/purchase-orders`  
**Role:** Manager  

Creates a new purchase order.

**Responses**
- `201 Created`
- `400 Bad Request`
- `404 Not Found`

---

### Get Purchase Orders  
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/purchase-orders`  
**Role:** Manager  

Returns all purchase orders.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `createdAt|status`, default `createdAt`)
- `order` (optional, `asc|desc`, default `desc`)
- `supplierId` (optional, filter by supplier)
- `status` (optional, `Pending|PartiallyReceived|Completed|Cancelled`)
- `from` (optional, `YYYY-MM-DD`)
- `to` (optional, `YYYY-MM-DD`)

**Responses**
- `200 OK`

---

### Get Open Purchase Orders  
**Use Case:** UC05, UC15  
**Method:** `GET`  
**Route:** `/api/purchase-orders/open`  
**Role:** Warehouse Staff  

Returns purchase orders that still have quantities outstanding for receiving.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `createdAt|status`, default `createdAt`)
- `order` (optional, `asc|desc`, default `desc`)

**Responses**
- `200 OK`

---

### Get Purchase Order  
**Use Case:** UC12, UC05, UC15  
**Method:** `GET`  
**Route:** `/api/purchase-orders/{purchaseOrderId}`  
**Role:** Manager, Warehouse Staff  

Returns purchase order details.

**Responses**
- `200 OK`
- `404 Not Found`

---

### Cancel Purchase Order  
**Use Case:** UC14  
**Method:** `POST`  
**Route:** `/api/purchase-orders/{purchaseOrderId}/cancel`  
**Role:** Manager  

Cancels a pending or partially received purchase order.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### Receive Delivery (Full or Partial)  
**Use Case:** UC05, UC15  
**Method:** `POST`  
**Route:** `/api/purchase-orders/{purchaseOrderId}/receipts`  
**Role:** Warehouse Staff  

Records goods receipt and updates inventory. Partial deliveries set status to
Partially Received until remaining quantities are received or cancelled.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### Get Purchase Order Receipts  
**Use Case:** UC12, UC05, UC15  
**Method:** `GET`  
**Route:** `/api/purchase-orders/{purchaseOrderId}/receipts`  
**Role:** Manager, Warehouse Staff  

Returns delivery history for a purchase order.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `receivedAt`, default `receivedAt`)
- `order` (optional, `asc|desc`, default `desc`)
- `from` (optional, `YYYY-MM-DD`)
- `to` (optional, `YYYY-MM-DD`)

**Responses**
- `200 OK`
- `404 Not Found`

---

## 5. Customer Orders

### Create Customer Order  
**Use Case:** UC08  
**Method:** `POST`  
**Route:** `/api/customer-orders`  
**Role:** Warehouse Staff  

Creates and confirms a customer order.

**Responses**
- `201 Created`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### Get Customer Order  
**Use Case:** UC12, UC13  
**Method:** `GET`  
**Route:** `/api/customer-orders/{customerOrderId}`  
**Role:** Manager, Warehouse Staff  

Returns customer order details.

**Responses**
- `200 OK`
- `404 Not Found`

---

### Get Customer Orders  
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/customer-orders`  
**Role:** Manager  

Returns customer order history.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `createdAt|status`, default `createdAt`)
- `order` (optional, `asc|desc`, default `desc`)
- `customerId` (optional, filter by customer)
- `status` (optional, `Draft|Confirmed|Cancelled`)
- `from` (optional, `YYYY-MM-DD`)
- `to` (optional, `YYYY-MM-DD`)

**Responses**
- `200 OK`

---

### Get Open Customer Orders  
**Use Case:** UC13  
**Method:** `GET`  
**Route:** `/api/customer-orders/open`  
**Role:** Warehouse Staff  

Returns customer orders that are still eligible for cancellation.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `createdAt|status`, default `createdAt`)
- `order` (optional, `asc|desc`, default `desc`)

**Responses**
- `200 OK`

---

### Cancel Customer Order  
**Use Case:** UC13  
**Method:** `POST`  
**Route:** `/api/customer-orders/{customerOrderId}/cancel`  
**Role:** Warehouse Staff  

Cancels a draft or confirmed customer order and restores inventory.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

## 6. Financial Management

### Get Financial Transactions  
**Method:** `GET`  
**Route:** `/api/transactions`  
**Role:** Administrator  

Returns recorded financial transactions.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `occurredAt|amount`, default `occurredAt`)
- `order` (optional, `asc|desc`, default `desc`)
- `type` (optional, filter by transaction type)
- `status` (optional, `Pending|Posted|Voided|Reversed`)
- `from` (optional, `YYYY-MM-DD`)
- `to` (optional, `YYYY-MM-DD`)

**Responses**
- `200 OK`

---

### Get Financial Transaction  
**Method:** `GET`  
**Route:** `/api/transactions/{transactionId}`  
**Role:** Administrator  

Returns a specific transaction.

**Responses**
- `200 OK`
- `404 Not Found`

---

### Void / Reverse Financial Transaction  
**Use Case:** UC16  
**Method:** `POST`  
**Route:** `/api/transactions/{transactionId}/void-or-reverse`  
**Role:** Administrator  

Voids a transaction or creates a reversal linked to the original.

**Responses**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### Generate Financial Report  
**Use Case:** UC10  
**Method:** `GET`  
**Route:** `/api/reports/financial`  
**Role:** Administrator  

Generates a financial summary.

**Query Parameters**
- `from` (optional)
- `to` (optional)

**Responses**
- `200 OK`

---

### Export Financial Report  
**Use Case:** UC11  
**Method:** `POST`  
**Route:** `/api/reports/financial/export`  
**Role:** Administrator  

Exports a financial report to file.

**Responses**
- `200 OK`
- `400 Bad Request`

---

### Get Report Export History  
**Method:** `GET`  
**Route:** `/api/reports/exports`  
**Role:** Administrator  

Returns report export records.

**Query Parameters**
- `page` (optional, default `1`)
- `pageSize` (optional, default `50`, max `200`)
- `sort` (optional, `generatedAt`, default `generatedAt`)
- `order` (optional, `asc|desc`, default `desc`)
- `reportType` (optional, filter by report type)
- `format` (optional, `TXT|JSON`)
- `from` (optional, `YYYY-MM-DD`)
- `to` (optional, `YYYY-MM-DD`)

**Responses**
- `200 OK`

---

## 7. System Endpoints

### Health Check  
**Method:** `GET`  
**Route:** `/api/health`  
**Role:** Any  

Used for monitoring and CI validation.

**Responses**
- `200 OK`

---

## 8. Endpoint to Use-Case Traceability

| Use Case | Endpoint |
|--------|----------|
| UC01 | POST /api/suppliers |
| UC02 | PUT /api/suppliers/{supplierId} |
| UC03 | DELETE /api/suppliers/{supplierId} |
| UC04 | POST /api/purchase-orders |
| UC05 | POST /api/purchase-orders/{purchaseOrderId}/receipts |
| UC06 | GET /api/products/stock |
| UC07 | GET /api/products/low-stock |
| UC08 | POST /api/customer-orders |
| UC09 | Automatic (no direct endpoint) |
| UC10 | GET /api/reports/financial |
| UC11 | POST /api/reports/financial/export |
| UC12 | GET /api/purchase-orders; GET /api/customer-orders; GET /api/suppliers/{supplierId}/purchase-orders |
| UC13 | POST /api/customer-orders/{customerOrderId}/cancel |
| UC14 | POST /api/purchase-orders/{purchaseOrderId}/cancel |
| UC15 | POST /api/purchase-orders/{purchaseOrderId}/receipts |
| UC16 | POST /api/transactions/{transactionId}/void-or-reverse |
| UC17 | POST /api/products/{productId}/adjust-stock |

---

## 9. Endpoint Summary Table

| Method | Endpoint | Use Case | Role |
|---|---|---|---|
| POST | /api/suppliers | UC01 | Manager |
| GET | /api/suppliers | — | Manager |
| GET | /api/suppliers/{supplierId} | — | Manager |
| PUT | /api/suppliers/{supplierId} | UC02 | Manager |
| DELETE | /api/suppliers/{supplierId} | UC03 | Manager |
| GET | /api/suppliers/{supplierId}/purchase-orders | UC12 | Manager |
| POST | /api/products | — | Manager |
| GET | /api/products | — | Manager |
| GET | /api/products/{productId} | — | Manager |
| PUT | /api/products/{productId} | — | Manager |
| DELETE | /api/products/{productId} | — | Manager |
| GET | /api/products/stock | UC06 | Warehouse Staff |
| GET | /api/products/low-stock | UC07 | Manager |
| POST | /api/products/{productId}/adjust-stock | UC17 | Warehouse Staff |
| POST | /api/purchase-orders | UC04 | Manager |
| GET | /api/purchase-orders | UC12 | Manager |
| GET | /api/purchase-orders/open | UC05, UC15 | Warehouse Staff |
| GET | /api/purchase-orders/{purchaseOrderId} | UC12, UC05, UC15 | Manager, Warehouse Staff |
| POST | /api/purchase-orders/{purchaseOrderId}/cancel | UC14 | Manager |
| POST | /api/purchase-orders/{purchaseOrderId}/receipts | UC05, UC15 | Warehouse Staff |
| GET | /api/purchase-orders/{purchaseOrderId}/receipts | UC12, UC05, UC15 | Manager, Warehouse Staff |
| POST | /api/customer-orders | UC08 | Warehouse Staff |
| GET | /api/customer-orders/open | UC13 | Warehouse Staff |
| GET | /api/customer-orders/{customerOrderId} | UC12, UC13 | Manager, Warehouse Staff |
| GET | /api/customer-orders | UC12 | Manager |
| POST | /api/customer-orders/{customerOrderId}/cancel | UC13 | Warehouse Staff |
| GET | /api/transactions | — | Administrator |
| GET | /api/transactions/{transactionId} | — | Administrator |
| POST | /api/transactions/{transactionId}/void-or-reverse | UC16 | Administrator |
| GET | /api/reports/financial | UC10 | Administrator |
| POST | /api/reports/financial/export | UC11 | Administrator |
| GET | /api/reports/exports | — | Administrator |
| GET | /api/health | — | Any |
