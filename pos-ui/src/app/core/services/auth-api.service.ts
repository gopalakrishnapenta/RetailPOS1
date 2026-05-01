import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Store } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient) {}

  login(credentials: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/login`, credentials);
  }

  verifyLoginOtp(verifyDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/verify-login-otp`, verifyDto, { withCredentials: true });
  }

  resendLoginOtp(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/resend-login-otp`, { email });
  }

  register(registerDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, registerDto);
  }

  resendVerification(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/resend-verification`, { email });
  }

  googleLogin(googleDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/google-login`, googleDto, { withCredentials: true });
  }

  verifyEmail(verifyDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/verify-email`, verifyDto);
  }

  sendPasswordResetOtp(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/send-otp`, { email });
  }

  resetPassword(resetDto: { email: string; otp: string; newPassword: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/reset-password`, resetDto);
  }

  getStores(): Observable<Store[]> {
    return this.http.get<Store[]>(`${this.baseUrl}/stores`);
  }

  getUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/users`);
  }

  refresh(accessToken: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/refresh`, { accessToken }, { withCredentials: true });
  }

  logout(): Observable<any> {
    return this.http.post(`${this.baseUrl}/logout`, {}, { withCredentials: true });
  }
}
