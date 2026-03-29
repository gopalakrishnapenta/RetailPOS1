import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
import { NotificationService } from '../../../core/services/notification.service';
import { GoogleSigninButtonModule, SocialAuthService } from '@abacritt/angularx-social-login';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, GoogleSigninButtonModule],
  templateUrl: './login.component.html',
  styleUrls: []
})
export class LoginComponent implements OnInit {
  credentials = { 
    email: '', 
    password: ''
  };
  
  isLoading = false;
  error = '';
  showPassword = false;

  // 2FA Fields
  isOtpStep = false;
  otpValue = '';

  private auth = inject(AuthService);
  private router = inject(Router);
  private socialAuth = inject(SocialAuthService);
  private notification = inject(NotificationService);

  ngOnInit() {
    this.socialAuth.authState.subscribe((user) => {
      if (user && user.idToken) {
        this.onGoogleLogin(user.idToken);
      }
    });
  }

  onGoogleLogin(idToken: string) {
    this.isLoading = true;
    this.error = '';
    this.auth.googleLogin(idToken).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.handlePostLogin(res);
      },
      error: (err: any) => {
        this.isLoading = false;
        const msg = err.error?.detail || err.error?.message;
        
        if (msg === 'GOOGLE_SIGNUP_REQUIRED_FIELDS' || err.status === 422) {
          this.error = 'First time? Please go to the Signup page, select your Store and Role, then Sign in with Google.';
          setTimeout(() => this.router.navigate(['/auth/signup']), 3000);
        } else {
          this.error = msg || 'Google login failed';
        }
      }
    });
  }

  private handlePostLogin(res: any) {
    const finalShiftDate = res.shiftDate || new Date().toISOString().split('T')[0];

    localStorage.setItem('storeContext', JSON.stringify({
      storeId: res.storeId,
      storeCode: res.storeCode,
      shiftDate: finalShiftDate
    }));
    
    const role = res.role ? res.role.toLowerCase() : '';
    if (role === 'admin' || role === 'storemanager' || role === 'manager') {
      this.router.navigate(['/admin/dashboard']);
    } else {
      this.router.navigate(['/pos/billing']);
    }
    this.notification.success('Welcome back to RetailPOS!');
  }


  onLogin() {
    this.isLoading = true;
    this.error = '';
    
    this.auth.login(this.credentials).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.requiresOtp) {
          this.isOtpStep = true;
          this.notification.warning('Please check your email (or console for dummy accounts) for the login code.', 'OTP Required');
        } else {
          this.handlePostLogin(res);
        }
      },
      error: (err: any) => {
        this.isLoading = false;
        const msg = err.error?.detail || err.error?.message;

        if (msg === 'EMAIL_NOT_VERIFIED' || err.status === 403) {
          this.error = 'Email not verified. Redirecting to verification page...';
          setTimeout(() => this.router.navigate(['/auth/verify-email'], { queryParams: { email: this.credentials.email } }), 2000);
        } else {
          this.error = msg || 'Invalid email or password';
        }
      }
    });
  }

  onVerifyOtp() {
    this.isLoading = true;
    this.error = '';

    const verifyDto = {
      email: this.credentials.email,
      otp: this.otpValue,
      storeCode: null, // Basic login doesn't specify store code in first step usually
      shiftDate: new Date().toISOString().split('T')[0]
    };

    this.auth.verifyLoginOtp(verifyDto).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.handlePostLogin(res);
      },
      error: (err: any) => {
        this.isLoading = false;
        this.error = err.error?.detail || 'Invalid or expired OTP';
      }
    });
  }

  cancelOtp() {
    this.isOtpStep = false;
    this.otpValue = '';
    this.error = '';
  }
}
