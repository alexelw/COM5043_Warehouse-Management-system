import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { UserRole } from '../../models/user-role.type';
import { RoleService } from '../../services/role.service';
import { RoleSelectorComponent } from '../role-selector/role-selector';

interface NavItem {
  readonly label: string;
  readonly path: string;
  readonly roles: readonly UserRole[];
}

@Component({
  selector: 'app-top-nav',
  imports: [RouterLink, RouterLinkActive, RoleSelectorComponent],
  templateUrl: './top-nav.html',
  styleUrl: './top-nav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopNavComponent {
  private readonly roleService = inject(RoleService);

  protected readonly selectedRole = this.roleService.selectedRole;
  protected readonly navItems: readonly NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', roles: ['WarehouseStaff', 'Manager', 'Administrator'] },
    { label: 'Suppliers', path: '/suppliers', roles: ['Manager'] },
    { label: 'Inventory', path: '/inventory', roles: ['WarehouseStaff', 'Manager'] },
    { label: 'Purchase Orders', path: '/purchase-orders', roles: ['WarehouseStaff', 'Manager'] },
    { label: 'Customer Orders', path: '/customer-orders', roles: ['WarehouseStaff', 'Manager'] },
    { label: 'Finance', path: '/finance-reports', roles: ['Administrator'] },
  ];
  protected readonly visibleNavItems = computed(() =>
    this.navItems.filter((item) => item.roles.includes(this.selectedRole())),
  );
}
