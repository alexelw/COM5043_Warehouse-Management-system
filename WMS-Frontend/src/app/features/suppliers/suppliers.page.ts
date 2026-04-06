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
import { ApiErrorState, SupplierResponse } from '../../core/models/api.types';
import { RoleService } from '../../core/services/role.service';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { SuppliersApiService } from './data/suppliers.api';

const phonePattern = /^[0-9+()\-\s]*$/;

function supplierContactValidator(control: AbstractControl): ValidationErrors | null {
  const email = control.get('email')?.value as string | null | undefined;
  const phone = control.get('phone')?.value as string | null | undefined;
  const address = control.get('address')?.value as string | null | undefined;

  return email || phone || address ? null : { contactRequired: true };
}

@Component({
  selector: 'app-suppliers-page',
  imports: [
    ReactiveFormsModule,
    EmptyStateComponent,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
  ],
  templateUrl: './suppliers.page.html',
  styleUrl: './suppliers.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuppliersPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly roleService = inject(RoleService);
  private readonly suppliersApi = inject(SuppliersApiService);

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly isManager = computed(() => this.selectedRole() === 'Manager');
  protected readonly suppliers = signal<readonly SupplierResponse[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly listError = signal<ApiErrorState | null>(null);
  protected readonly saveError = signal<ApiErrorState | null>(null);
  protected readonly editingSupplierId = signal<string | null>(null);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    q: '',
    sort: 'name',
    order: 'asc',
  });

  protected readonly supplierForm = this.formBuilder.group(
    {
      name: this.formBuilder.nonNullable.control('', [Validators.required]),
      email: this.formBuilder.control('', [Validators.email]),
      phone: this.formBuilder.control('', [Validators.pattern(phonePattern)]),
      address: this.formBuilder.control(''),
    },
    { validators: supplierContactValidator },
  );

  constructor() {
    effect(() => {
      if (this.isManager()) {
        this.loadSuppliers();
        return;
      }

      this.suppliers.set([]);
      this.listError.set(null);
      this.saveError.set(null);
    });
  }

  protected loadSuppliers(): void {
    if (!this.isManager()) {
      return;
    }

    this.isLoading.set(true);
    this.listError.set(null);

    const filterValues = this.filtersForm.getRawValue();

    this.suppliersApi
      .getSuppliers({
        q: filterValues.q.trim() || undefined,
        sort: filterValues.sort,
        order: filterValues.order,
      })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
      )
      .subscribe({
        next: (suppliers) => {
          this.suppliers.set(suppliers);
        },
        error: (error) => {
          this.listError.set(toApiErrorState(error));
        },
      });
  }

  protected submitSupplier(): void {
    this.supplierForm.markAllAsTouched();
    this.saveError.set(null);

    if (this.supplierForm.invalid || !this.isManager()) {
      return;
    }

    this.isSaving.set(true);

    const request = {
      name: this.supplierForm.controls.name.getRawValue().trim(),
      email: this.normalizeOptionalValue(this.supplierForm.controls.email.getRawValue()),
      phone: this.normalizeOptionalValue(this.supplierForm.controls.phone.getRawValue()),
      address: this.normalizeOptionalValue(this.supplierForm.controls.address.getRawValue()),
    };

    const editingSupplierId = this.editingSupplierId();
    const operation = editingSupplierId
      ? this.suppliersApi.updateSupplier(editingSupplierId, request)
      : this.suppliersApi.createSupplier(request);

    operation
      .pipe(
        finalize(() => {
          this.isSaving.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.resetSupplierForm();
          this.loadSuppliers();
        },
        error: (error) => {
          this.saveError.set(toApiErrorState(error));
        },
      });
  }

  protected editSupplier(supplier: SupplierResponse): void {
    this.editingSupplierId.set(supplier.supplierId);
    this.saveError.set(null);
    this.supplierForm.reset({
      name: supplier.name,
      email: supplier.email ?? '',
      phone: supplier.phone ?? '',
      address: supplier.address ?? '',
    });
  }

  protected deleteSupplier(supplier: SupplierResponse): void {
    if (!this.isManager()) {
      return;
    }

    const confirmed = globalThis.confirm?.(`Delete supplier "${supplier.name}"?`) ?? false;

    if (!confirmed) {
      return;
    }

    this.listError.set(null);

    this.suppliersApi.deleteSupplier(supplier.supplierId).subscribe({
      next: () => {
        if (this.editingSupplierId() === supplier.supplierId) {
          this.resetSupplierForm();
        }

        this.loadSuppliers();
      },
      error: (error) => {
        this.listError.set(toApiErrorState(error));
      },
    });
  }

  protected resetSupplierForm(): void {
    this.editingSupplierId.set(null);
    this.saveError.set(null);
    this.supplierForm.reset({
      name: '',
      email: '',
      phone: '',
      address: '',
    });
  }

  protected getControlErrors(controlName: 'name' | 'email' | 'phone'): readonly string[] {
    const control = this.supplierForm.controls[controlName];
    const errors: string[] = [];

    if (control.hasError('required')) {
      errors.push('This field is required.');
    }

    if (controlName === 'email' && control.hasError('email')) {
      errors.push('Enter a valid email address.');
    }

    if (controlName === 'phone' && control.hasError('pattern')) {
      errors.push('Use digits, spaces, and common phone symbols only.');
    }

    return errors;
  }

  protected getServerErrors(...fieldNames: readonly string[]): readonly string[] {
    return getFieldErrors(this.saveError(), ...fieldNames);
  }

  protected hasControlError(controlName: 'name' | 'email' | 'phone'): boolean {
    const control = this.supplierForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected showContactError(): boolean {
    return Boolean(
      this.supplierForm.hasError('contactRequired') &&
      (this.supplierForm.touched || this.supplierForm.dirty),
    );
  }

  protected supplierContact(supplier: SupplierResponse): string {
    return supplier.email ?? supplier.address ?? 'No email or address';
  }

  private normalizeOptionalValue(value: string | null): string | null {
    return value?.trim() || null;
  }
}
