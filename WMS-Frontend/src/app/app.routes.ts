import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.page').then((module) => module.DashboardPage),
  },
  {
    path: 'suppliers',
    loadComponent: () =>
      import('./features/suppliers/suppliers.page').then((module) => module.SuppliersPage),
  },
  {
    path: 'inventory',
    loadComponent: () =>
      import('./features/inventory/inventory.page').then((module) => module.InventoryPage),
  },
  {
    path: 'purchase-orders',
    loadComponent: () =>
      import('./features/purchase-orders/purchase-orders.page').then(
        (module) => module.PurchaseOrdersPage,
      ),
  },
  {
    path: 'customer-orders',
    loadComponent: () =>
      import('./features/customer-orders/customer-orders.page').then(
        (module) => module.CustomerOrdersPage,
      ),
  },
  {
    path: 'finance-reports',
    loadComponent: () =>
      import('./features/finance-reports/finance-reports.page').then(
        (module) => module.FinanceReportsPage,
      ),
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
