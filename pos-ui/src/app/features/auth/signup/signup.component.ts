import { Component, OnInit, AfterViewInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { AuthService } from '../../../core/services/auth.service';

declare var google: any;

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './signup.component.html',
  styleUrl: '../login/login.component.css' // Reusing login styles
})
export class SignupComponent implements OnInit, AfterViewInit, OnDestroy {
  email = '';
  password = '';
  confirmPassword = '';
  verificationOtp = '';
  isRegistered = false;
  showPassword = false;
  showConfirmPassword = false;
  isLoading = false;
  resendCooldown = 0;
  private cooldownInterval: any;

  fullName = '';

  private authApi = inject(AuthApiService);
  private authService = inject(AuthService);
  private router = inject(Router);

  constructor() {}

  ngOnInit() {
  }

  ngAfterViewInit() {
    this.initGoogleSignupBtn();
  }

  initGoogleSignupBtn(retries = 0) {
    if (typeof google !== 'undefined') {
      const btnElement = document.getElementById('googleSignupBtn');
      if (btnElement) {
        google.accounts.id.initialize({
          client_id: '279364566745-oc9hv9a5tt1k91hgoi51s3qi0ot80u92.apps.googleusercontent.com',
          callback: this.handleGoogleSignupResponse.bind(this),
          auto_select: false,
          ux_mode: 'popup'
        });
        google.accounts.id.renderButton(
          btnElement,
          { theme: 'outline', size: 'large', text: 'signup_with' }
        );
      } else if (retries < 10) {
        setTimeout(() => this.initGoogleSignupBtn(retries + 1), 500);
      }
    } else if (retries < 10) {
      setTimeout(() => this.initGoogleSignupBtn(retries + 1), 500);
    }
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  handleGoogleSignupResponse(response: any) {
    this.isLoading = true;
    this.authService.googleLogin({ idToken: response.credential }).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        this.handleAuthNavigation(res.data);
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Google signup error:', err);
        alert(err.error?.message || 'Google Sign-Up failed');
      }
    });
  }

  private handleAuthNavigation(data: any) {
    if (!data) return;
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

  resendOtp() {
    if (this.resendCooldown > 0) return;
    this.authApi.resendVerification(this.email).subscribe({
      next: () => {
        this.startCooldown();
        alert('Verification code resent successfully.');
      },
      error: (err) => alert(err.error?.message || 'Failed to resend code')
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

  onSubmit() {
    this.isLoading = true;
    if (this.isRegistered) {
      this.authApi.verifyEmail({ email: this.email, otp: this.verificationOtp }).subscribe({
        next: () => {
          this.isLoading = false;
          alert('Email verified successfully! Please log in.');
          this.router.navigate(['/login']);
        },
        error: (err) => {
          this.isLoading = false;
          alert(err.error?.message || 'Verification failed');
        }
      });
    } else {
      if (!this.fullName) {
        this.isLoading = false;
        alert('Please enter your full name');
        return;
      }
      if (this.password !== this.confirmPassword) {
        this.isLoading = false;
        alert('Passwords do not match');
        return;
      }

      this.authApi.register({ fullName: this.fullName, email: this.email, password: this.password }).subscribe({
        next: (res: any) => {
          this.isLoading = false;
          this.isRegistered = true;
          this.startCooldown();
          alert(res.message);
        },
        error: (err) => {
          this.isLoading = false;
          alert(err.error?.message || 'Registration failed');
        }
      });
    }
  }

  ngOnDestroy() {
    if (this.cooldownInterval) clearInterval(this.cooldownInterval);
  }
}

