import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Router } from '@angular/router';

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

  constructor() {
    this.loadUserFromStorage();
  }

  login(credentials: any) {
    return this.http.post<any>('http://localhost:5000/gateway/auth/login', credentials).pipe(
      tap((response: any) => {
        if (response.data && response.data.token) {
          const authData: User = {
            ...response.data,
            role: response.data.role || 'Staff'
          };
          this.setSession(authData, response.refreshToken);
        }
      })
    );
  }

  googleLogin(googleData: any) {
    return this.http.post<any>('http://localhost:5000/gateway/auth/google-login', googleData).pipe(
      tap((response: any) => {
        if (response.data && response.data.token) {
          const authData: User = {
            ...response.data,
            role: response.data.role || 'Staff'
          };
          this.setSession(authData, response.refreshToken);
        }
      })
    );
  }

  refreshToken(): Observable<any> {
    const token = localStorage.getItem('token');
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<any>('http://localhost:5000/gateway/auth/refresh', {
      accessToken: token,
      refreshToken: refreshToken
    }).pipe(
      tap((response: any) => {
        if (response.data && response.data.token) {
          const authData: User = {
            ...response.data,
            role: response.data.role || 'Staff'
          };
          this.setSession(authData, response.refreshToken);
        }
      })
    );
  }

  private setSession(authData: User, refreshToken?: string) {
    localStorage.setItem('token', authData.token || '');
    if (refreshToken) {
      localStorage.setItem('refreshToken', refreshToken);
    }
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
