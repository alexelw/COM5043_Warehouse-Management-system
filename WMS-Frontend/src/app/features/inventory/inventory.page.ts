import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { getFieldErrors, toApiErrorState } from '../../core/http/api-helpers';
import {
  ApiErrorState,
  ProductResponse,
  StockLevelResponse,
  SupplierResponse,
} from '../../core/models/api.types';
import { RoleService } from '../../core/services/role.service';
import { toStatusTone } from '../../core/utils/status-tone';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { StatusBadgeComponent } from '../../shared/ui/status-badge/status-badge';
import { InventoryApiService } from './data/inventory.api';
import { SuppliersApiService } from '../suppliers/data/suppliers.api';

@Component({
  selector: 'app-inventory-page',
  imports: [
    ReactiveFormsModule,
    EmptyStateComponent,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './inventory.page.html',
  styleUrl: './inventory.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly roleService = inject(RoleService);
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly suppliersApi = inject(SuppliersApiService);

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly isManager = computed(() => this.selectedRole() === 'Manager');
  protected readonly isWarehouseStaff = computed(() => this.selectedRole() === 'WarehouseStaff');
  protected readonly suppliers = signal<readonly SupplierResponse[]>([]);
  protected readonly products = signal<readonly ProductResponse[]>([]);
  protected readonly lowStockProducts = signal<readonly ProductResponse[]>([]);
  protected readonly stockLevels = signal<readonly StockLevelResponse[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isSavingProduct = signal(false);
  protected readonly isAdjustingStock = signal(false);
  protected readonly loadError = signal<ApiErrorState | null>(null);
  protected readonly productSaveError = signal<ApiErrorState | null>(null);
  protected readonly adjustmentError = signal<ApiErrorState | null>(null);
  protected readonly editingProductId = signal<string | null>(null);

  protected readonly managerFiltersForm = this.formBuilder.nonNullable.group({
    q: '',
  });

  protected readonly stockFiltersForm = this.formBuilder.nonNullable.group({
    q: '',
  });

  protected readonly productForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required]],
    sku: ['', [Validators.required]],
    supplierId: ['', [Validators.required]],
    reorderThreshold: [0, [Validators.required, Validators.min(0)]],
    unitCostAmount: [0, [Validators.required, Validators.min(0.01)]],
  });

  protected readonly adjustmentForm = this.formBuilder.nonNullable.group({
    productId: ['', [Validators.required]],
    quantity: [0, [Validators.required, Validators.pattern(/^-?\d+$/)]],
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

      this.products.set([]);
      this.lowStockProducts.set([]);
      this.stockLevels.set([]);
      this.suppliers.set([]);
      this.loadError.set(null);
    });
  }

  protected loadManagerData(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const searchTerm = this.managerFiltersForm.controls.q.getRawValue().trim() || undefined;

    forkJoin({
      suppliers: this.suppliersApi.getSuppliers({ sort: 'name', order: 'asc' }),
      products: this.inventoryApi.getProducts({
        q: searchTerm,
        sort: 'name',
        order: 'asc',
      }),
      lowStock: this.inventoryApi.getLowStockProducts({
        q: searchTerm,
        sort: 'quantityOnHand',
        order: 'asc',
      }),
    })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: ({ suppliers, products, lowStock }) => {
          this.suppliers.set(suppliers);
          this.products.set(products);
          this.lowStockProducts.set(lowStock);
        },
        error: (error) => {
          this.loadError.set(toApiErrorState(error));
        },
      });
  }

  protected loadWarehouseData(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const searchTerm = this.stockFiltersForm.controls.q.getRawValue().trim() || undefined;

    this.inventoryApi
      .getStockLevels({
        q: searchTerm,
        sort: 'quantityOnHand',
        order: 'desc',
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

  protected submitProduct(): void {
    this.productForm.markAllAsTouched();
    this.productSaveError.set(null);

    if (this.productForm.invalid || !this.isManager()) {
      return;
    }

    this.isSavingProduct.set(true);

    const request = {
      sku: this.productForm.controls.sku.getRawValue().trim(),
      supplierId: this.productForm.controls.supplierId.getRawValue(),
      reorderThreshold: this.productForm.controls.reorderThreshold.getRawValue(),
      name: this.productForm.controls.name.getRawValue().trim(),
      unitCost: {
        amount: this.productForm.controls.unitCostAmount.getRawValue(),
        currency: 'GBP',
      },
    };

    const editingProductId = this.editingProductId();
    const operation = editingProductId
      ? this.inventoryApi.updateProduct(editingProductId, request)
      : this.inventoryApi.createProduct(request);

    operation
      .pipe(
        finalize(() => {
          this.isSavingProduct.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.resetProductForm();
          this.loadManagerData();
        },
        error: (error) => {
          this.productSaveError.set(toApiErrorState(error));
        },
      });
  }

  protected editProduct(product: ProductResponse): void {
    this.editingProductId.set(product.productId);
    this.productSaveError.set(null);
    this.productForm.reset({
      name: product.name,
      sku: product.sku,
      supplierId: product.supplierId,
      reorderThreshold: product.reorderThreshold,
      unitCostAmount: product.unitCost.amount,
    });
  }

  protected deleteProduct(product: ProductResponse): void {
    if (!this.isManager()) {
      return;
    }

    const confirmed = window.confirm(`Delete product "${product.name}"?`);

    if (!confirmed) {
      return;
    }

    this.inventoryApi.deleteProduct(product.productId).subscribe({
      next: () => {
        if (this.editingProductId() === product.productId) {
          this.resetProductForm();
        }

        this.loadManagerData();
      },
      error: (error) => {
        this.loadError.set(toApiErrorState(error));
      },
    });
  }

  protected submitAdjustment(): void {
    this.adjustmentForm.markAllAsTouched();
    this.adjustmentError.set(null);

    const quantity = Number(this.adjustmentForm.controls.quantity.getRawValue());

    if (quantity === 0) {
      this.adjustmentForm.controls.quantity.setErrors({ zero: true });
    }

    if (this.adjustmentForm.invalid || !this.isWarehouseStaff()) {
      return;
    }

    this.isAdjustingStock.set(true);

    this.inventoryApi
      .adjustStock(this.adjustmentForm.controls.productId.getRawValue(), {
        quantity,
        reason: this.adjustmentForm.controls.reason.getRawValue().trim(),
      })
      .pipe(
        finalize(() => {
          this.isAdjustingStock.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.adjustmentForm.reset({
            productId: '',
            quantity: 0,
            reason: '',
          });
          this.loadWarehouseData();
        },
        error: (error) => {
          this.adjustmentError.set(toApiErrorState(error));
        },
      });
  }

  protected resetProductForm(): void {
    this.editingProductId.set(null);
    this.productSaveError.set(null);
    this.productForm.reset({
      name: '',
      sku: '',
      supplierId: '',
      reorderThreshold: 0,
      unitCostAmount: 0,
    });
  }

  protected supplierName(supplierId: string): string {
    return (
      this.suppliers().find((supplier) => supplier.supplierId === supplierId)?.name ?? supplierId
    );
  }

  protected productStatus(product: ProductResponse): string {
    return product.quantityOnHand <= product.reorderThreshold ? 'Low stock' : 'Healthy';
  }

  protected productTone(product: ProductResponse) {
    return toStatusTone(this.productStatus(product));
  }

  protected stockTone(stockLevel: StockLevelResponse) {
    return toStatusTone(
      this.lowStockProducts().some((product) => product.productId === stockLevel.productId)
        ? 'Low stock'
        : 'Healthy',
    );
  }

  protected productControlErrors(
    controlName: 'name' | 'sku' | 'supplierId' | 'reorderThreshold' | 'unitCostAmount',
  ): readonly string[] {
    const control = this.productForm.controls[controlName];
    const errors: string[] = [];

    if (control.hasError('required')) {
      errors.push('This field is required.');
    }

    if (control.hasError('min')) {
      errors.push('Enter a value above the allowed minimum.');
    }

    return errors;
  }

  protected adjustmentControlErrors(
    controlName: 'productId' | 'quantity' | 'reason',
  ): readonly string[] {
    const control = this.adjustmentForm.controls[controlName];
    const errors: string[] = [];

    if (control.hasError('required')) {
      errors.push('This field is required.');
    }

    if (controlName === 'quantity' && control.hasError('pattern')) {
      errors.push('Quantity must be a whole number.');
    }

    if (controlName === 'quantity' && control.hasError('zero')) {
      errors.push('Quantity cannot be zero.');
    }

    return errors;
  }

  protected showProductControlError(
    controlName: 'name' | 'sku' | 'supplierId' | 'reorderThreshold' | 'unitCostAmount',
  ): boolean {
    const control = this.productForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected showAdjustmentControlError(controlName: 'productId' | 'quantity' | 'reason'): boolean {
    const control = this.adjustmentForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected productServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.productSaveError(), ...fieldNames);
  }

  protected adjustmentServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.adjustmentError(), ...fieldNames);
  }
}
