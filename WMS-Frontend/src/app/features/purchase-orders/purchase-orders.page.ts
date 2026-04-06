import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
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
import { catchError, finalize, forkJoin, of, startWith } from 'rxjs';
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

interface OutstandingReceiptLineView {
  readonly productId: string;
  readonly name: string;
  readonly sku: string;
  readonly quantityOrdered: number;
  readonly quantityReceived: number;
  readonly quantityOutstanding: number;
}

interface ReceiptProductOptionView {
  readonly productId: string;
  readonly label: string;
}

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
  private readonly destroyRef = inject(DestroyRef);
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
  protected readonly openPurchaseOrders = signal<readonly PurchaseOrderResponse[]>([]);
  protected readonly receipts = signal<readonly GoodsReceiptResponse[]>([]);
  protected readonly lastCreatedPurchaseOrder = signal<PurchaseOrderResponse | null>(null);
  protected readonly loadedReceiptPurchaseOrder = signal<PurchaseOrderResponse | null>(null);
  protected readonly selectedPurchaseOrderId = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isLoadingReceipts = signal(false);
  protected readonly isLoadingReceiptOrder = signal(false);
  protected readonly isCreating = signal(false);
  protected readonly isReceiving = signal(false);
  protected readonly loadError = signal<ApiErrorState | null>(null);
  protected readonly createError = signal<ApiErrorState | null>(null);
  protected readonly receiveError = signal<ApiErrorState | null>(null);
  protected readonly receiptLookupError = signal<ApiErrorState | null>(null);

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

  private readonly selectedSupplierId = toSignal(
    this.createPurchaseOrderForm.controls.supplierId.valueChanges.pipe(
      startWith(this.createPurchaseOrderForm.controls.supplierId.getRawValue()),
    ),
    {
      initialValue: this.createPurchaseOrderForm.controls.supplierId.getRawValue(),
    },
  );

  protected readonly availableProducts = computed(() => {
    const supplierId = this.selectedSupplierId();
    if (!supplierId) {
      return [];
    }

    return this.products().filter((product) => product.supplierId === supplierId);
  });

  protected readonly receivableLines = computed<readonly OutstandingReceiptLineView[]>(() => {
    const purchaseOrder = this.loadedReceiptPurchaseOrder();
    if (!purchaseOrder) {
      return [];
    }

    const orderedQuantities = new Map<string, number>();
    for (const line of purchaseOrder.lines) {
      orderedQuantities.set(
        line.productId,
        (orderedQuantities.get(line.productId) ?? 0) + line.quantityOrdered,
      );
    }

    const receivedQuantities = new Map<string, number>();
    for (const receipt of this.receipts()) {
      for (const line of receipt.lines) {
        receivedQuantities.set(
          line.productId,
          (receivedQuantities.get(line.productId) ?? 0) + line.quantityReceived,
        );
      }
    }

    return Array.from(orderedQuantities.entries())
      .map(([productId, quantityOrdered]) => {
        const quantityReceived = receivedQuantities.get(productId) ?? 0;

        return {
          productId,
          name: this.productName(productId),
          sku: this.productSku(productId),
          quantityOrdered,
          quantityReceived,
          quantityOutstanding: Math.max(0, quantityOrdered - quantityReceived),
        };
      })
      .filter((line) => line.quantityOutstanding > 0)
      .sort((left, right) => left.name.localeCompare(right.name));
  });
  protected readonly receiptProductOptions = computed<readonly ReceiptProductOptionView[]>(() => {
    const purchaseOrder = this.loadedReceiptPurchaseOrder();

    if (purchaseOrder) {
      return this.receivableLines().map((line) => ({
        productId: line.productId,
        label: this.receiptProductLabel(line),
      }));
    }

    return this.stockLevels().map((stockLevel) => ({
      productId: stockLevel.productId,
      label: `${stockLevel.name} (${stockLevel.sku})`,
    }));
  });

  constructor() {
    this.createPurchaseOrderForm.controls.supplierId.valueChanges
      .pipe(
        startWith(this.createPurchaseOrderForm.controls.supplierId.getRawValue()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.syncPurchaseOrderLinesWithAvailableProducts();
      });

    this.receiveDeliveryForm.controls.purchaseOrderId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((purchaseOrderId) => {
        const normalizedPurchaseOrderId = purchaseOrderId.trim();
        const loadedPurchaseOrder = this.loadedReceiptPurchaseOrder();

        if (
          loadedPurchaseOrder &&
          loadedPurchaseOrder.purchaseOrderId !== normalizedPurchaseOrderId
        ) {
          this.loadedReceiptPurchaseOrder.set(null);
          this.receipts.set([]);
          this.selectedPurchaseOrderId.set(null);
        }

        this.receiptLookupError.set(null);
      });

    effect(() => {
      this.receivableLines();
      this.syncReceiptLinesWithLoadedOrder();
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

      this.suppliers.set([]);
      this.products.set([]);
      this.stockLevels.set([]);
      this.purchaseOrders.set([]);
      this.openPurchaseOrders.set([]);
      this.receipts.set([]);
      this.selectedPurchaseOrderId.set(null);
      this.loadedReceiptPurchaseOrder.set(null);
      this.receiptLookupError.set(null);
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
          this.syncPurchaseOrderLinesWithAvailableProducts();

          const selectedPurchaseOrderId = this.selectedPurchaseOrderId();

          if (
            selectedPurchaseOrderId &&
            purchaseOrders.some(
              (purchaseOrder) => purchaseOrder.purchaseOrderId === selectedPurchaseOrderId,
            )
          ) {
            this.loadReceipts(selectedPurchaseOrderId);
          } else if (this.isManager()) {
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

    forkJoin({
      stockLevels: this.inventoryApi.getStockLevels({ sort: 'name', order: 'asc' }),
      openPurchaseOrders: this.purchaseOrdersApi
        .getOpenPurchaseOrders({
          sort: 'createdAt',
          order: 'desc',
        })
        .pipe(catchError(() => of<readonly PurchaseOrderResponse[]>([]))),
    })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: ({ stockLevels, openPurchaseOrders }) => {
          this.stockLevels.set(stockLevels);
          this.openPurchaseOrders.set(openPurchaseOrders);
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
    this.syncReceiptLinesWithLoadedOrder();
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

    const formValue = this.createPurchaseOrderForm.getRawValue();
    const availableProductIds = new Set(
      this.availableProducts().map((product) => product.productId),
    );
    const hasProductFromWrongSupplier = formValue.lines.some(
      (line) => !availableProductIds.has(line.productId),
    );

    if (hasProductFromWrongSupplier) {
      this.createError.set(
        this.createPageErrorState('Choose products from the selected supplier only.', {
          Lines: ['Choose products from the selected supplier only.'],
        }),
      );
      return;
    }

    this.isCreating.set(true);

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
        next: (purchaseOrder) => {
          this.lastCreatedPurchaseOrder.set(purchaseOrder);
          this.selectedPurchaseOrderId.set(purchaseOrder.purchaseOrderId);
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

    const reason =
      globalThis.prompt?.(
        `Enter a cancellation reason for ${purchaseOrder.purchaseOrderId}:`,
        'No longer required',
      ) ?? null;

    if (!reason?.trim()) {
      return;
    }

    this.purchaseOrdersApi
      .cancelPurchaseOrder(purchaseOrder.purchaseOrderId, {
        reason: reason.trim(),
      })
      .subscribe({
        next: () => {
          if (this.lastCreatedPurchaseOrder()?.purchaseOrderId === purchaseOrder.purchaseOrderId) {
            this.lastCreatedPurchaseOrder.set({
              ...purchaseOrder,
              status: 'Cancelled',
            });
          }

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

  protected usePurchaseOrderForReceipt(purchaseOrder: PurchaseOrderResponse): void {
    this.receiveDeliveryForm.controls.purchaseOrderId.setValue(purchaseOrder.purchaseOrderId);
    this.loadPurchaseOrderForReceipt();
  }

  protected useLastCreatedPurchaseOrder(): void {
    const purchaseOrder = this.lastCreatedPurchaseOrder();
    if (!purchaseOrder) {
      return;
    }

    this.receiveDeliveryForm.controls.purchaseOrderId.setValue(purchaseOrder.purchaseOrderId);
    this.loadPurchaseOrderForReceipt();
  }

  protected useOutstandingQuantities(): void {
    const receivableLines = this.receivableLines();

    if (!receivableLines.length) {
      return;
    }

    this.receiptLines.clear();

    for (const line of receivableLines) {
      const group = this.createReceiveDeliveryLineGroup();
      group.patchValue(
        {
          productId: line.productId,
          quantityReceived: line.quantityOutstanding,
        },
        { emitEvent: false },
      );
      this.receiptLines.push(group);
    }
  }

  protected loadPurchaseOrderForReceipt(): void {
    const purchaseOrderId = this.receiveDeliveryForm.controls.purchaseOrderId.getRawValue().trim();

    this.receiveError.set(null);
    this.receiptLookupError.set(null);

    if (!purchaseOrderId || !this.isWarehouseStaff()) {
      this.loadedReceiptPurchaseOrder.set(null);
      this.receipts.set([]);
      this.selectedPurchaseOrderId.set(null);
      return;
    }

    this.isLoadingReceiptOrder.set(true);

    forkJoin({
      purchaseOrder: this.purchaseOrdersApi.getPurchaseOrder(purchaseOrderId),
      receipts: this.purchaseOrdersApi.getReceipts(purchaseOrderId, {
        sort: 'receivedAt',
        order: 'desc',
      }),
    })
      .pipe(
        finalize(() => {
          this.isLoadingReceiptOrder.set(false);
        }),
      )
      .subscribe({
        next: ({ purchaseOrder, receipts }) => {
          this.receiveDeliveryForm.controls.purchaseOrderId.setValue(purchaseOrder.purchaseOrderId);
          this.loadedReceiptPurchaseOrder.set(purchaseOrder);
          this.selectedPurchaseOrderId.set(purchaseOrder.purchaseOrderId);
          this.receipts.set(receipts);
        },
        error: (error) => {
          this.loadedReceiptPurchaseOrder.set(null);
          this.receipts.set([]);
          this.selectedPurchaseOrderId.set(null);
          this.receiptLookupError.set(toApiErrorState(error));
        },
      });
  }

  protected submitReceipt(): void {
    this.receiveDeliveryForm.markAllAsTouched();
    this.receiveError.set(null);

    if (this.receiveDeliveryForm.invalid || !this.isWarehouseStaff()) {
      return;
    }

    const formValue = this.receiveDeliveryForm.getRawValue();
    const purchaseOrderId = formValue.purchaseOrderId.trim();
    const loadedPurchaseOrder = this.loadedReceiptPurchaseOrder();
    const isLoadedPurchaseOrder = loadedPurchaseOrder?.purchaseOrderId === purchaseOrderId;

    if (
      isLoadedPurchaseOrder &&
      loadedPurchaseOrder.status !== 'Pending' &&
      loadedPurchaseOrder.status !== 'PartiallyReceived'
    ) {
      this.receiveError.set(
        this.createPageErrorState(
          'This purchase order can no longer receive additional deliveries.',
        ),
      );
      return;
    }

    if (isLoadedPurchaseOrder) {
      const receiptValidationMessage = this.validateReceiptLines(formValue.lines);
      if (receiptValidationMessage) {
        this.receiveError.set(this.createPageErrorState(receiptValidationMessage));
        return;
      }
    }

    this.isReceiving.set(true);

    this.purchaseOrdersApi
      .receiveDelivery(purchaseOrderId, {
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
          this.loadWarehouseData();
          this.receiveDeliveryForm.controls.purchaseOrderId.setValue(purchaseOrderId);
          this.loadPurchaseOrderForReceipt();
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

  protected productOptionLabel(product: ProductResponse): string {
    return `${product.name} (${product.sku})`;
  }

  protected hasAvailableProducts(): boolean {
    return this.availableProducts().length > 0;
  }

  protected hasOpenPurchaseOrders(): boolean {
    return this.openPurchaseOrders().length > 0;
  }

  protected receiptProductLabel(line: OutstandingReceiptLineView): string {
    return `${line.name} (${line.sku})`;
  }

  protected hasReceiptProductOptions(): boolean {
    return this.receiptProductOptions().length > 0;
  }

  protected canRecordDelivery(): boolean {
    const purchaseOrder = this.loadedReceiptPurchaseOrder();

    if (!this.receiveDeliveryForm.valid) {
      return false;
    }

    if (!purchaseOrder) {
      return true;
    }

    return Boolean(
      (purchaseOrder.status === 'Pending' || purchaseOrder.status === 'PartiallyReceived') &&
      this.receivableLines().length,
    );
  }

  protected outstandingQuantityForReceiptLine(index: number): number | null {
    const productId = String(this.receiptLineGroup(index).get('productId')?.value ?? '');
    if (!productId) {
      return null;
    }

    return (
      this.receivableLines().find((line) => line.productId === productId)?.quantityOutstanding ??
      null
    );
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

  private resetReceiveDeliveryForm(purchaseOrderId = ''): void {
    this.receiveError.set(null);
    this.loadedReceiptPurchaseOrder.set(null);
    this.receipts.set([]);
    this.selectedPurchaseOrderId.set(null);
    this.receiveDeliveryForm.reset({
      purchaseOrderId,
      lines: [this.createReceiveDeliveryLineGroup().getRawValue()],
    });
    this.receiptLines.clear();
    this.receiptLines.push(this.createReceiveDeliveryLineGroup());
  }

  private syncPurchaseOrderLinesWithAvailableProducts(): void {
    const availableProductIds = new Set(
      this.availableProducts().map((product) => product.productId),
    );

    for (let index = 0; index < this.purchaseOrderLines.length; index += 1) {
      const group = this.lineGroup(index);
      const productId = String(group.get('productId')?.value ?? '');

      if (productId && !availableProductIds.has(productId)) {
        group.patchValue({ productId: '' }, { emitEvent: false });
      }
    }
  }

  private syncReceiptLinesWithLoadedOrder(): void {
    if (this.receiptLines.length === 0) {
      this.receiptLines.push(this.createReceiveDeliveryLineGroup());
    }

    const receivableLines = this.receivableLines();
    const receivableProductIds = new Set(receivableLines.map((line) => line.productId));
    const firstReceivableProductId = receivableLines[0]?.productId ?? '';

    for (let index = 0; index < this.receiptLines.length; index += 1) {
      const group = this.receiptLineGroup(index);
      const productId = String(group.get('productId')?.value ?? '');

      if (productId && !receivableProductIds.has(productId)) {
        group.patchValue(
          {
            productId: '',
            quantityReceived: 1,
          },
          { emitEvent: false },
        );
      }
    }

    if (firstReceivableProductId) {
      const firstGroup = this.receiptLineGroup(0);
      const firstProductId = String(firstGroup.get('productId')?.value ?? '');

      if (!firstProductId) {
        firstGroup.patchValue(
          {
            productId: firstReceivableProductId,
            quantityReceived: 1,
          },
          { emitEvent: false },
        );
      }
    }
  }

  private validateReceiptLines(
    lines: readonly { productId: string; quantityReceived: number }[],
  ): string | null {
    const requestedQuantities = new Map<string, number>();
    for (const line of lines) {
      requestedQuantities.set(
        line.productId,
        (requestedQuantities.get(line.productId) ?? 0) + Number(line.quantityReceived),
      );
    }

    for (const [productId, requestedQuantity] of requestedQuantities) {
      const receivableLine = this.receivableLines().find((line) => line.productId === productId);
      if (!receivableLine) {
        return 'Receipt lines must match the selected purchase order.';
      }

      if (requestedQuantity > receivableLine.quantityOutstanding) {
        return `${receivableLine.name} only has ${receivableLine.quantityOutstanding} item(s) left to receive.`;
      }
    }

    return null;
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

  private productName(productId: string): string {
    return (
      this.products().find((product) => product.productId === productId)?.name ??
      this.stockLevels().find((stockLevel) => stockLevel.productId === productId)?.name ??
      productId
    );
  }

  private productSku(productId: string): string {
    return (
      this.products().find((product) => product.productId === productId)?.sku ??
      this.stockLevels().find((stockLevel) => stockLevel.productId === productId)?.sku ??
      'Unknown SKU'
    );
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
