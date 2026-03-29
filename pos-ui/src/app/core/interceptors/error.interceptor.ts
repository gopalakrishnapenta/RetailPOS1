import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error) {
        switch (error.status) {
          case 400:
            // Handle RFC 7807 (Problem Details)
            const detail = error.error?.detail || error.error?.message || 'Bad Request';
            notificationService.error(detail, 'Validation Error');
            break;
            
          case 401:
            notificationService.error('Session expired. Please login again.', 'Unauthorized');
            authService.logout();
            router.navigate(['/login']);
            break;
            
          case 403:
            notificationService.error('You do not have permission to perform this action.', 'Forbidden');
            break;
            
          case 404:
            const notFoundMsg = error.error?.detail || 'Resource not found';
            notificationService.error(notFoundMsg, 'Not Found');
            break;

          case 409:
            const conflictMsg = error.error?.detail || 'A conflict occurred';
            notificationService.error(conflictMsg, 'Conflict');
            break;
            
          case 422:
            const businessMsg = error.error?.detail || 'Business rule violation';
            notificationService.error(businessMsg, 'Process Error');
            break;

          case 500:
            notificationService.error('A server-side error occurred. Please contact support.', 'Server Error');
            break;

          case 0:
            notificationService.error('Cannot connect to the server. Please check your internet connection.', 'Network Error');
            break;

          default:
            notificationService.error('Something went wrong. Please try again later.', 'Error');
            console.error(error);
            break;
        }
      }
      return throwError(() => error);
    })
  );
};
