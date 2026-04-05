import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../../../core/http/api-helpers';
import {
  CancelCustomerOrderRequest,
  CreateCustomerOrderRequest,
  CustomerOrderResponse,
} from '../../../core/models/api.types';

interface CustomerOrderQuery {
  readonly customerId?: string;
  readonly status?: string;
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
export class CustomerOrdersApiService {
  private readonly http = inject(HttpClient);

  getCustomerOrders(query: CustomerOrderQuery = {}): Observable<readonly CustomerOrderResponse[]> {
    return this.http.get<readonly CustomerOrderResponse[]>('/api/customer-orders', {
      params: buildHttpParams(query),
    });
  }

  createCustomerOrder(request: CreateCustomerOrderRequest): Observable<CustomerOrderResponse> {
    return this.http.post<CustomerOrderResponse>('/api/customer-orders', request);
  }

  cancelCustomerOrder(
    customerOrderId: string,
    request: CancelCustomerOrderRequest,
  ): Observable<CustomerOrderResponse> {
    return this.http.post<CustomerOrderResponse>(
      `/api/customer-orders/${customerOrderId}/cancel`,
      request,
    );
  }
}
