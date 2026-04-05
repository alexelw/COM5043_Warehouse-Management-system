import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toApiErrorState } from '../../core/http/api-helpers';
import { HealthResponse } from '../../core/models/api.types';
import { RoleService } from '../../core/services/role.service';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state';
import { ErrorBannerComponent } from '../../shared/ui/error-banner/error-banner';
import { LoadingStateComponent } from '../../shared/ui/loading-state/loading-state';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header';
import { StatusBadgeComponent } from '../../shared/ui/status-badge/status-badge';
import { DashboardApiService } from './data/dashboard.api';

interface QuickLink {
  readonly title: string;
  readonly description: string;
  readonly route: string;
  readonly label: string;
  readonly tone: 'info' | 'success' | 'warning';
}

@Component({
  selector: 'app-dashboard-page',
  imports: [
    RouterLink,
    EmptyStateComponent,
    ErrorBannerComponent,
    LoadingStateComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
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
      description: 'Manage supplier contact details and keep purchasing records tidy.',
      route: '/suppliers',
      label: 'Manager flow',
      tone: 'info',
    },
    {
      title: 'Inventory',
      description: 'Review stock levels, spot shortages, and record stock adjustments.',
      route: '/inventory',
      label: 'Warehouse flow',
      tone: 'success',
    },
    {
      title: 'Finance',
      description: 'Review transactions, generate summaries, and track report exports.',
      route: '/finance-reports',
      label: 'Administrator flow',
      tone: 'warning',
    },
  ];

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly foundationItems: readonly string[] = [
    'Role selection is applied automatically to API requests.',
    'Each feature page uses reactive forms with inline validation.',
    'Tables and forms share consistent error, loading, and empty states.',
    'Supplier, inventory, order, and finance routes are all available from one shell.',
    'Frontend quality checks run through format, lint, build, and test scripts.',
  ];

  protected readonly roleHighlights: Record<string, readonly string[]> = {
    Manager: [
      'Maintain suppliers and products.',
      'Create and review purchase orders.',
      'Track low stock and order history.',
    ],
    WarehouseStaff: [
      'Review live stock levels.',
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
