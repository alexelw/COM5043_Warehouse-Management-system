import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { RoleService } from '../services/role.service';

export const roleHeaderInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const roleService = inject(RoleService);

  return next(
    request.clone({
      setHeaders: {
        'X-Wms-Role': roleService.selectedRole(),
      },
    }),
  );
};
