import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { RoleService } from '../../services/role.service';
import { RoleSelectorComponent } from '../role-selector/role-selector';

interface NavItem {
  readonly label: string;
  readonly path: string;
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
    { label: 'Dashboard', path: '/dashboard' },
    { label: 'Suppliers', path: '/suppliers' },
    { label: 'Inventory', path: '/inventory' },
    { label: 'Purchase Orders', path: '/purchase-orders' },
    { label: 'Customer Orders', path: '/customer-orders' },
    { label: 'Finance', path: '/finance-reports' },
  ];
}
