import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { getFieldErrors, toApiErrorState } from '../../core/http/api-helpers';
import {
  ApiErrorState,
  GoodsReceiptResponse,
  ProductResponse,
  PURCHASE_ORDER_STATUSES,
  PurchaseOrderResponse,
  StockLevelResponse,
  SupplierResponse,
} from '../../core/models/api.types';
import { RoleService } from '../../core/services/role.service';
import { dateRangeValidator } from '../../core/utils/date-range.validator';
import { formatDateTime } from '../../core/utils/formatters';
import { toStatusTone } from '../../core/utils/status-tone';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { StatusBadgeComponent } from '../../shared/ui/status-badge/status-badge';
import { InventoryApiService } from '../inventory/data/inventory.api';
import { SuppliersApiService } from '../suppliers/data/suppliers.api';
import { PurchaseOrdersApiService } from './data/purchase-orders.api';

@Component({
  selector: 'app-purchase-orders-page',
  imports: [
    ReactiveFormsModule,
    EmptyStateComponent,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './purchase-orders.page.html',
  styleUrl: './purchase-orders.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseOrdersPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly roleService = inject(RoleService);
  private readonly suppliersApi = inject(SuppliersApiService);
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly purchaseOrdersApi = inject(PurchaseOrdersApiService);

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly isManager = computed(() => this.selectedRole() === 'Manager');
  protected readonly isWarehouseStaff = computed(() => this.selectedRole() === 'WarehouseStaff');
  protected readonly purchaseOrderStatuses = PURCHASE_ORDER_STATUSES;
  protected readonly suppliers = signal<readonly SupplierResponse[]>([]);
  protected readonly products = signal<readonly ProductResponse[]>([]);
  protected readonly stockLevels = signal<readonly StockLevelResponse[]>([]);
  protected readonly purchaseOrders = signal<readonly PurchaseOrderResponse[]>([]);
  protected readonly receipts = signal<readonly GoodsReceiptResponse[]>([]);
  protected readonly selectedPurchaseOrderId = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isLoadingReceipts = signal(false);
  protected readonly isCreating = signal(false);
  protected readonly isReceiving = signal(false);
  protected readonly loadError = signal<ApiErrorState | null>(null);
  protected readonly createError = signal<ApiErrorState | null>(null);
  protected readonly receiveError = signal<ApiErrorState | null>(null);

  protected readonly managerFiltersForm = this.formBuilder.group(
    {
      supplierId: this.formBuilder.nonNullable.control(''),
      status: this.formBuilder.nonNullable.control(''),
      from: this.formBuilder.nonNullable.control(''),
      to: this.formBuilder.nonNullable.control(''),
    },
    { validators: dateRangeValidator('from', 'to') },
  );

  protected readonly createPurchaseOrderForm = this.formBuilder.nonNullable.group({
    supplierId: ['', [Validators.required]],
    lines: this.formBuilder.array([this.createPurchaseOrderLineGroup()]),
  });

  protected readonly receiveDeliveryForm = this.formBuilder.nonNullable.group({
    purchaseOrderId: ['', [Validators.required]],
    lines: this.formBuilder.array([this.createReceiveDeliveryLineGroup()]),
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

      this.suppliers.set([]);
      this.products.set([]);
      this.stockLevels.set([]);
      this.purchaseOrders.set([]);
      this.receipts.set([]);
    });
  }

  protected get purchaseOrderLines(): FormArray {
    return this.createPurchaseOrderForm.controls.lines;
  }

  protected get receiptLines(): FormArray {
    return this.receiveDeliveryForm.controls.lines;
  }

  protected loadManagerData(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const filterValues = this.managerFiltersForm.getRawValue();

    forkJoin({
      suppliers: this.suppliersApi.getSuppliers({ sort: 'name', order: 'asc' }),
      products: this.inventoryApi.getProducts({ sort: 'name', order: 'asc' }),
      purchaseOrders: this.purchaseOrdersApi.getPurchaseOrders({
        supplierId: filterValues.supplierId || undefined,
        status: filterValues.status || undefined,
        from: filterValues.from || undefined,
        to: filterValues.to || undefined,
        sort: 'createdAt',
        order: 'desc',
      }),
    })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: ({ suppliers, products, purchaseOrders }) => {
          this.suppliers.set(suppliers);
          this.products.set(products);
          this.purchaseOrders.set(purchaseOrders);

          const selectedPurchaseOrderId = this.selectedPurchaseOrderId();

          if (
            selectedPurchaseOrderId &&
            purchaseOrders.some(
              (purchaseOrder) => purchaseOrder.purchaseOrderId === selectedPurchaseOrderId,
            )
          ) {
            this.loadReceipts(selectedPurchaseOrderId);
          } else {
            this.receipts.set([]);
            this.selectedPurchaseOrderId.set(null);
          }
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
      .getStockLevels({ sort: 'name', order: 'asc' })
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

  protected addPurchaseOrderLine(): void {
    this.purchaseOrderLines.push(this.createPurchaseOrderLineGroup());
  }

  protected removePurchaseOrderLine(index: number): void {
    if (this.purchaseOrderLines.length === 1) {
      return;
    }

    this.purchaseOrderLines.removeAt(index);
  }

  protected addReceiptLine(): void {
    this.receiptLines.push(this.createReceiveDeliveryLineGroup());
  }

  protected removeReceiptLine(index: number): void {
    if (this.receiptLines.length === 1) {
      return;
    }

    this.receiptLines.removeAt(index);
  }

  protected submitPurchaseOrder(): void {
    this.createPurchaseOrderForm.markAllAsTouched();
    this.createError.set(null);

    if (this.createPurchaseOrderForm.invalid || !this.isManager()) {
      return;
    }

    this.isCreating.set(true);

    const formValue = this.createPurchaseOrderForm.getRawValue();

    this.purchaseOrdersApi
      .createPurchaseOrder({
        supplierId: formValue.supplierId,
        lines: formValue.lines.map((line) => ({
          productId: line.productId,
          quantity: Number(line.quantity),
          unitCost: {
            amount: Number(line.unitCostAmount),
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
          this.resetPurchaseOrderForm();
          this.loadManagerData();
        },
        error: (error) => {
          this.createError.set(toApiErrorState(error));
        },
      });
  }

  protected cancelPurchaseOrder(purchaseOrder: PurchaseOrderResponse): void {
    if (!this.isManager()) {
      return;
    }

    const reason = window.prompt(
      `Enter a cancellation reason for ${purchaseOrder.purchaseOrderId}:`,
      'No longer required',
    );

    if (!reason?.trim()) {
      return;
    }

    this.purchaseOrdersApi
      .cancelPurchaseOrder(purchaseOrder.purchaseOrderId, {
        reason: reason.trim(),
      })
      .subscribe({
        next: () => {
          this.loadManagerData();
        },
        error: (error) => {
          this.loadError.set(toApiErrorState(error));
        },
      });
  }

  protected selectPurchaseOrder(purchaseOrderId: string): void {
    this.selectedPurchaseOrderId.set(purchaseOrderId);
    this.loadReceipts(purchaseOrderId);
  }

  protected submitReceipt(): void {
    this.receiveDeliveryForm.markAllAsTouched();
    this.receiveError.set(null);

    if (this.receiveDeliveryForm.invalid || !this.isWarehouseStaff()) {
      return;
    }

    this.isReceiving.set(true);

    const formValue = this.receiveDeliveryForm.getRawValue();

    this.purchaseOrdersApi
      .receiveDelivery(formValue.purchaseOrderId, {
        lines: formValue.lines.map((line) => ({
          productId: line.productId,
          quantityReceived: Number(line.quantityReceived),
        })),
      })
      .pipe(
        finalize(() => {
          this.isReceiving.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.receiveDeliveryForm.reset({
            purchaseOrderId: '',
            lines: [this.createReceiveDeliveryLineGroup().getRawValue()],
          });
          this.receiptLines.clear();
          this.receiptLines.push(this.createReceiveDeliveryLineGroup());
        },
        error: (error) => {
          this.receiveError.set(toApiErrorState(error));
        },
      });
  }

  protected lineGroup(index: number) {
    return this.purchaseOrderLines.at(index);
  }

  protected receiptLineGroup(index: number) {
    return this.receiptLines.at(index);
  }

  protected supplierName(supplierId: string): string {
    return (
      this.suppliers().find((supplier) => supplier.supplierId === supplierId)?.name ?? supplierId
    );
  }

  protected formatCreatedAt(value: string): string {
    return formatDateTime(value);
  }

  protected statusTone(status: string) {
    return toStatusTone(status);
  }

  protected loadReceipts(purchaseOrderId: string): void {
    this.isLoadingReceipts.set(true);

    this.purchaseOrdersApi
      .getReceipts(purchaseOrderId, {
        sort: 'receivedAt',
        order: 'desc',
      })
      .pipe(
        finalize(() => {
          this.isLoadingReceipts.set(false);
        }),
      )
      .subscribe({
        next: (receipts) => {
          this.receipts.set(receipts);
        },
        error: (error) => {
          this.loadError.set(toApiErrorState(error));
          this.receipts.set([]);
        },
      });
  }

  protected purchaseOrderLineErrors(
    index: number,
    controlName: 'productId' | 'quantity' | 'unitCostAmount',
  ) {
    const control = this.lineGroup(index).get(controlName);
    const errors: string[] = [];

    if (control?.hasError('required')) {
      errors.push('This field is required.');
    }

    if (control?.hasError('min')) {
      errors.push('Enter a value above the minimum.');
    }

    return errors;
  }

  protected receiptLineErrors(index: number, controlName: 'productId' | 'quantityReceived') {
    const control = this.receiptLineGroup(index).get(controlName);
    const errors: string[] = [];

    if (control?.hasError('required')) {
      errors.push('This field is required.');
    }

    if (control?.hasError('min')) {
      errors.push('Enter a value above the minimum.');
    }

    return errors;
  }

  protected showPurchaseOrderLineError(
    index: number,
    controlName: 'productId' | 'quantity' | 'unitCostAmount',
  ): boolean {
    const control = this.lineGroup(index).get(controlName);
    return Boolean(control?.invalid && (control.touched || control.dirty));
  }

  protected showReceiptLineError(
    index: number,
    controlName: 'productId' | 'quantityReceived',
  ): boolean {
    const control = this.receiptLineGroup(index).get(controlName);
    return Boolean(control?.invalid && (control.touched || control.dirty));
  }

  protected createServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.createError(), ...fieldNames);
  }

  protected receiveServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.receiveError(), ...fieldNames);
  }

  protected get selectedPurchaseOrder(): PurchaseOrderResponse | null {
    return (
      this.purchaseOrders().find(
        (purchaseOrder) => purchaseOrder.purchaseOrderId === this.selectedPurchaseOrderId(),
      ) ?? null
    );
  }

  private resetPurchaseOrderForm(): void {
    this.createError.set(null);
    this.createPurchaseOrderForm.reset({
      supplierId: '',
      lines: [this.createPurchaseOrderLineGroup().getRawValue()],
    });
    this.purchaseOrderLines.clear();
    this.purchaseOrderLines.push(this.createPurchaseOrderLineGroup());
  }

  private createPurchaseOrderLineGroup() {
    return this.formBuilder.nonNullable.group({
      productId: ['', [Validators.required]],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitCostAmount: [0.01, [Validators.required, Validators.min(0.01)]],
    });
  }

  private createReceiveDeliveryLineGroup() {
    return this.formBuilder.nonNullable.group({
      productId: ['', [Validators.required]],
      quantityReceived: [1, [Validators.required, Validators.min(1)]],
    });
  }
}
