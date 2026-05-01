import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthApiService } from './auth-api.service';

export interface User {
  id?: number;
  email: string;
  role: string;
  storeId: number;
  token?: string;
  permissions: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private _user = signal<User | null>(null);
  readonly user = this._user.asReadonly();
  readonly isAuth = signal(false);

  private http = inject(HttpClient);
  private router = inject(Router);
  private authApi = inject(AuthApiService);

  constructor() {
    this.loadUserFromStorage();
  }

  login(credentials: any) {
    return this.authApi.login(credentials).pipe(
      tap((response: any) => {
        if (response.data && response.data.token) {
          const authData: User = {
            ...response.data,
            role: response.data.role || 'Staff'
          };
          this.setSession(authData);
        }
      })
    );
  }

  googleLogin(googleData: any) {
    return this.authApi.googleLogin(googleData).pipe(
      tap((response: any) => {
        if (response.data && response.data.token) {
          const authData: User = {
            ...response.data,
            role: response.data.role || 'Staff'
          };
          this.setSession(authData);
        }
      })
    );
  }

  refreshToken(): Observable<any> {
    const token = localStorage.getItem('token');
    // Note: refreshToken is handled via HttpOnly cookie in the backend
    return this.authApi.refresh(token || '').pipe(
      tap((response: any) => {
        if (response && response.token) {
          const authData: User = {
            ...response,
            role: response.role || 'Staff'
          };
          this.setSession(authData);
        }
      })
    );
  }

  private setSession(authData: User) {
    localStorage.setItem('token', authData.token || '');
    localStorage.setItem('user', JSON.stringify(authData));
    this._user.set(authData);
    this.isAuth.set(true);
  }

  private loadUserFromStorage() {
    const data = localStorage.getItem('user');
    const token = localStorage.getItem('token');
    if (data && token) {
      this._user.set(JSON.parse(data));
      this.isAuth.set(true);
    } else {
      this.isAuth.set(false);
    }
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this._user.set(null);
    this.isAuth.set(false);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return this.isAuth();
  }

  hasRole(allowedRoles: string[]): boolean {
    const user = this._user();
    if (!user) return false;
    return allowedRoles.includes(user.role);
  }

  hasPermission(permission: string): boolean {
    return this._user()?.permissions.includes(permission) || false;
  }
}
