import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const user = authService.currentUser();

  if (!user || !authService.hasToken()) {
    return router.createUrlTree(['/login']);
  }

  const allowedRoles:string[] = route.data?.['roles'] as string[];
  if (allowedRoles && allowedRoles.includes(user.role)) {
    return true;
  }

  return false;
};
