import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../../../core/http/api-helpers';
import {
  ExportFinancialReportRequest,
  FinancialReportResponse,
  FinancialTransactionResponse,
  ReportExportResponse,
  VoidOrReverseTransactionRequest,
} from '../../../core/models/api.types';

interface TransactionQuery {
  readonly type?: string;
  readonly status?: string;
  readonly from?: string;
  readonly to?: string;
  readonly sort?: string;
  readonly order?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

interface ExportQuery {
  readonly reportType?: string;
  readonly format?: string;
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
export class FinanceReportsApiService {
  private readonly http = inject(HttpClient);

  getTransactions(
    query: TransactionQuery = {},
  ): Observable<readonly FinancialTransactionResponse[]> {
    return this.http.get<readonly FinancialTransactionResponse[]>('/api/transactions', {
      params: buildHttpParams(query),
    });
  }

  voidOrReverseTransaction(
    transactionId: string,
    request: VoidOrReverseTransactionRequest,
  ): Observable<FinancialTransactionResponse> {
    return this.http.post<FinancialTransactionResponse>(
      `/api/transactions/${transactionId}/void-or-reverse`,
      request,
    );
  }

  getFinancialReport(
    from?: string | null,
    to?: string | null,
  ): Observable<FinancialReportResponse> {
    return this.http.get<FinancialReportResponse>('/api/reports/financial', {
      params: buildHttpParams({ from, to }),
    });
  }

  exportFinancialReport(request: ExportFinancialReportRequest): Observable<ReportExportResponse> {
    return this.http.post<ReportExportResponse>('/api/reports/financial/export', request);
  }

  getReportExports(query: ExportQuery = {}): Observable<readonly ReportExportResponse[]> {
    return this.http.get<readonly ReportExportResponse[]>('/api/reports/exports', {
      params: buildHttpParams(query),
    });
  }
}
