import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  // 1. Check Authentication First
  if (!authService.isLoggedIn()) {
    console.warn(`[Security Guard] Blocked access to ${state.url} - User is not logged in. Redirecting to Login.`);
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // 2. Check Authorization (Roles) if required by route data
  const allowedRoles = route.data['roles'] as string[];
  if (allowedRoles && !authService.hasRole(allowedRoles)) {
    console.error(`[Security Guard] Access Denied for ${authService.user()?.email} to ${state.url}. Required roles: ${allowedRoles}. User role: ${authService.user()?.role}`);
    // Logged in but wrong role -> Send back to dashboard
    router.navigate(['/admin/dashboard']); 
    return false;
  }

  return true;
};
