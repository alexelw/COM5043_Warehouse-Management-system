import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RoleService } from '../../services/role.service';
import { UserRole } from '../../models/user-role.type';

@Component({
  selector: 'app-role-selector',
  templateUrl: './role-selector.html',
  styleUrl: './role-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleSelectorComponent {
  private readonly roleService = inject(RoleService);

  protected readonly roles = this.roleService.roles;
  protected readonly selectedRole = this.roleService.selectedRole;

  protected updateRole(role: string): void {
    this.roleService.setSelectedRole(role as UserRole);
  }
}
