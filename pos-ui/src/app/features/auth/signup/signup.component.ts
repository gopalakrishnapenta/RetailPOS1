import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
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
          <input type="email" [(ngModel)]="data.email" name="email" required placeholder="name@gmail.com"
            pattern="^[a-zA-Z0-9.]+@gmail\.com$">
          <small *ngIf="form.controls['email']?.errors?.['pattern']" class="error-msg">Must be a &#64;gmail.com address</small>
        </div>
        <div class="form-group">
          <label>Password</label>
          <div class="password-wrapper">
            <input [type]="showPassword ? 'text' : 'password'" [(ngModel)]="data.password" name="password" required placeholder="Create a secure password"
              pattern="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$">
            <span class="toggle-password" (click)="showPassword = !showPassword">
              {{ showPassword ? '👁️' : '👁️‍🗨️' }}
            </span>
          </div>
          <small *ngIf="form.controls['password']?.errors?.['pattern']" class="error-msg" style="font-size: 0.7rem;">
            8+ chars, Uppercase, Lowercase, Number, Symbol required
          </small>
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

  private auth = inject(AuthService);
  private router = inject(Router);
  private socialAuth = inject(SocialAuthService);
  private notification = inject(NotificationService);

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
    this.msg = '';
    // Pass selected store and role for first-time Google signups
    this.auth.googleLogin(idToken, this.data.storeId, this.data.role).subscribe({
      next: (res) => {
        this.isLoading = false;
        localStorage.setItem('storeContext', JSON.stringify({
          storeId: res.storeId,
          storeCode: res.storeCode,
          shiftDate: new Date().toISOString().split('T')[0]
        }));
        this.notification.success('Welcome to RetailPOS!');
        this.router.navigate(['/pos/billing']);
      },
      error: (err: any) => {
        this.isLoading = false;
        const msg = err.error?.detail || err.error?.message;
        if (msg === 'GOOGLE_SIGNUP_REQUIRED_FIELDS' || err.status === 422) {
          this.msg = 'Please select a Store and Role first, then click Google Sign-in again.';
        } else {
          this.msg = msg || 'Google login failed';
        }
        this.isErr = true;
      }
    });
  }

  loadStores() {
    this.auth.getStores().subscribe({
      next: (res) => {
        this.stores = res;
      },
      error: (err: any) => {
        console.error('Failed to load stores', err);
        this.notification.error('Check your internet or server connection.', 'Store Load Failed');
      }
    });
  }

  onRegister() {
    this.isLoading = true;
    this.msg = '';
    this.auth.register(this.data).subscribe({
      next: () => {
        this.isLoading = false;
        this.notification.success('Registration successful! Redirecting...');
        this.msg = 'Registration successful! Check your email for OTP.';
        this.isErr = false;
        setTimeout(() => this.router.navigate(['/auth/verify-email'], { queryParams: { email: this.data.email } }), 2000);
      },
      error: (err: any) => {
        this.isLoading = false;
        const msg = err.error?.detail || err.error?.message;
        this.msg = msg || 'Registration failed';
        this.isErr = true;
      }
    });
  }
}
