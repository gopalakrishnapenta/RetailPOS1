import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-otp-verification',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-container">
      <div class="auth-card glass-panel">
        <div class="theme-toggle">
          <button type="button" class="theme-btn" (click)="setTheme('light')" [class.active]="theme === 'light'">Light</button>
          <button type="button" class="theme-btn" (click)="setTheme('dark')" [class.active]="theme === 'dark'">Dark</button>
        </div>
        <div class="auth-header">
          <div class="logo">N</div>
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
      max-width: 400px;
      padding: 40px;
      border-radius: 24px;
      background: var(--panel-bg);
      backdrop-filter: blur(18px);
      border: 1px solid var(--border-color);
      box-shadow: var(--shadow-lg);
      text-align: center;
      color: var(--text-primary);
    }
    .theme-toggle {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-bottom: 16px;
    }
    .theme-btn {
      border: 1px solid var(--border-color);
      background: var(--bg-secondary);
      color: var(--text-secondary);
      padding: 6px 12px;
      border-radius: 999px;
      font-size: 12px;
      cursor: pointer;
      transition: all 0.2s;
    }
    .theme-btn.active {
      background: #3b82f6;
      color: white;
      border-color: #3b82f6;
    }
    .logo {
      width: 48px;
      height: 48px;
      background: #3b82f6;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 800;
      font-size: 24px;
      margin: 0 auto 24px;
      box-shadow: 0 0 20px rgba(59, 130, 246, 0.4);
    }
    h1 { font-size: 24px; margin-bottom: 8px; font-weight: 700; }
    p { color: var(--text-secondary); font-size: 14px; margin-bottom: 32px; }
    strong { color: #3b82f6; }
    .form-group { text-align: left; margin-bottom: 24px; }
    label { display: block; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 8px; color: var(--text-muted); }
    input {
      width: 100%;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 14px;
      color: var(--text-primary);
      font-size: 20px;
      text-align: center;
      letter-spacing: 0.5em;
      transition: all 0.2s;
    }
    input:focus { border-color: #3b82f6; outline: none; background: var(--bg-tertiary); box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.1); }
    .helper-text { display: block; margin-top: 8px; font-size: 12px; color: var(--text-muted); }
    .btn-block { width: 100%; padding: 14px; border-radius: 12px; font-weight: 600; font-size: 16px; border: none; cursor: pointer; transition: all 0.2s; }
    .btn-primary { background: #3b82f6; color: white; }
    .btn-primary:hover { background: #2563eb; transform: translateY(-1px); }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
    .auth-footer { margin-top: 32px; font-size: 14px; }
    .link-btn { background: none; border: none; color: #3b82f6; font-weight: 600; cursor: pointer; padding: 0; }
    .link-btn:disabled { color: var(--text-muted); cursor: not-allowed; }
    .back-link { display: block; margin-top: 16px; color: var(--text-muted); text-decoration: none; transition: color 0.2s; }
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
    private api: ApiService
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

  setTheme(mode: 'light' | 'dark') {
    this.theme = mode;
    localStorage.setItem('theme', mode);
    document.documentElement.setAttribute('data-theme', mode);
  }

  verifyOtp() {
    this.isProcessing = true;
    this.api.verifyLoginOtp({ email: this.email, otp: this.otp }).subscribe({
      next: (res) => {
        localStorage.setItem('token', res.token);
        localStorage.setItem('user', JSON.stringify(res));
        
        const rawRole = res.role || res.Role || '';
        const role = rawRole.toLowerCase();

        if (role.includes('admin')) {
          this.router.navigate(['/admin/dashboard']);
        } else if (role.includes('manager')) {
          this.router.navigate(['/admin/inventory']); // Managers see inventory
        } else if (role.includes('cashier')) {
          this.router.navigate(['/pos/billing']);
        } else if (role.includes('pending')) {
          this.router.navigate(['/pending-approval']);
        } else {
          this.router.navigate(['/login']);
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
