import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-otp-verification',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-container">
      <div class="auth-card glass-panel">
        <div class="theme-toggle-header">
          <div class="theme-switch" (click)="toggleTheme()" [class.dark]="theme === 'dark'">
            <div class="toggle-track">
              <div class="icon sun">☀️</div>
              <div class="icon moon">🌙</div>
              <div class="toggle-thumb"></div>
            </div>
          </div>
        </div>
        <div class="auth-header">
          <div class="logo">POS</div>
          <h1>Verify Identity</h1>
          <p>We've sent a 6-digit code to <strong>{{ email }}</strong></p>
        </div>

        <form (ngSubmit)="verifyOtp()" #otpForm="ngForm">
          <div class="form-group">
            <label>Verification Code</label>
            <div class="otp-input-group">
              <input 
                type="text" 
                [(ngModel)]="otp" 
                name="otp" 
                placeholder="000000" 
                maxlength="6" 
                required
                autocomplete="one-time-code">
            </div>
            <small class="helper-text">Enter the 6-digit code from your email</small>
          </div>

          <button type="submit" class="btn btn-primary btn-block" [disabled]="otp.length !== 6 || isProcessing">
            {{ isProcessing ? 'Verifying...' : 'Complete Sign In' }}
          </button>
        </form>

        <div class="auth-footer">
          <p>Didn't receive the code? 
            <button class="link-btn" (click)="resendOtp()" [disabled]="resendCooldown > 0">
              {{ resendCooldown > 0 ? 'Resend in ' + resendCooldown + 's' : 'Resend Code' }}
            </button>
          </p>
          <a routerLink="/login" class="back-link">← Back to Login</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-primary);
      font-family: 'Inter', sans-serif;
    }
    .auth-card {
      width: 100%;
      max-width: 440px;
      padding: 48px;
      border-radius: 24px;
      background: var(--panel-bg);
      backdrop-filter: blur(18px);
      border: 1px solid var(--border-color);
      box-shadow: var(--shadow-lg);
      text-align: center;
      color: var(--text-primary);
      position: relative;
    }
    .theme-toggle-header {
      position: absolute;
      top: 24px;
      right: 24px;
    }
    
    .logo {
      width: 56px;
      height: 56px;
      background: var(--accent-primary);
      color: white;
      border-radius: 14px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 800;
      font-size: 16px;
      margin: 0 auto 24px;
      box-shadow: 0 8px 16px rgba(37, 99, 235, 0.2);
    }
    h1 { font-size: 24px; margin-bottom: 8px; font-weight: 800; color: var(--text-primary); }
    p { color: var(--text-secondary); font-size: 14px; margin-bottom: 32px; }
    strong { color: var(--accent-primary); }
    .form-group { text-align: left; margin-bottom: 24px; }
    label { display: block; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 8px; color: var(--text-muted); }
    input {
      width: 100%;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 16px;
      color: var(--text-primary);
      font-size: 24px;
      text-align: center;
      letter-spacing: 0.4em;
      font-weight: 700;
      transition: all 0.2s;
    }
    input:focus { border-color: var(--accent-primary); outline: none; background: var(--bg-tertiary); box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.1); }
    .helper-text { display: block; margin-top: 8px; font-size: 12px; color: var(--text-muted); }
    .btn-block { width: 100%; padding: 16px; border-radius: 12px; font-weight: 700; font-size: 16px; border: none; cursor: pointer; transition: all 0.2s; }
    .btn-primary { background: var(--accent-primary); color: white; }
    .btn-primary:hover:not(:disabled) { background: var(--accent-secondary); transform: translateY(-1px); }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .auth-footer { margin-top: 32px; font-size: 14px; }
    .link-btn { background: none; border: none; color: var(--accent-primary); font-weight: 700; cursor: pointer; padding: 0; }
    .link-btn:disabled { color: var(--text-muted); cursor: not-allowed; }
    .back-link { display: block; margin-top: 16px; color: var(--text-muted); text-decoration: none; transition: color 0.2s; font-weight: 600; }
    .back-link:hover { color: var(--text-primary); }
  `]
})
export class OtpVerificationComponent implements OnInit {
  email: string = '';
  otp: string = '';
  isProcessing = false;
  resendCooldown = 0;
  theme: 'light' | 'dark' = 'light';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.email = this.route.snapshot.queryParamMap.get('email') || '';
    if (!this.email) {
      this.router.navigate(['/login']);
    }
    const saved = localStorage.getItem('theme');
    if (saved === 'light' || saved === 'dark') this.theme = saved;
    document.documentElement.setAttribute('data-theme', this.theme);
  }

  toggleTheme() {
    this.theme = this.theme === 'dark' ? 'light' : 'dark';
    localStorage.setItem('theme', this.theme);
    document.documentElement.setAttribute('data-theme', this.theme);
  }

  verifyOtp() {
    this.isProcessing = true;
    this.authService.verifyLoginOtp({ email: this.email, otp: this.otp }).subscribe({
      next: (res: any) => {
        const data = res.data || res;
        const rawRole = data.role || data.Role || '';
        const role = rawRole.toLowerCase();

        console.log('[OTP Verification] Success. Role:', rawRole);

        if (role.includes('admin') || role.includes('storemanager')) {
          this.router.navigate(['/admin/dashboard']);
        } else if (role.includes('pending')) {
          this.router.navigate(['/pending-approval']);
        } else {
          // Default for Staff, Cashier, etc.
          this.router.navigate(['/pos/billing']);
        }
      },
      error: (err) => {
        alert(err.error?.message || 'Invalid verification code');
        this.isProcessing = false;
      }
    });
  }

  resendOtp() {
    this.resendCooldown = 30;
    const timer = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0) clearInterval(timer);
    }, 1000);

    this.api.resendLoginOtp(this.email).subscribe({
      next: () => alert('New code sent!'),
      error: (err) => alert('Failed to resend code')
    });
  }
}
