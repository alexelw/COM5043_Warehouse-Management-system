import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toApiErrorState } from '../../core/http/api-helpers';
import { HealthResponse } from '../../core/models/api.types';
import { UserRole } from '../../core/models/user-role.type';
import { RoleService } from '../../core/services/role.service';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { DashboardApiService } from './data/dashboard.api';

interface QuickLink {
  readonly title: string;
  readonly route: string;
  readonly roleLabel: string;
  readonly roles: readonly UserRole[];
}

interface DashboardAction {
  readonly label: string;
  readonly route: string;
}

@Component({
  selector: 'app-dashboard-page',
  imports: [
    RouterLink,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly dashboardApi = inject(DashboardApiService);
  private readonly roleService = inject(RoleService);

  protected readonly quickLinks: readonly QuickLink[] = [
    {
      title: 'Suppliers',
      route: '/suppliers',
      roleLabel: 'Manager',
      roles: ['Manager'],
    },
    {
      title: 'Inventory',
      route: '/inventory',
      roleLabel: 'Manager / Warehouse Staff',
      roles: ['Manager', 'WarehouseStaff'],
    },
    {
      title: 'Purchase Orders',
      route: '/purchase-orders',
      roleLabel: 'Manager / Warehouse Staff',
      roles: ['Manager', 'WarehouseStaff'],
    },
    {
      title: 'Customer Orders',
      route: '/customer-orders',
      roleLabel: 'Manager / Warehouse Staff',
      roles: ['Manager', 'WarehouseStaff'],
    },
    {
      title: 'Finance',
      route: '/finance-reports',
      roleLabel: 'Administrator',
      roles: ['Administrator'],
    },
  ];

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly visibleQuickLinks = computed(() =>
    this.quickLinks.filter((item) => item.roles.includes(this.selectedRole())),
  );
  protected readonly primaryAction = computed<DashboardAction>(() => {
    switch (this.selectedRole()) {
      case 'WarehouseStaff':
        return { label: 'Purchase Orders', route: '/purchase-orders' };
      case 'Administrator':
        return { label: 'Finance', route: '/finance-reports' };
      default:
        return { label: 'Suppliers', route: '/suppliers' };
    }
  });

  protected readonly roleHighlights: Record<string, readonly string[]> = {
    Manager: [
      'Maintain suppliers and products.',
      'Create and review purchase orders.',
      'Track low stock and order history.',
    ],
    WarehouseStaff: [
      'Review live stock levels.',
      'Receive purchase-order deliveries.',
      'Record stock adjustments.',
      'Create and cancel customer orders.',
    ],
    Administrator: [
      'Review financial transactions.',
      'Generate financial summaries.',
      'Export reporting history.',
    ],
  };

  protected readonly health = signal<HealthResponse | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly error = signal<ReturnType<typeof toApiErrorState> | null>(null);

  constructor() {
    effect(() => {
      this.selectedRole();
      this.loadHealth();
    });
  }

  protected loadHealth(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.dashboardApi.getHealth().subscribe({
      next: (health) => {
        this.health.set(health);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.error.set(toApiErrorState(error));
        this.isLoading.set(false);
      },
    });
  }
}
