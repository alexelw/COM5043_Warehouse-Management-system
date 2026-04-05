import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { finalize } from 'rxjs';
import { getFieldErrors, toApiErrorState } from '../../core/http/api-helpers';
import {
  ApiErrorState,
  FinancialReportResponse,
  FinancialTransactionResponse,
  REPORT_FORMATS,
  REPORT_TYPES,
  ReportExportResponse,
  TRANSACTION_STATUSES,
  TRANSACTION_TYPES,
  VOID_OR_REVERSE_ACTIONS,
} from '../../core/models/api.types';
import { RoleService } from '../../core/services/role.service';
import { dateRangeValidator } from '../../core/utils/date-range.validator';
import { formatDate, formatDateTime, formatMoney } from '../../core/utils/formatters';
import { toStatusTone } from '../../core/utils/status-tone';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { StatusBadgeComponent } from '../../shared/ui/status-badge/status-badge';
import { FinanceReportsApiService } from './data/finance-reports.api';

function voidOrReverseValidator(control: AbstractControl): ValidationErrors | null {
  const action = control.get('action')?.value as string | null | undefined;
  const reason = control.get('reason')?.value as string | null | undefined;

  if (action === 'Void' && !reason?.trim()) {
    return { reasonRequired: true };
  }

  return null;
}

