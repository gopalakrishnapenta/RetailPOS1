import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  private http = inject(HttpClient);
  private router = inject(Router);

  constructor() {
    this.loadUserFromStorage();
  }

  login(credentials: any) {
    return this.http.post<any>('http://localhost:5000/gateway/auth/login', credentials).pipe(
      tap((response: any) => {
        if (response.data && response.data.token) {
          this.setSession(response.data);
        }
      })
    );
  }

  private setSession(authData: User) {
    localStorage.setItem('token', authData.token || '');
    localStorage.setItem('user', JSON.stringify(authData));
    this._user.set(authData);
  }

  private loadUserFromStorage() {
    const data = localStorage.getItem('user');
    if (data) {
      this._user.set(JSON.parse(data));
    }
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this._user.set(null);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return !!this._user();
  }

  hasPermission(permission: string): boolean {
    return this._user()?.permissions.includes(permission) || false;
  }
}
