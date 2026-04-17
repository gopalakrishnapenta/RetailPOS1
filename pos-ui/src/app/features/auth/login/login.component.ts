import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

declare var google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit, AfterViewInit {
  email = '';
  password = '';
  username = '';
  otp = '';
  authError = '';
  showOtp = false;
  rememberMe = false;
  resendCooldown = 0;
  showPassword = false;
  showForgotPassword = false;
  resetEmail = '';
  resetOtp = '';
  resetPassword = '';
  resetError = '';
  resetBusy = false;
  resetOtpCooldown = 0;
  showResetPassword = false;
  isLoading = false;
  private cooldownInterval: any;
  private resetCooldownInterval: any;


  constructor(
    private api: ApiService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadRememberedEmail();
  }

  ngAfterViewInit() {
    this.initGoogleBtn();
  }

  private loadRememberedEmail() {
    const savedEmail = localStorage.getItem('rememberedEmail');
    if (savedEmail) {
      this.email = savedEmail;
      this.rememberMe = true;
      this.resetEmail = savedEmail;
    }
  }

  private handleRememberMe() {
    if (this.rememberMe) {
      localStorage.setItem('rememberedEmail', this.email);
    } else {
      localStorage.removeItem('rememberedEmail');
    }
  }

  initGoogleBtn(retries = 0) {
    if (typeof google !== 'undefined') {
      const btnElement = document.getElementById('googleBtn');
      if (btnElement) {
        google.accounts.id.initialize({
          client_id: '279364566745-oc9hv9a5tt1k91hgoi51s3qi0ot80u92.apps.googleusercontent.com',
          callback: this.handleCredentialResponse.bind(this),
          auto_select: false
        });
        google.accounts.id.renderButton(
          btnElement,
          { theme: 'outline', size: 'large', text: 'signin_with' }
        );
      } else if (retries < 10) {
        setTimeout(() => this.initGoogleBtn(retries + 1), 500);
      }
    } else if (retries < 10) {
      setTimeout(() => this.initGoogleBtn(retries + 1), 500);
    }
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  handleCredentialResponse(response: any) {
    this.authError = '';
    this.isLoading = true;
    this.api.googleLogin({ idToken: response.credential }).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        this.handleAuthResponse(res);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Google login error:', err);
        this.authError = err.error?.message || 'Google login failed';
      }
    });
  }

  onSubmit() {
    this.authError = '';
    this.isLoading = true;
    if (this.showOtp) {
      this.api.verifyLoginOtp({ email: this.email, otp: this.otp }).subscribe({
        next: (res: any) => {
          this.isLoading = false;
          this.handleAuthResponse(res);
        },
        error: (err) => {
          this.isLoading = false;
          this.authError = err.error?.message || 'Invalid OTP';
        }
      });
    } else {
      this.handleRememberMe();
      this.api.login({ email: this.email, password: this.password }).subscribe({
        next: (res: any) => {
          this.isLoading = false;
          if (res.message === 'OTP_SENT') {
            this.router.navigate(['/otp-verification'], { queryParams: { email: this.email } });
          } else {
            this.handleAuthResponse(res.data);
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.authError = err.error?.message || 'Login failed';
        }
      });
    }
  }

  resendOtp() {
    if (this.resendCooldown > 0) return;
    this.authError = '';
    
    this.api.resendLoginOtp(this.email).subscribe({
      next: (res: any) => {
        this.startCooldown();
      },
      error: (err) => {
        this.authError = err.error?.message || 'Failed to resend OTP';
      }
    });
  }

  private startCooldown() {
    this.resendCooldown = 60;
    if (this.cooldownInterval) clearInterval(this.cooldownInterval);
    this.cooldownInterval = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0) {
        clearInterval(this.cooldownInterval);
      }
    }, 1000);
  }

  private handleAuthResponse(data: any) {
    localStorage.setItem('token', data.token || data.Token || '');
    localStorage.setItem('user', JSON.stringify(data));

    const role = data.role || data.Role || '';
    const token = data.token || data.Token || '';

    if (role === 'PENDING_STAFF' || !token) {
      this.router.navigate(['/pending-approval']);
    } else if (role === 'Admin' || role === 'StoreManager') {
      this.router.navigate(['/admin/dashboard']);
    } else {
      this.router.navigate(['/pos/billing']);
    }
  }

  openForgotPassword() {
    this.resetError = '';
    this.resetOtp = '';
    this.resetPassword = '';
    if (!this.resetEmail) this.resetEmail = this.email;
    this.showForgotPassword = true;
  }

  closeForgotPassword() {
    this.showForgotPassword = false;
  }

  sendResetOtp() {
    if (this.resetOtpCooldown > 0) return;
    this.resetError = '';
    const email = this.resetEmail || this.email;
    if (!email) {
      this.resetError = 'Please enter your email.';
      return;
    }
    this.api.sendPasswordResetOtp(email).subscribe({
      next: () => {
        this.resetOtpCooldown = 60;
        if (this.resetCooldownInterval) clearInterval(this.resetCooldownInterval);
        this.resetCooldownInterval = setInterval(() => {
          this.resetOtpCooldown--;
          if (this.resetOtpCooldown <= 0) {
            clearInterval(this.resetCooldownInterval);
          }
        }, 1000);
      },
      error: (err) => {
        this.resetError = err.error?.message || 'Failed to send reset code';
      }
    });
  }

  submitPasswordReset() {
    this.resetError = '';
    const email = this.resetEmail || this.email;
    if (!email || !this.resetOtp || !this.resetPassword) {
      this.resetError = 'Please fill email, OTP, and new password.';
      return;
    }
    this.resetBusy = true;
    this.api.resetPassword({ email, otp: this.resetOtp, newPassword: this.resetPassword }).subscribe({
      next: () => {
        this.resetBusy = false;
        this.showForgotPassword = false;
        this.authError = '';
        alert('Password reset successfully. Please log in.');
      },
      error: (err) => {
        this.resetBusy = false;
        this.resetError = err.error?.message || 'Password reset failed';
      }
    });
  }

  ngOnDestroy() {
    if (this.cooldownInterval) clearInterval(this.cooldownInterval);
    if (this.resetCooldownInterval) clearInterval(this.resetCooldownInterval);
  }
}
