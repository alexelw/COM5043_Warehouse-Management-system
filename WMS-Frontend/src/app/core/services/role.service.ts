import { Injectable, signal } from '@angular/core';
import { USER_ROLES, UserRole } from '../models/user-role.type';

@Injectable({
  providedIn: 'root',
})
export class RoleService {
  private readonly storageKey = 'wms.selected-role';
  private readonly selectedRoleState = signal<UserRole>(this.readInitialRole());

  readonly roles = USER_ROLES;
  readonly selectedRole = this.selectedRoleState.asReadonly();

  setSelectedRole(role: UserRole): void {
    this.selectedRoleState.set(role);
    this.persistRole(role);
  }

  private readInitialRole(): UserRole {
    if (typeof localStorage === 'undefined') {
      return 'Manager';
    }

    const storedRole = localStorage.getItem(this.storageKey);

    return USER_ROLES.includes(storedRole as UserRole) ? (storedRole as UserRole) : 'Manager';
  }

  private persistRole(role: UserRole): void {
    if (typeof localStorage === 'undefined') {
      return;
    }

    localStorage.setItem(this.storageKey, role);
  }
}
