import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { getFieldErrors, toApiErrorState } from '../../core/http/api-helpers';
import {
  ApiErrorState,
  CUSTOMER_ORDER_STATUSES,
  CustomerOrderResponse,
  StockLevelResponse,
} from '../../core/models/api.types';
import { RoleService } from '../../core/services/role.service';
import { dateRangeValidator } from '../../core/utils/date-range.validator';
import { formatDateTime, formatMoney } from '../../core/utils/formatters';
import { toStatusTone } from '../../core/utils/status-tone';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { StatusBadgeComponent } from '../../shared/ui/status-badge/status-badge';
import { InventoryApiService } from '../inventory/data/inventory.api';
import { CustomerOrdersApiService } from './data/customer-orders.api';

@Component({
  selector: 'app-customer-orders-page',
  imports: [
    ReactiveFormsModule,
    EmptyStateComponent,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './customer-orders.page.html',
  styleUrl: './customer-orders.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerOrdersPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly roleService = inject(RoleService);
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly customerOrdersApi = inject(CustomerOrdersApiService);

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly isManager = computed(() => this.selectedRole() === 'Manager');
  protected readonly isWarehouseStaff = computed(() => this.selectedRole() === 'WarehouseStaff');
  protected readonly customerOrderStatuses = CUSTOMER_ORDER_STATUSES;
  protected readonly stockLevels = signal<readonly StockLevelResponse[]>([]);
  protected readonly customerOrders = signal<readonly CustomerOrderResponse[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isCreating = signal(false);
  protected readonly isCancelling = signal(false);
  protected readonly loadError = signal<ApiErrorState | null>(null);
  protected readonly createError = signal<ApiErrorState | null>(null);
  protected readonly cancelError = signal<ApiErrorState | null>(null);

  protected readonly managerFiltersForm = this.formBuilder.group(
    {
      status: this.formBuilder.nonNullable.control(''),
      from: this.formBuilder.nonNullable.control(''),
      to: this.formBuilder.nonNullable.control(''),
    },
    { validators: dateRangeValidator('from', 'to') },
  );

  protected readonly createOrderForm = this.formBuilder.nonNullable.group({
    customerName: ['', [Validators.required]],
    customerEmail: ['', [Validators.email]],
    customerPhone: ['', [Validators.pattern(/^[0-9+()\-\s]*$/)]],
    lines: this.formBuilder.array([this.createOrderLineGroup()]),
  });

  protected readonly cancelOrderForm = this.formBuilder.nonNullable.group({
    customerOrderId: ['', [Validators.required]],
    reason: ['', [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (this.isManager()) {
        this.loadManagerData();
        return;
      }

      if (this.isWarehouseStaff()) {
        this.loadWarehouseData();
        return;
      }

      this.customerOrders.set([]);
      this.stockLevels.set([]);
      this.loadError.set(null);
    });
  }

  protected get orderLines(): FormArray {
    return this.createOrderForm.controls.lines;
  }

  protected loadManagerData(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const filterValues = this.managerFiltersForm.getRawValue();

    this.customerOrdersApi
      .getCustomerOrders({
        status: filterValues.status || undefined,
        from: filterValues.from || undefined,
        to: filterValues.to || undefined,
        sort: 'createdAt',
        order: 'desc',
      })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: (customerOrders) => {
          this.customerOrders.set(customerOrders);
        },
        error: (error) => {
          this.loadError.set(toApiErrorState(error));
        },
      });
  }

  protected loadWarehouseData(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.inventoryApi
      .getStockLevels({
        sort: 'name',
        order: 'asc',
      })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: (stockLevels) => {
          this.stockLevels.set(stockLevels);
        },
        error: (error) => {
          this.loadError.set(toApiErrorState(error));
        },
      });
  }

  protected addOrderLine(): void {
    this.orderLines.push(this.createOrderLineGroup());
  }

  protected removeOrderLine(index: number): void {
    if (this.orderLines.length === 1) {
      return;
    }

    this.orderLines.removeAt(index);
  }

  protected submitCustomerOrder(): void {
    this.createOrderForm.markAllAsTouched();
    this.createError.set(null);

    if (this.createOrderForm.invalid || !this.isWarehouseStaff()) {
      return;
    }

    this.isCreating.set(true);

    const formValue = this.createOrderForm.getRawValue();

    this.customerOrdersApi
      .createCustomerOrder({
        customer: {
          name: formValue.customerName.trim(),
          email: this.normalizeOptionalValue(formValue.customerEmail),
          phone: this.normalizeOptionalValue(formValue.customerPhone),
        },
        lines: formValue.lines.map((line) => ({
          productId: line.productId,
          quantity: Number(line.quantity),
          unitPrice: {
            amount: Number(line.unitPriceAmount),
            currency: 'GBP',
          },
        })),
      })
      .pipe(
        finalize(() => {
          this.isCreating.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.resetCreateOrderForm();
        },
        error: (error) => {
          this.createError.set(toApiErrorState(error));
        },
      });
  }

  protected submitCancelOrder(): void {
    this.cancelOrderForm.markAllAsTouched();
    this.cancelError.set(null);

    if (this.cancelOrderForm.invalid || !this.isWarehouseStaff()) {
      return;
    }

    this.isCancelling.set(true);

    const formValue = this.cancelOrderForm.getRawValue();

    this.customerOrdersApi
      .cancelCustomerOrder(formValue.customerOrderId.trim(), {
        reason: formValue.reason.trim(),
      })
      .pipe(
        finalize(() => {
          this.isCancelling.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.cancelOrderForm.reset({
            customerOrderId: '',
            reason: '',
          });
        },
        error: (error) => {
          this.cancelError.set(toApiErrorState(error));
        },
      });
  }

  protected statusTone(status: string) {
    return toStatusTone(status);
  }

  protected formatCreatedAt(value: string): string {
    return formatDateTime(value);
  }

  protected formatTotal(order: CustomerOrderResponse): string {
    return formatMoney(order.totalAmount);
  }

  protected lineErrors(index: number, controlName: 'productId' | 'quantity' | 'unitPriceAmount') {
    const control = this.orderLines.at(index).get(controlName);
    const errors: string[] = [];

    if (control?.hasError('required')) {
      errors.push('This field is required.');
    }

    if (control?.hasError('min')) {
      errors.push('Enter a value above the minimum.');
    }

    return errors;
  }

  protected showLineError(
    index: number,
    controlName: 'productId' | 'quantity' | 'unitPriceAmount',
  ) {
    const control = this.orderLines.at(index).get(controlName);
    return Boolean(control?.invalid && (control.touched || control.dirty));
  }

  protected createServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.createError(), ...fieldNames);
  }

  protected cancelServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.cancelError(), ...fieldNames);
  }

  private createOrderLineGroup() {
    return this.formBuilder.nonNullable.group({
      productId: ['', [Validators.required]],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPriceAmount: [0.01, [Validators.required, Validators.min(0.01)]],
    });
  }

  private resetCreateOrderForm(): void {
    this.createOrderForm.reset({
      customerName: '',
      customerEmail: '',
      customerPhone: '',
      lines: [this.createOrderLineGroup().getRawValue()],
    });
    this.orderLines.clear();
    this.orderLines.push(this.createOrderLineGroup());
  }

  private normalizeOptionalValue(value: string): string | null {
    const trimmedValue = value.trim();
    return trimmedValue ? trimmedValue : null;
  }
}
