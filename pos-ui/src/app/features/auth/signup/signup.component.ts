import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

declare var google: any;

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './signup.component.html',
  styleUrl: '../login/login.component.css' // Reusing login styles
})
export class SignupComponent implements OnInit, AfterViewInit {
  email = '';
  password = '';
  confirmPassword = '';
  verificationOtp = '';
  isRegistered = false;
  showPassword = false;
  showConfirmPassword = false;
  isLoading = false;


  fullName = '';

  constructor(
    private api: ApiService,
    private router: Router
  ) {}

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
          auto_select: false
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
    this.api.googleLogin({ idToken: response.credential }).subscribe({
      next: (res: any) => {
        localStorage.setItem('token', res.token || res.Token || '');
        localStorage.setItem('user', JSON.stringify(res));
        this.router.navigate(['/pending-approval']);
      },
      error: (err) => alert(err.error?.message || 'Google Sign-Up failed')
    });
  }

  onSubmit() {
    this.isLoading = true;
    if (this.isRegistered) {
      this.api.verifyEmail({ email: this.email, otp: this.verificationOtp }).subscribe({
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

      this.api.register({ fullName: this.fullName, email: this.email, password: this.password }).subscribe({
        next: (res: any) => {
          this.isLoading = false;
          this.isRegistered = true;
          alert(res.message);
        },
        error: (err) => {
          this.isLoading = false;
          alert(err.error?.message || 'Registration failed');
        }
      });
    }
  }
}
