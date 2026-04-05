# Frontend API Connection Notes

Notes for wiring the current Angular starter UI to the backend API and tightening the forms so they behave like a real app.

## 1. Global Things That Need Wiring First

### 1.1 API plumbing

- The role selector needs to drive the `X-Wms-Role` header on every protected request.
- Add a frontend API base URL config so the app is not hardcoded to one environment.
- Add feature API services for:
  - suppliers
  - inventory
  - purchase orders
  - customer orders
  - finance / reporting
- Add a shared error mapper for the backend `ErrorResponse` shape:
  - `code`
  - `message`
  - `errors[field]`
- Add loading, success, and error handling at page level.

### 1.2 Shared UI / QoL pieces that are still missing

- `loading-state` component for tables and forms while requests are running.
- `error-banner` component for server and validation errors.
- `confirm-dialog` for delete / cancel / void / reverse actions.
- Shared pagination controls because list endpoints support `page`, `pageSize`, `sort`, and `order`.
- Shared search input for endpoints that support `q`.
- Shared empty-state for `no rows found` vs `not loaded yet`.
- Shared form error display under fields.
- Shared date-range validation helper:
  - show inline error when `from > to`
  - reject invalid dates
  - keep date format to `YYYY-MM-DD`

### 1.3 Form approach

- Use Angular reactive forms for every page with writes.
- Disable submit while invalid or saving.
- Mark fields touched on submit so validation appears immediately.
- Surface backend validation errors next to the matching control where possible.
- Keep all date fields as native `input type="date"` first for simplicity.
- Keep money fields as numeric inputs with GBP fixed in the UI, because the backend only accepts GBP.

## 2. Biggest Mismatches To Fix Before Wiring

- The selected UI role and the API role restrictions do not line up on every page.
- Some visible fields do not exist in the current backend contracts.
- Some tables show columns that are not returned by the backend.

### 2.1 Role mismatches

- `inventory` page mixes Manager actions and Warehouse Staff actions.
  - Product create / edit / delete is Manager.
  - Stock levels and stock adjustment are Warehouse Staff.
- `purchase-orders` page mixes Manager actions and Warehouse Staff actions.
  - Create / list / cancel is Manager.
  - Receive delivery is Warehouse Staff.
- `customer-orders` page mixes Warehouse Staff actions and Manager reads.
  - Create / cancel is Warehouse Staff.
  - List / get order is Manager.

### 2.2 Contract mismatches

- Purchase order form has `Expected delivery`, but the API create request does not have that field.
- Receive delivery form has `Notes`, but the API receipt request does not have that field.
- Customer order list shows `Customer`, but `CustomerOrderResponse` does not return customer details.
- Purchase order list shows `Supplier` name and `ETA`, but `PurchaseOrderResponse` only returns `supplierId`, `status`, `createdAt`, and `lines`.
- Supplier list shows `Status`, but `SupplierResponse` does not return a status field.
- Finance page has `Report type = Transaction history`, but that is not the same as the `/api/reports/financial` endpoint.
- Export history table shows `Name`, but `ReportExportResponse` does not return a display name.

## 3. Route-By-Route Notes

## 3.1 Dashboard

Current purpose:
- Landing page with quick links only.

API connections needed:
- `GET /api/health`
- Optional role-aware summary calls:
  - `GET /api/products/low-stock`
  - `GET /api/products/stock`
  - `GET /api/purchase-orders`
  - `GET /api/customer-orders`
  - `GET /api/transactions`

Important note:
- The dashboard should be role-aware.
- Do not call endpoints the selected role is not allowed to access, or the page will fill with `403` responses.

QoL:
- Add a small `last updated` timestamp.
- Add a manual refresh button.
- Show cards that change based on selected role.

## 3.2 Suppliers Page

Current page:
- Supplier form
- Supplier table

API connections needed:
- `GET /api/suppliers`
- `POST /api/suppliers`
- `GET /api/suppliers/{supplierId}` for edit mode
- `PUT /api/suppliers/{supplierId}`
- `DELETE /api/suppliers/{supplierId}`
- Optional drilldown:
  - `GET /api/suppliers/{supplierId}/purchase-orders`

Form controls to add:
- `name`: text, required
- `email`: `type="email"`
- `phone`: `type="tel"`
- `address`: `textarea`
- hidden / internal `supplierId` for edit mode

