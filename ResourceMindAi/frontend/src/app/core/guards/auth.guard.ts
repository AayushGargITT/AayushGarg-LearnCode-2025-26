import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // In a real app we'd check tokens. For now, check if currentUser signal has value
  if (authService.currentUser()) {
    return true;
  }

  // Allow access to demo but normally redirect to /login
  // return router.createUrlTree(['/login']);
  return true; // Bypassing for easy UI development
};
