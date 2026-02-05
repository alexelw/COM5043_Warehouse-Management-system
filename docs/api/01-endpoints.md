# API Endpoints

This document defines the HTTP API surface for the Warehouse Management System (WMS).
The API acts as a transport layer only; all business rules are implemented in the
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
- `409 Conflict`

---

### Get Suppliers  
**Method:** `GET`  
**Route:** `/api/suppliers`  
**Role:** Manager  

Returns all suppliers.

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
- `409 Conflict`

---

### Get Products  
**Method:** `GET`  
**Route:** `/api/products`  
**Role:** Manager  

Returns product catalogue.

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

**Responses**
- `200 OK`

---

### Get Low Stock Items  
**Use Case:** UC07  
**Method:** `GET`  
**Route:** `/api/products/low-stock`  
**Role:** Manager  

Returns products at or below reorder threshold.

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

---

### Get Purchase Orders  
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/purchase-orders`  
**Role:** Manager  

Returns all purchase orders.

**Responses**
- `200 OK`

---

### Get Purchase Order  
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/purchase-orders/{purchaseOrderId}`  
**Role:** Manager  

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
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/purchase-orders/{purchaseOrderId}/receipts`  
**Role:** Manager  

Returns delivery history for a purchase order.

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
- `409 Conflict`

---

### Get Customer Order  
**Use Case:** UC12  
**Method:** `GET`  
**Route:** `/api/customer-orders/{customerOrderId}`  
**Role:** Manager  

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
| GET | /api/purchase-orders/{purchaseOrderId} | UC12 | Manager |
| POST | /api/purchase-orders/{purchaseOrderId}/cancel | UC14 | Manager |
| POST | /api/purchase-orders/{purchaseOrderId}/receipts | UC05, UC15 | Warehouse Staff |
| GET | /api/purchase-orders/{purchaseOrderId}/receipts | UC12 | Manager |
| POST | /api/customer-orders | UC08 | Warehouse Staff |
| GET | /api/customer-orders/{customerOrderId} | UC12 | Manager |
| GET | /api/customer-orders | UC12 | Manager |
| POST | /api/customer-orders/{customerOrderId}/cancel | UC13 | Warehouse Staff |
| GET | /api/transactions | — | Administrator |
| GET | /api/transactions/{transactionId} | — | Administrator |
| POST | /api/transactions/{transactionId}/void-or-reverse | UC16 | Administrator |
| GET | /api/reports/financial | UC10 | Administrator |
| POST | /api/reports/financial/export | UC11 | Administrator |
| GET | /api/reports/exports | — | Administrator |
| GET | /api/health | — | Any |
