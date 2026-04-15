import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  // 1. Check Authentication First
  if (!authService.isLoggedIn()) {
    console.warn(`[Security] Unauthorized access attempt to ${state.url}. Redirecting to Login.`);
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // 2. Check Authorization (Roles) if required by route data
  const allowedRoles = route.data['roles'] as string[];
  if (allowedRoles && !authService.hasRole(allowedRoles)) {
    console.error(`[Security] User ${authService.user()?.email} denied access to ${state.url}. Required roles: ${allowedRoles}`);
    // Logged in but wrong role -> Send back to dashboard
    router.navigate(['/admin/dashboard']); 
    return false;
  }

  return true;
};
