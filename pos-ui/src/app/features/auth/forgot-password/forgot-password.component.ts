import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="login-container premium-card fade-in">
      <div class="login-header">
        <h2 *ngIf="step === 1">Reset Password</h2>
        <h2 *ngIf="step === 2">Verify OTP</h2>
        <p *ngIf="step === 1">Verify identity via Gmail OTP</p>
        <p *ngIf="step === 2">Check your email for the code</p>
      </div>
      
      <form *ngIf="step === 1" (ngSubmit)="sendOtp()" #f1="ngForm">
        <div class="form-group">
          <label>Registered Email Address</label>
          <input type="email" [(ngModel)]="email" name="email" required placeholder="name@gmail.com">
        </div>
        <div *ngIf="msg" class="success-msg">{{ msg }}</div>
        <button type="submit" [disabled]="!f1.valid" class="btn btn-primary btn-block" style="margin-top:1.5rem">Send OTP</button>
      </form>

      <form *ngIf="step === 2" (ngSubmit)="resetPwd()" #f2="ngForm">
        <p style="font-size:0.9rem; color:var(--text-muted); margin-bottom:1.5rem; text-align: left; background: var(--panel-hover); padding: 10px; border-radius: 6px;">An OTP has been sent to <strong>{{ email }}</strong>.<br><br><small>(Mock Mode: Check terminal window running IdentityService to view the OTP logs)</small></p>
        <div class="form-group">
          <label>6-Digit OTP</label>
          <input type="text" [(ngModel)]="resetData.otp" name="otp" required placeholder="XXXXXX" style="letter-spacing: 0.5rem; text-align: center; font-weight: bold; font-size: 1.2rem;">
        </div>
        <div class="form-group">
          <label>New Password</label>
          <input type="password" [(ngModel)]="resetData.newPassword" name="pwd" required placeholder="Enter advanced new password">
        </div>
        <div *ngIf="msg" [ngClass]="isErr ? 'error-msg' : 'success-msg'">{{ msg }}</div>
        <button type="submit" [disabled]="!f2.valid" class="btn btn-primary btn-block" style="margin-top: 1.5rem">Verify & Reset</button>
      </form>

      <div class="auth-footer"><a routerLink="/auth/login" class="text-link">← Back to Sign in</a></div>
    </div>
  `
})
export class ForgotPasswordComponent {
  step = 1;
  email = '';
  resetData = { email: '', otp: '', newPassword: '' };
  msg = ''; isErr = false;

  constructor(private auth: AuthService, private router: Router) {}

  sendOtp() {
    this.auth.sendOtp(this.email).subscribe(res => {
      this.msg = res.message;
      this.resetData.email = this.email;
      setTimeout(() => { this.step = 2; this.msg = ''; }, 1000);
    });
  }

  resetPwd() {
    this.auth.resetPassword(this.resetData).subscribe({
      next: () => {
        this.isErr = false;
        this.msg = 'Password reset successfully!';
        setTimeout(() => this.router.navigate(['/auth/login']), 1500);
      },
      error: (err) => {
        this.isErr = true;
        this.msg = err.error?.message || 'Invalid or expired OTP';
      }
    });
  }
}
