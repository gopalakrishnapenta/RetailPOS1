import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');

  if (token) {
    // Basic check: is there a token? 
    // In a full production app, you might also decode it to check expiration.
    return true;
  }

  // Not logged in -> redirect to login with return url
  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
