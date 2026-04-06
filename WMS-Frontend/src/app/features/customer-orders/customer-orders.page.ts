import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, forkJoin, of } from 'rxjs';
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
  private readonly destroyRef = inject(DestroyRef);
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
  protected readonly activeCustomerOrders = signal<readonly CustomerOrderResponse[]>([]);
  protected readonly lastCreatedCustomerOrder = signal<CustomerOrderResponse | null>(null);
  protected readonly selectedCancelOrder = signal<CustomerOrderResponse | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isLoadingCancelOrder = signal(false);
  protected readonly isCreating = signal(false);
  protected readonly isCancelling = signal(false);
  protected readonly loadError = signal<ApiErrorState | null>(null);
  protected readonly createError = signal<ApiErrorState | null>(null);
  protected readonly cancelError = signal<ApiErrorState | null>(null);
  protected readonly cancelLookupError = signal<ApiErrorState | null>(null);
  protected readonly availableStockLevels = computed(() =>
    this.stockLevels().filter((stockLevel) => stockLevel.quantityOnHand > 0),
  );

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
    customerPhone: ['', [Validators.pattern(/^$|^[0-9+()\-\s]+$/)]],
    lines: this.formBuilder.array([this.createOrderLineGroup()]),
  });

  protected readonly cancelOrderForm = this.formBuilder.nonNullable.group({
    customerOrderId: ['', [Validators.required]],
    reason: ['', [Validators.required]],
  });

  constructor() {
    this.cancelOrderForm.controls.customerOrderId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((customerOrderId) => {
        const normalizedCustomerOrderId = customerOrderId.trim();
        const selectedOrder = this.selectedCancelOrder();

        if (selectedOrder && selectedOrder.customerOrderId !== normalizedCustomerOrderId) {
          this.selectedCancelOrder.set(null);
        }

        this.cancelLookupError.set(null);
      });

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
      this.activeCustomerOrders.set([]);
      this.loadError.set(null);
      this.selectedCancelOrder.set(null);
      this.cancelLookupError.set(null);
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

    forkJoin({
      stockLevels: this.inventoryApi.getStockLevels({
        sort: 'name',
        order: 'asc',
      }),
      activeCustomerOrders: this.customerOrdersApi
        .getOpenCustomerOrders({
          sort: 'createdAt',
          order: 'desc',
        })
        .pipe(catchError(() => of<readonly CustomerOrderResponse[]>([]))),
    })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: ({ stockLevels, activeCustomerOrders }) => {
          this.stockLevels.set(stockLevels);
          this.activeCustomerOrders.set(activeCustomerOrders);
          this.syncOrderLinesWithAvailableStock();
        },
        error: (error) => {
          this.loadError.set(toApiErrorState(error));
        },
      });
  }

  protected addOrderLine(): void {
    this.orderLines.push(this.createOrderLineGroup());
    this.syncOrderLinesWithAvailableStock();
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

    const formValue = this.createOrderForm.getRawValue();
    const stockValidationMessage = this.validateCreateOrderLines(formValue.lines);
    if (stockValidationMessage) {
      this.createError.set(this.createPageErrorState(stockValidationMessage));
      return;
    }

    this.isCreating.set(true);

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
        next: (customerOrder) => {
          this.lastCreatedCustomerOrder.set(customerOrder);
          this.selectedCancelOrder.set(customerOrder);
          this.cancelOrderForm.controls.customerOrderId.setValue(customerOrder.customerOrderId);
          this.cancelOrderForm.controls.reason.setValue('');
          this.resetCreateOrderForm();
          this.loadWarehouseData();
        },
        error: (error) => {
          this.createError.set(toApiErrorState(error));
        },
      });
  }

  protected useLastCreatedOrder(): void {
    const customerOrder = this.lastCreatedCustomerOrder();
    if (!customerOrder) {
      return;
    }

    this.cancelOrderForm.controls.customerOrderId.setValue(customerOrder.customerOrderId);
    this.selectedCancelOrder.set(customerOrder);
    this.cancelLookupError.set(null);
  }

  protected useCustomerOrderForCancel(customerOrder: CustomerOrderResponse): void {
    this.cancelOrderForm.controls.customerOrderId.setValue(customerOrder.customerOrderId);
    this.selectedCancelOrder.set(customerOrder);
    this.cancelLookupError.set(null);
  }

  protected loadCustomerOrderForCancel(): void {
    const customerOrderId = this.cancelOrderForm.controls.customerOrderId.getRawValue().trim();

    this.cancelError.set(null);
    this.cancelLookupError.set(null);

    if (!customerOrderId || !this.isWarehouseStaff()) {
      this.selectedCancelOrder.set(null);
      return;
    }

    this.isLoadingCancelOrder.set(true);

    this.customerOrdersApi
      .getCustomerOrder(customerOrderId)
      .pipe(
        finalize(() => {
          this.isLoadingCancelOrder.set(false);
        }),
      )
      .subscribe({
        next: (customerOrder) => {
          this.cancelOrderForm.controls.customerOrderId.setValue(customerOrder.customerOrderId);
          this.selectedCancelOrder.set(customerOrder);
        },
        error: (error) => {
          this.selectedCancelOrder.set(null);
          this.cancelLookupError.set(toApiErrorState(error));
        },
      });
  }

  protected submitCancelOrder(): void {
    this.cancelOrderForm.markAllAsTouched();
    this.cancelError.set(null);

    if (this.cancelOrderForm.invalid || !this.isWarehouseStaff()) {
      return;
    }

    const formValue = this.cancelOrderForm.getRawValue();
    const customerOrderId = formValue.customerOrderId.trim();
    const selectedOrder = this.selectedCancelOrder();

    if (
      selectedOrder?.customerOrderId === customerOrderId &&
      selectedOrder.status === 'Cancelled'
    ) {
      this.cancelError.set(this.createPageErrorState('This customer order is already cancelled.'));
      return;
    }

    this.isCancelling.set(true);

    this.customerOrdersApi
      .cancelCustomerOrder(customerOrderId, {
        reason: formValue.reason.trim(),
      })
      .pipe(
        finalize(() => {
          this.isCancelling.set(false);
        }),
      )
      .subscribe({
        next: (customerOrder) => {
          this.selectedCancelOrder.set(customerOrder);

          if (this.lastCreatedCustomerOrder()?.customerOrderId === customerOrder.customerOrderId) {
            this.lastCreatedCustomerOrder.set(customerOrder);
          }

          this.cancelOrderForm.controls.reason.setValue('');
          this.loadWarehouseData();
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

  protected customerFieldErrors(controlName: 'customerName' | 'customerEmail' | 'customerPhone') {
    const control = this.createOrderForm.controls[controlName];
    const errors: string[] = [];

    if (control.hasError('required')) {
      errors.push('This field is required.');
    }

    if (control.hasError('email')) {
      errors.push('Enter a valid email address.');
    }

    if (control.hasError('pattern')) {
      errors.push('Enter a valid phone number.');
    }

    return errors;
  }

  protected showCustomerFieldError(
    controlName: 'customerName' | 'customerEmail' | 'customerPhone',
  ) {
    const control = this.createOrderForm.controls[controlName];
    return Boolean(control.invalid && (control.touched || control.dirty));
  }

  protected hasAvailableStock(): boolean {
    return this.availableStockLevels().length > 0;
  }

  protected stockLevelLabel(stockLevel: StockLevelResponse): string {
    return `${stockLevel.name} (${stockLevel.sku})`;
  }

  protected availableStockForLine(index: number): number | null {
    const productId = String(this.orderLines.at(index).get('productId')?.value ?? '');
    if (!productId) {
      return null;
    }

    return (
      this.stockLevels().find((stockLevel) => stockLevel.productId === productId)?.quantityOnHand ??
      null
    );
  }

  protected canCancelSelectedOrder(): boolean {
    const selectedOrder = this.selectedCancelOrder();
    return Boolean(this.cancelOrderForm.valid && selectedOrder?.status !== 'Cancelled');
  }

  protected hasActiveCustomerOrders(): boolean {
    return this.activeCustomerOrders().length > 0;
  }

  protected customerOrderLineLabel(productId: string): string {
    return (
      this.stockLevels().find((stockLevel) => stockLevel.productId === productId)?.name ?? productId
    );
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

  private syncOrderLinesWithAvailableStock(): void {
    const availableProductIds = new Set(
      this.availableStockLevels().map((stockLevel) => stockLevel.productId),
    );

    for (let index = 0; index < this.orderLines.length; index += 1) {
      const group = this.orderLines.at(index);
      const productId = String(group.get('productId')?.value ?? '');

      if (productId && !availableProductIds.has(productId)) {
        group.patchValue({ productId: '' }, { emitEvent: false });
      }
    }
  }

  private validateCreateOrderLines(
    lines: readonly { productId: string; quantity: number }[],
  ): string | null {
    const availableByProduct = new Map(
      this.stockLevels().map((stockLevel) => [stockLevel.productId, stockLevel]),
    );
    const requestedQuantities = new Map<string, number>();

    for (const line of lines) {
      requestedQuantities.set(
        line.productId,
        (requestedQuantities.get(line.productId) ?? 0) + Number(line.quantity),
      );
    }

    for (const [productId, requestedQuantity] of requestedQuantities) {
      const stockLevel = availableByProduct.get(productId);
      if (!stockLevel) {
        return 'Choose a product that currently has stock available.';
      }

      if (requestedQuantity > stockLevel.quantityOnHand) {
        return `${stockLevel.name} only has ${stockLevel.quantityOnHand} item(s) in stock.`;
      }
    }

    return null;
  }

  private normalizeOptionalValue(value: string): string | null {
    return value.trim() || null;
  }

  private createPageErrorState(
    message: string,
    errors: Record<string, readonly string[]> = {},
  ): ApiErrorState {
    return {
      message,
      errors,
    };
  }
}