@Component({
  selector: 'app-finance-reports-page',
  imports: [
    ReactiveFormsModule,
    EmptyStateComponent,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './finance-reports.page.html',
  styleUrl: './finance-reports.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinanceReportsPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly roleService = inject(RoleService);
  private readonly financeReportsApi = inject(FinanceReportsApiService);

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly isAdministrator = computed(() => this.selectedRole() === 'Administrator');
  protected readonly transactionTypes = TRANSACTION_TYPES;
  protected readonly transactionStatuses = TRANSACTION_STATUSES;
  protected readonly reportTypes = REPORT_TYPES;
  protected readonly reportFormats = REPORT_FORMATS;
  protected readonly actionOptions = VOID_OR_REVERSE_ACTIONS;
  protected readonly transactions = signal<readonly FinancialTransactionResponse[]>([]);
  protected readonly report = signal<FinancialReportResponse | null>(null);
  protected readonly exports = signal<readonly ReportExportResponse[]>([]);
  protected readonly lastExport = signal<ReportExportResponse | null>(null);
  protected readonly isLoadingTransactions = signal(false);
  protected readonly isLoadingReport = signal(false);
  protected readonly isLoadingExports = signal(false);
  protected readonly isExporting = signal(false);
  protected readonly isSubmittingAction = signal(false);
  protected readonly transactionsError = signal<ApiErrorState | null>(null);
  protected readonly reportError = signal<ApiErrorState | null>(null);
  protected readonly exportsError = signal<ApiErrorState | null>(null);
  protected readonly exportError = signal<ApiErrorState | null>(null);
  protected readonly actionError = signal<ApiErrorState | null>(null);

  protected readonly transactionFiltersForm = this.formBuilder.group(
    {
      type: this.formBuilder.nonNullable.control(''),
      status: this.formBuilder.nonNullable.control(''),
      from: this.formBuilder.nonNullable.control(''),
      to: this.formBuilder.nonNullable.control(''),
    },
    { validators: dateRangeValidator('from', 'to') },
  );

  protected readonly reportForm = this.formBuilder.group(
    {
      format: this.formBuilder.nonNullable.control<'TXT' | 'JSON'>('TXT', [Validators.required]),
      from: this.formBuilder.nonNullable.control(''),
      to: this.formBuilder.nonNullable.control(''),
    },
    { validators: dateRangeValidator('from', 'to') },
  );

  protected readonly exportFiltersForm = this.formBuilder.group(
    {
      reportType: this.formBuilder.nonNullable.control(''),
      format: this.formBuilder.nonNullable.control(''),
      from: this.formBuilder.nonNullable.control(''),
      to: this.formBuilder.nonNullable.control(''),
    },
    { validators: dateRangeValidator('from', 'to') },
  );

  protected readonly actionForm = this.formBuilder.group(
    {
      transactionId: this.formBuilder.nonNullable.control('', [Validators.required]),
      action: this.formBuilder.nonNullable.control<'Void' | 'Reverse'>('Void', [
        Validators.required,
      ]),
      reason: this.formBuilder.nonNullable.control(''),
    },
    { validators: voidOrReverseValidator },
  );

  constructor() {
    effect(() => {
      if (this.isAdministrator()) {
        this.refreshAll();
        return;
      }

      this.transactions.set([]);
      this.report.set(null);
      this.exports.set([]);
      this.lastExport.set(null);
      this.transactionsError.set(null);
      this.reportError.set(null);
      this.exportsError.set(null);
      this.exportError.set(null);
      this.actionError.set(null);
    });
  }

  protected refreshAll(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.loadTransactions();
    this.loadReportPreview();
    this.loadExports();
  }

  protected loadTransactions(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.transactionFiltersForm.markAllAsTouched();

    if (this.transactionFiltersForm.invalid) {
      return;
    }

    this.isLoadingTransactions.set(true);
    this.transactionsError.set(null);

    const filterValues = this.transactionFiltersForm.getRawValue();

    this.financeReportsApi
      .getTransactions({
        type: filterValues.type || undefined,
        status: filterValues.status || undefined,
        from: filterValues.from || undefined,
        to: filterValues.to || undefined,
        sort: 'occurredAt',
        order: 'desc',
      })
      .pipe(
        finalize(() => {
          this.isLoadingTransactions.set(false);
        }),
      )
      .subscribe({
        next: (transactions) => {
          this.transactions.set(transactions);
        },
        error: (error) => {
          this.transactionsError.set(toApiErrorState(error));
        },
      });
  }

  protected loadReportPreview(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.reportForm.markAllAsTouched();

    if (this.reportForm.invalid) {
      return;
    }

    this.isLoadingReport.set(true);
    this.reportError.set(null);

    const filterValues = this.reportForm.getRawValue();

    this.financeReportsApi
      .getFinancialReport(filterValues.from || null, filterValues.to || null)
      .pipe(
        finalize(() => {
          this.isLoadingReport.set(false);
        }),
      )
      .subscribe({
        next: (report) => {
          this.report.set(report);
        },
        error: (error) => {
          this.reportError.set(toApiErrorState(error));
        },
      });
  }

  protected loadExports(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.exportFiltersForm.markAllAsTouched();

    if (this.exportFiltersForm.invalid) {
      return;
    }

    this.isLoadingExports.set(true);
    this.exportsError.set(null);

    const filterValues = this.exportFiltersForm.getRawValue();

    this.financeReportsApi
      .getReportExports({
        reportType: filterValues.reportType || undefined,
        format: filterValues.format || undefined,
        from: filterValues.from || undefined,
        to: filterValues.to || undefined,
        sort: 'generatedAt',
        order: 'desc',
      })
      .pipe(
        finalize(() => {
          this.isLoadingExports.set(false);
        }),
      )
      .subscribe({
        next: (exports) => {
          this.exports.set(exports);
        },
        error: (error) => {
          this.exportsError.set(toApiErrorState(error));
        },
      });
  }

  protected submitReportExport(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.reportForm.markAllAsTouched();
    this.exportError.set(null);

    if (this.reportForm.invalid) {
      return;
    }

    this.isExporting.set(true);

    const reportValues = this.reportForm.getRawValue();

    this.financeReportsApi
      .exportFinancialReport({
        format: reportValues.format,
        from: reportValues.from || null,
        to: reportValues.to || null,
      })
      .pipe(
        finalize(() => {
          this.isExporting.set(false);
        }),
      )
      .subscribe({
        next: (reportExport) => {
          this.lastExport.set(reportExport);
          this.loadExports();
        },
        error: (error) => {
          this.exportError.set(toApiErrorState(error));
        },
      });
  }

  protected useTransaction(transaction: FinancialTransactionResponse): void {
    this.actionError.set(null);
    this.actionForm.patchValue({
      transactionId: transaction.transactionId,
    });
  }

  protected submitTransactionAction(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.actionForm.markAllAsTouched();
    this.actionError.set(null);

    if (this.actionForm.invalid) {
      return;
    }

    this.isSubmittingAction.set(true);

    const actionValues = this.actionForm.getRawValue();

    this.financeReportsApi
      .voidOrReverseTransaction(actionValues.transactionId.trim(), {
        action: actionValues.action,
        reason: this.normalizeOptionalValue(actionValues.reason),
      })
      .pipe(
        finalize(() => {
          this.isSubmittingAction.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.resetActionForm();
          this.loadTransactions();
          this.loadReportPreview();
        },
        error: (error) => {
          this.actionError.set(toApiErrorState(error));
        },
      });
  }

  protected clearActionForm(): void {
    this.resetActionForm();
    this.actionError.set(null);
  }

  protected isActionReasonRequired(): boolean {
    return this.actionForm.controls.action.getRawValue() === 'Void';
  }

  protected hasActionControlError(controlName: 'transactionId' | 'reason'): boolean {
    const control = this.actionForm.controls[controlName];

    return control.invalid && (control.touched || control.dirty);
  }

  protected getActionControlErrors(controlName: 'transactionId' | 'reason'): readonly string[] {
    const control = this.actionForm.controls[controlName];
    const errors: string[] = [];

    if (controlName === 'transactionId' && control.hasError('required')) {
      errors.push('Choose a transaction or paste its ID.');
    }

    if (controlName === 'reason' && this.actionForm.hasError('reasonRequired')) {
      errors.push('A reason is required when voiding a transaction.');
    }

    return errors;
  }

  protected actionServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.actionError(), ...fieldNames);
  }

  protected exportServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.exportError(), ...fieldNames);
  }

  protected statusTone(status: string) {
    return toStatusTone(status);
  }

  protected formatAmount(value: FinancialTransactionResponse['amount']): string {
    return formatMoney(value);
  }

  protected formatTotalSales(report: FinancialReportResponse): string {
    return formatMoney(report.totalSales);
  }

  protected formatTotalExpenses(report: FinancialReportResponse): string {
    return formatMoney(report.totalExpenses);
  }

  protected formatNetPosition(report: FinancialReportResponse): string {
    return `${report.totalSales.currency} ${(report.totalSales.amount - report.totalExpenses.amount).toFixed(2)}`;
  }

  protected formatOccurredAt(value: string): string {
    return formatDateTime(value);
  }

  protected formatGeneratedAt(value: string): string {
    return formatDateTime(value);
  }

  protected formatReportWindow(report: FinancialReportResponse): string {
    if (!report.from && !report.to) {
      return 'All recorded dates';
    }

    const from = formatDate(report.from);
    const to = formatDate(report.to);

    if (report.from && report.to) {
      return `${from} to ${to}`;
    }

    return report.from ? `From ${from}` : `Until ${to}`;
  }

  private resetActionForm(): void {
    this.actionForm.reset({
      transactionId: '',
      action: 'Void',
      reason: '',
    });
  }

  private normalizeOptionalValue(value: string): string | null {
    const trimmedValue = value.trim();
    return trimmedValue ? trimmedValue : null;
  }
}