List controls to add:
- search input for `q`
- sort control, default `name`
- page size selector
- pagination controls
- row actions:
  - edit
  - delete
  - view purchase orders

Validation and QoL:
- Show inline error when `name` is empty.
- Show inline error when email format is invalid.
- Show inline error when phone format is invalid.
- Show form-level error when all of `email`, `phone`, and `address` are empty.
- Confirm before delete.
- Show conflict errors cleanly when duplicate supplier creation fails.

## 3.3 Inventory Page

Current page:
- Product create/edit form
- Stock table
- Manual stock adjustment panel

API connections needed:
- `GET /api/suppliers` for supplier dropdown
- `GET /api/products`
- `POST /api/products`
- `GET /api/products/{productId}` for edit mode
- `PUT /api/products/{productId}`
- `DELETE /api/products/{productId}`
- `GET /api/products/stock`
- `GET /api/products/low-stock`
- `POST /api/products/{productId}/adjust-stock`

Important role note:
- Split the page into clearly labeled sections or tabs:
  - Manager: product CRUD, low stock
  - Warehouse Staff: stock levels, stock adjustment

Product form controls to add:
- `name`: text, required
- `sku`: text, required
- `supplierId`: select, required
- `reorderThreshold`: `type="number"`, `min="0"`, `step="1"`
- `unitCost.amount`: `type="number"`, `min="0.01"`, `step="0.01"`
- currency display locked to `GBP`

Current product form gap:
- It is missing the supplier select, but the API requires `supplierId`.

Stock / list controls to add:
- search input for `q`
- sort control
- pagination
- optional low-stock toggle
- delete and edit row actions for Manager

Adjustment form controls to add:
- `productId`: select or autocomplete, not free-text SKU only
- `quantity`: `type="number"`, `step="1"`, must not be `0`
- `reason`: text or textarea, required

Validation and QoL:
- Show a clear error if adjustment quantity is `0`.
- Allow negative adjustments but explain them.
- Show current stock before adjustment.
- Refresh stock table after save / adjust.
- Show backend conflict errors if an adjustment would violate business rules.

## 3.4 Purchase Orders Page

Current page:
- Create purchase order form
- Purchase order list
- Receive delivery panel

API connections needed:
- `GET /api/suppliers`
- `GET /api/products`
- `POST /api/purchase-orders`
- `GET /api/purchase-orders`
- `GET /api/purchase-orders/{purchaseOrderId}`
- `POST /api/purchase-orders/{purchaseOrderId}/cancel`
- `POST /api/purchase-orders/{purchaseOrderId}/receipts`
- `GET /api/purchase-orders/{purchaseOrderId}/receipts`

Important role note:
- Keep create / list / cancel as Manager.
- Keep receive delivery as Warehouse Staff.
- Hide or disable the wrong section for the wrong role.

Major contract gaps:
- `Expected delivery` is not in the create purchase order request.
- `Notes` is not in the receive delivery request.
- The page currently assumes one product line, but the API requires a `lines[]` collection.

Create form controls to add:
- `supplierId`: select, required
- dynamic `lines[]` form array
  - `productId`: select, required
  - `quantity`: `type="number"`, `min="1"`, `step="1"`
  - `unitCost.amount`: `type="number"`, `min="0.01"`, `step="0.01"`
- add / remove line buttons

List controls to add:
- status filter
- date range filter
  - `from`: `type="date"`
  - `to`: `type="date"`
- sort control
- pagination
- row actions:
  - view details
  - cancel
  - view receipts

Receive delivery controls to add:
- choose an open purchase order from the table or a select
- dynamic receipt line array based on outstanding order lines
  - `productId`: locked / prefilled from PO line
  - `quantityReceived`: `type="number"`, `min="1"`, `max=outstanding`

Validation and QoL:
- At least one line is required on create and receive.
- Prevent duplicate product lines in the same order.
- Cancel action needs a required reason field because the API requires it.
- If date filters are kept, use native date inputs and show an inline error when `from > to`.
- If expected delivery is wanted, the backend contract needs extending first.

## 3.5 Customer Orders Page

Current page:
- Create customer order form
- Recent order list

