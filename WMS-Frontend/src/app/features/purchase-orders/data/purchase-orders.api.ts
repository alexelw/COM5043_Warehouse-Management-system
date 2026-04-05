import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../../../core/http/api-helpers';
import {
  CancelPurchaseOrderRequest,
  CreatePurchaseOrderRequest,
  GoodsReceiptResponse,
  PurchaseOrderResponse,
  ReceiveDeliveryRequest,
} from '../../../core/models/api.types';

interface PurchaseOrderQuery {
  readonly supplierId?: string;
  readonly status?: string;
  readonly from?: string;
  readonly to?: string;
  readonly sort?: string;
  readonly order?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

interface ReceiptQuery {
  readonly from?: string;
  readonly to?: string;
  readonly sort?: string;
  readonly order?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

@Injectable({
  providedIn: 'root',
})
export class PurchaseOrdersApiService {
  private readonly http = inject(HttpClient);

  getPurchaseOrders(query: PurchaseOrderQuery = {}): Observable<readonly PurchaseOrderResponse[]> {
    return this.http.get<readonly PurchaseOrderResponse[]>('/api/purchase-orders', {
      params: buildHttpParams(query),
    });
  }

  createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<PurchaseOrderResponse> {
    return this.http.post<PurchaseOrderResponse>('/api/purchase-orders', request);
  }

  cancelPurchaseOrder(
    purchaseOrderId: string,
    request: CancelPurchaseOrderRequest,
  ): Observable<PurchaseOrderResponse> {
    return this.http.post<PurchaseOrderResponse>(
      `/api/purchase-orders/${purchaseOrderId}/cancel`,
      request,
    );
  }

  receiveDelivery(
    purchaseOrderId: string,
    request: ReceiveDeliveryRequest,
  ): Observable<GoodsReceiptResponse> {
    return this.http.post<GoodsReceiptResponse>(
      `/api/purchase-orders/${purchaseOrderId}/receipts`,
      request,
    );
  }

  getReceipts(
    purchaseOrderId: string,
    query: ReceiptQuery = {},
  ): Observable<readonly GoodsReceiptResponse[]> {
    return this.http.get<readonly GoodsReceiptResponse[]>(
      `/api/purchase-orders/${purchaseOrderId}/receipts`,
      {
        params: buildHttpParams(query),
      },
    );
  }
}
