import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { GoogleSigninButtonModule, SocialAuthService } from '@abacritt/angularx-social-login';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, GoogleSigninButtonModule],
  template: `
    <div class="login-container premium-card fade-in">
      <div class="login-header">
        <h2>Create Account</h2>
        <p>Join NexusPOS today</p>
      </div>
      
      <form (ngSubmit)="onRegister()" #form="ngForm">
        <div class="form-group">
          <label>Email Address</label>
          <input type="email" [(ngModel)]="data.email" name="email" required placeholder="name@gmail.com">
        </div>
        <div class="form-group">
          <label>Password</label>
          <div class="password-wrapper">
            <input [type]="showPassword ? 'text' : 'password'" [(ngModel)]="data.password" name="password" required placeholder="Create a secure password">
            <span class="toggle-password" (click)="showPassword = !showPassword">
              {{ showPassword ? '👁️' : '👁️‍🗨️' }}
            </span>
          </div>
        </div>
        <div class="form-group">
          <label>Select Store</label>
          <select [(ngModel)]="data.storeId" name="storeId" required class="form-control">
            <option [value]="null" disabled selected>-- Select Store --</option>
            <option *ngFor="let s of stores" [value]="s.id">{{ s.name }} ({{ s.storeCode }})</option>
          </select>
        </div>
        <div class="form-group">
          <label>Role</label>
          <select [(ngModel)]="data.role" name="role" required class="form-control">
            <option value="Cashier">Cashier</option>
            <option value="StoreManager">Store Manager</option>
          </select>
        </div>
        
        <div *ngIf="msg" [ngClass]="isErr ? 'error-msg' : 'success-msg'">{{ msg }}</div>
        
        <button type="submit" [disabled]="!form.valid || isLoading" class="btn btn-primary btn-block" style="margin-top: 1.5rem">
          {{ isLoading ? 'Registering...' : 'Register' }}
        </button>
      </form>

      <div class="divider"><span>OR</span></div>
      
      <div class="social-login">
        <asl-google-signin-button type="standard" size="large" [width]="240"></asl-google-signin-button>
      </div>

      <div class="auth-footer">Already have an account? <a routerLink="/auth/login" class="text-link" style="font-weight: 600;">Sign in</a></div>
    </div>
  `
})
export class SignupComponent implements OnInit {
  data = { email: '', password: '', role: 'Cashier', storeId: null as any };
  stores: any[] = [];
  msg = ''; isErr = false; isLoading = false; showPassword = false;

  constructor(private auth: AuthService, private router: Router, private socialAuth: SocialAuthService) { }

  ngOnInit() {
    this.loadStores();
    this.socialAuth.authState.subscribe((user) => {
      if (user && user.idToken) {
        this.onGoogleLogin(user.idToken);
      }
    });
  }

  onGoogleLogin(idToken: string) {
    this.isLoading = true;
    this.auth.googleLogin(idToken).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.msg = err.error?.message || 'Google login failed';
        this.isErr = true;
      }
    });
  }

  loadStores() {
    this.auth.getStores().subscribe({
      next: (res) => {
        this.stores = res;
        if (this.stores.length > 0) {
          // No auto-selection, user must pick
        }
      },
      error: (err) => {
        console.error('Failed to load stores', err);
        this.msg = 'Failed to load stores. Please check connection.';
        this.isErr = true;
      }
    });
  }

  onRegister() {
    this.isLoading = true;
    this.msg = '';
    this.auth.register(this.data).subscribe({
      next: () => {
        this.isLoading = false;
        this.msg = 'Registration successful! Redirecting...';
        this.isErr = false;
        setTimeout(() => this.router.navigate(['/auth/login']), 1500);
      },
      error: (err) => {
        this.isLoading = false;
        this.msg = err.error?.message || 'Registration failed';
        this.isErr = true;
      }
    });
  }
}
