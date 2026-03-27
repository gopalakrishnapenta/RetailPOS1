import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = 'http://localhost:5000/gateway/auth';
  private tokenKey = 'pos_token';
  private roleKey = 'pos_role';

  constructor(private http: HttpClient) {}

  login(credentials: any) {
    return this.http.post<any>(`${this.baseUrl}/login`, credentials).pipe(
      tap(res => {
        if (res && res.token) {
          localStorage.setItem(this.tokenKey, res.token);
          localStorage.setItem(this.roleKey, res.role);
          localStorage.setItem('pos_store_id', res.storeId.toString());
          localStorage.setItem('pos_user', JSON.stringify({ email: credentials.email, role: res.role, storeId: res.storeId }));
        }
      })
    );
  }

  get currentUserValue() {
    const user = localStorage.getItem('pos_user');
    return user ? JSON.parse(user) : null;
  }

  register(data: any) { return this.http.post<any>(`${this.baseUrl}/register`, data); }
  sendOtp(email: string) { return this.http.post<any>(`${this.baseUrl}/send-otp`, { email }); }
  resetPassword(data: any) { return this.http.post<any>(`${this.baseUrl}/reset-password`, data); }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
  }

  isAuthenticated(): boolean { return !!localStorage.getItem(this.tokenKey); }
  getRole(): string | null { return localStorage.getItem(this.roleKey); }
  getStores() { return this.http.get<any[]>(`${this.baseUrl}/stores`); }
}
