import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
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

  constructor(
    private auth: AuthService, 
    private router: Router,
    private socialAuth: SocialAuthService
  ) {}

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
      error: (err) => {
        this.isLoading = false;
        if (err.error?.message === 'GOOGLE_SIGNUP_REQUIRED_FIELDS') {
          this.error = 'First time? Please go to the Signup page, select your Store and Role, then Sign in with Google.';
          // Optionally redirect automatically
          setTimeout(() => this.router.navigate(['/auth/signup']), 3000);
        } else {
          this.error = err.error?.message || 'Google login failed';
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
  }


  onLogin() {
    this.isLoading = true;
    this.error = '';
    
    this.auth.login(this.credentials).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.handlePostLogin(res);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.error?.message === 'EMAIL_NOT_VERIFIED') {
          this.error = 'Email not verified. Redirecting to verification page...';
          setTimeout(() => this.router.navigate(['/auth/verify-email'], { queryParams: { email: this.credentials.email } }), 2000);
        } else {
          this.error = 'Invalid email or password';
        }
      }
    });
  }
}
