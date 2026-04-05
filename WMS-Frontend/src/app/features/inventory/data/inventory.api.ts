import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../../../core/http/api-helpers';
import {
  AdjustStockRequest,
  CreateProductRequest,
  ProductResponse,
  StockLevelResponse,
  UpdateProductRequest,
} from '../../../core/models/api.types';

interface ProductQuery {
  readonly supplierId?: string;
  readonly q?: string;
  readonly sort?: string;
  readonly order?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

interface StockQuery {
  readonly q?: string;
  readonly sort?: string;
  readonly order?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

@Injectable({
  providedIn: 'root',
})
export class InventoryApiService {
  private readonly http = inject(HttpClient);

  getProducts(query: ProductQuery = {}): Observable<readonly ProductResponse[]> {
    return this.http.get<readonly ProductResponse[]>('/api/products', {
      params: buildHttpParams(query),
    });
  }

  createProduct(request: CreateProductRequest): Observable<ProductResponse> {
    return this.http.post<ProductResponse>('/api/products', request);
  }

  updateProduct(productId: string, request: UpdateProductRequest): Observable<ProductResponse> {
    return this.http.put<ProductResponse>(`/api/products/${productId}`, request);
  }

  deleteProduct(productId: string): Observable<void> {
    return this.http.delete<void>(`/api/products/${productId}`);
  }

  getStockLevels(query: StockQuery = {}): Observable<readonly StockLevelResponse[]> {
    return this.http.get<readonly StockLevelResponse[]>('/api/products/stock', {
      params: buildHttpParams(query),
    });
  }

  getLowStockProducts(query: StockQuery = {}): Observable<readonly ProductResponse[]> {
    return this.http.get<readonly ProductResponse[]>('/api/products/low-stock', {
      params: buildHttpParams(query),
    });
  }

  adjustStock(productId: string, request: AdjustStockRequest): Observable<StockLevelResponse> {
    return this.http.post<StockLevelResponse>(`/api/products/${productId}/adjust-stock`, request);
  }
}
