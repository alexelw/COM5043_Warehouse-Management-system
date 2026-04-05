import { TestBed } from '@angular/core/testing';
import { RoleService } from './role.service';

describe('RoleService', () => {
  beforeEach(() => {
    localStorage.removeItem('wms.selected-role');
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    localStorage.removeItem('wms.selected-role');
  });

  it('should default to the manager role', () => {
    const service = TestBed.inject(RoleService);

    expect(service.selectedRole()).toBe('Manager');
  });

  it('should update the selected role', () => {
    const service = TestBed.inject(RoleService);

    service.setSelectedRole('Administrator');

    expect(service.selectedRole()).toBe('Administrator');
  });
});