API connections needed:
- `POST /api/customer-orders`
- `GET /api/customer-orders`
- `GET /api/customer-orders/{customerOrderId}`
- `POST /api/customer-orders/{customerOrderId}/cancel`
- likely `GET /api/products` for product lookup

Important role note:
- Create / cancel is Warehouse Staff.
- List / view is Manager.
- Either split the route by role or make the list conditional.

Major contract gap:
- The page only captures one SKU and quantity, but the API expects a `customer` object plus `lines[]`.
- The current table shows customer name, but `CustomerOrderResponse` does not return customer details.

Create form controls to add:
- customer group
  - `customer.name`: text, required
  - `customer.email`: `type="email"`
  - `customer.phone`: `type="tel"`
- dynamic `lines[]` form array
  - `productId`: select or autocomplete
  - `quantity`: `type="number"`, `min="1"`, `step="1"`
  - `unitPrice.amount`: `type="number"`, `min="0.01"`, `step="0.01"`

List controls to add:
- status filter
- date range filter
  - `from`: `type="date"`
  - `to`: `type="date"`
- pagination
- optional detail drawer for lines

Cancel controls to add:
- cancel action button on eligible orders
- required reason textarea because the API expects a reason

Validation and QoL:
- Validate stock before final submission where possible.
- At least one line is required.
- Show read-only order total before submit.
- Use inline date errors if the list gets date filters.
- Either remove the `Customer` column from the list or request a backend contract change.

## 3.6 Finance and Reports Page

Current page:
- report filter form
- transactions table
- export history table

API connections needed:
- `GET /api/transactions`
- `GET /api/transactions/{transactionId}`
- `POST /api/transactions/{transactionId}/void-or-reverse`
- `GET /api/reports/financial`
- `POST /api/reports/financial/export`
- `GET /api/reports/exports`

Major contract / UX notes:
- `Transaction history` should not be a value inside the financial report form.
- This page really needs two separate filter areas:
  - financial summary / export
  - transaction list

Controls to add for financial summary / export:
- `from`: `type="date"`
- `to`: `type="date"`
- `format`: select with `TXT` / `JSON`
- preview and export actions

Controls to add for transactions:
- `type`: select from transaction types
- `status`: select from transaction statuses
- `from`: `type="date"`
- `to`: `type="date"`
- sort control
- pagination
- row action for `void / reverse`

Controls to add for export history:
- `reportType`: select
- `format`: select
- `from`: `type="date"`
- `to`: `type="date"`
- sort control
- pagination

Void / reverse form controls to add:
- `action`: select with `Void` / `Reverse`
- `reason`: textarea

Validation and QoL:
- Invalid dates should show inline errors.
- `from > to` should block submit and show a clear error.
- `reason` is required when `action = Void`.
- Replace export history `Name` with either:
  - `reportType`
  - `filePath`
  - or a client-generated label
- Add `Occurred At` and `Reference Type` columns to the transactions table because the API returns them.

## 4. Components That Need Direct API Awareness

- `role-selector`
  - must update request header state
- `dashboard.page`
  - health and summary reads
- `suppliers.page`
  - supplier CRUD and history
- `inventory.page`
  - product CRUD, stock reads, low stock, stock adjustment
- `purchase-orders.page`
  - purchase order CRUD-like actions and receipts
- `customer-orders.page`
  - create / list / cancel customer orders
- `finance-reports.page`
  - transactions, reports, export history, void / reverse

These shared components do not need to call the API themselves, but they do need API state passed into them:

- `status-badge`
  - map backend status strings to badge tone
- `page-header`
  - action buttons should support loading / disabled states
- `empty-state`
  - should show `no data`, `no permission`, or `no results` messages depending on API result

## 5. Recommended First Wiring Order

1. Global API setup:
   - base URL
   - role header interceptor
   - shared error handling
2. Suppliers:
   - simplest CRUD surface
3. Inventory:
   - gives supplier/product linkage and stock visibility
4. Purchase orders:
   - depends on suppliers and products
5. Customer orders:
   - depends on products
6. Finance / reports:
   - easiest once the rest of the data flows are stable

## 6. Simple Rule For Dates

For every date field in the frontend:

- use `input type="date"` first
- keep the value in `YYYY-MM-DD`
- reject invalid dates immediately
- show an inline error under the field
- if both `from` and `to` are present, show an error when `from > to`
- do not send invalid or partial dates to the API
