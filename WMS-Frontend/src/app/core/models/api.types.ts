export interface ApiErrorResponse {
  readonly traceId?: string;
  readonly code?: string;
  readonly message?: string;
  readonly errors?: Record<string, readonly string[]>;
}

export interface ApiErrorState {
  readonly status?: number;
  readonly code?: string;
  readonly message: string;
  readonly traceId?: string;
  readonly errors: Record<string, readonly string[]>;
}

export interface HealthResponse {
  readonly name: string;
  readonly status: string;
}

export interface MoneyDto {
  readonly amount: number;
  readonly currency: string;
}

export interface SupplierResponse {
  readonly supplierId: string;
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
  readonly address: string | null;
}

export interface CreateSupplierRequest {
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
  readonly address: string | null;
}

export type UpdateSupplierRequest = CreateSupplierRequest;

export interface ProductResponse {
  readonly productId: string;
  readonly sku: string;
  readonly name: string;
  readonly supplierId: string;
  readonly reorderThreshold: number;
  readonly quantityOnHand: number;
  readonly unitCost: MoneyDto;
}

export interface CreateProductRequest {
  readonly sku: string;
  readonly supplierId: string;
  readonly reorderThreshold: number;
  readonly name: string;
  readonly unitCost: MoneyDto;
}

export type UpdateProductRequest = CreateProductRequest;

export interface StockLevelResponse {
  readonly productId: string;
  readonly sku: string;
  readonly name: string;
  readonly quantityOnHand: number;
}

export interface AdjustStockRequest {
  readonly quantity: number;
  readonly reason: string;
}

export interface PurchaseOrderLineRequest {
  readonly productId: string;
  readonly quantity: number;
  readonly unitCost: MoneyDto;
}

export interface CreatePurchaseOrderRequest {
  readonly supplierId: string;
  readonly lines: readonly PurchaseOrderLineRequest[];
}

export interface PurchaseOrderLineResponse {
  readonly productId: string;
  readonly quantityOrdered: number;
  readonly unitCost: MoneyDto;
}

export interface PurchaseOrderResponse {
  readonly purchaseOrderId: string;
  readonly supplierId: string;
  readonly status: string;
  readonly createdAt: string;
  readonly lines: readonly PurchaseOrderLineResponse[];
}

export interface CancelPurchaseOrderRequest {
  readonly reason: string;
}

export interface GoodsReceiptLineRequest {
  readonly productId: string;
  readonly quantityReceived: number;
}

export interface ReceiveDeliveryRequest {
  readonly lines: readonly GoodsReceiptLineRequest[];
}

export interface GoodsReceiptLineResponse {
  readonly productId: string;
  readonly quantityReceived: number;
}

export interface GoodsReceiptResponse {
  readonly goodsReceiptId: string;
  readonly purchaseOrderId: string;
  readonly receivedAt: string;
  readonly lines: readonly GoodsReceiptLineResponse[];
}

export interface CustomerDto {
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
}

export interface CustomerOrderLineRequest {
  readonly productId: string;
  readonly quantity: number;
  readonly unitPrice: MoneyDto;
}

export interface CreateCustomerOrderRequest {
  readonly customer: CustomerDto;
  readonly lines: readonly CustomerOrderLineRequest[];
}

export interface CustomerOrderLineResponse {
  readonly productId: string;
  readonly quantity: number;
  readonly unitPrice: MoneyDto;
}

export interface CustomerOrderResponse {
  readonly customerOrderId: string;
  readonly status: string;
  readonly createdAt: string;
  readonly lines: readonly CustomerOrderLineResponse[];
  readonly totalAmount: MoneyDto;
}

export interface CancelCustomerOrderRequest {
  readonly reason: string;
}

export interface FinancialTransactionResponse {
  readonly transactionId: string;
  readonly type: string;
  readonly status: string;
  readonly amount: MoneyDto;
  readonly occurredAt: string;
  readonly referenceType: string;
  readonly referenceId: string;
  readonly reversalOfTransactionId: string | null;
}

export interface VoidOrReverseTransactionRequest {
  readonly action: 'Void' | 'Reverse';
  readonly reason: string | null;
}

export interface FinancialReportResponse {
  readonly from: string | null;
  readonly to: string | null;
  readonly totalSales: MoneyDto;
  readonly totalExpenses: MoneyDto;
}

export interface ExportFinancialReportRequest {
  readonly format: 'TXT' | 'JSON';
  readonly from: string | null;
  readonly to: string | null;
}

export interface ReportExportResponse {
  readonly exportId: string;
  readonly reportType: string;
  readonly format: string;
  readonly generatedAt: string;
  readonly filePath: string;
}

export const PURCHASE_ORDER_STATUSES = [
  'Pending',
  'PartiallyReceived',
  'Completed',
  'Cancelled',
] as const;

export const CUSTOMER_ORDER_STATUSES = ['Draft', 'Confirmed', 'Cancelled'] as const;

export const TRANSACTION_TYPES = ['Sale', 'PurchaseExpense', 'StockAdjustment'] as const;

export const TRANSACTION_STATUSES = ['Pending', 'Posted', 'Voided', 'Reversed'] as const;

export const REPORT_TYPES = ['SalesSummary', 'ExpenseSummary', 'FinancialSummary'] as const;

export const REPORT_FORMATS = ['TXT', 'JSON'] as const;

export const VOID_OR_REVERSE_ACTIONS = ['Void', 'Reverse'] as const;
