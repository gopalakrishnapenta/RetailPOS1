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
        this.error = err.error?.message || 'Google login failed';
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
      error: () => {
        this.isLoading = false;
        this.error = 'Invalid email or password';
      }
    });
  }
}
