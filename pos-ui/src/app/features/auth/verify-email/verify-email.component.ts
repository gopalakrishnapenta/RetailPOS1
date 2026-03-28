import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.css']
})
export class VerifyEmailComponent implements OnInit {
  email: string = '';
  otp: string = '';
  message: string = '';
  error: string = '';
  isLoading: boolean = false;
  canResend: boolean = false;
  timer: number = 30;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      if (!this.email) {
        this.router.navigate(['/auth/login']);
      }
    });
    this.startTimer();
  }

  startTimer() {
    this.canResend = false;
    this.timer = 30;
    const interval = setInterval(() => {
      this.timer--;
      if (this.timer <= 0) {
        this.canResend = true;
        clearInterval(interval);
      }
    }, 1000);
  }

  onVerify() {
    if (this.otp.length !== 6) {
      this.error = 'Please enter a valid 6-digit code.';
      return;
    }

    this.isLoading = true;
    this.error = '';
    this.message = '';

    this.authService.verifyEmail(this.email, this.otp).subscribe({
      next: () => {
        this.message = 'Email verified successfully! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/auth/login']), 2000);
      },
      error: (err) => {
        this.isLoading = false;
        this.error = err.error?.message || 'Verification failed. Please try again.';
      }
    });
  }

  onResend() {
    if (!this.canResend) return;

    this.isLoading = true;
    this.error = '';
    this.message = '';

    this.authService.resendVerification(this.email).subscribe({
      next: () => {
        this.isLoading = false;
        this.message = 'A new verification code has been sent to your email.';
        this.startTimer();
      },
      error: (err) => {
        this.isLoading = false;
        this.error = 'Failed to resend code. Please try again later.';
      }
    });
  }
}
