import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../../../core/http/api-helpers';
import {
  CreateSupplierRequest,
  PurchaseOrderResponse,
  SupplierResponse,
  UpdateSupplierRequest,
} from '../../../core/models/api.types';

interface SupplierQuery {
  readonly q?: string;
  readonly sort?: string;
  readonly order?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

interface SupplierPurchaseOrderQuery extends SupplierQuery {
  readonly status?: string;
  readonly from?: string;
  readonly to?: string;
}

@Injectable({
  providedIn: 'root',
})
export class SuppliersApiService {
  private readonly http = inject(HttpClient);

  getSuppliers(query: SupplierQuery = {}): Observable<readonly SupplierResponse[]> {
    return this.http.get<readonly SupplierResponse[]>('/api/suppliers', {
      params: buildHttpParams(query),
    });
  }

  createSupplier(request: CreateSupplierRequest): Observable<SupplierResponse> {
    return this.http.post<SupplierResponse>('/api/suppliers', request);
  }

  getSupplier(supplierId: string): Observable<SupplierResponse> {
    return this.http.get<SupplierResponse>(`/api/suppliers/${supplierId}`);
  }

  updateSupplier(supplierId: string, request: UpdateSupplierRequest): Observable<SupplierResponse> {
    return this.http.put<SupplierResponse>(`/api/suppliers/${supplierId}`, request);
  }

  deleteSupplier(supplierId: string): Observable<void> {
    return this.http.delete<void>(`/api/suppliers/${supplierId}`);
  }

  getSupplierPurchaseOrders(
    supplierId: string,
    query: SupplierPurchaseOrderQuery = {},
  ): Observable<readonly PurchaseOrderResponse[]> {
    return this.http.get<readonly PurchaseOrderResponse[]>(
      `/api/suppliers/${supplierId}/purchase-orders`,
      {
        params: buildHttpParams(query),
      },
    );
  }
}
