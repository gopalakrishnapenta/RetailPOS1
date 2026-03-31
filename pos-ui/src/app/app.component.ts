import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NotificationService } from './core/services/notification.service';
import { SignalrService } from './core/services/signalr.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'Retail POS system';
  private notificationService = inject(NotificationService);
  private signalrService = inject(SignalrService);
  private authService = inject(AuthService);
  toasts$ = this.notificationService.toasts;

  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.signalrService.startConnection();
    }
  }

  removeToast(id: number) {
    this.notificationService.remove(id);
  }

  getIcon(type: string) {
    switch (type) {
      case 'success': return '✓';
      case 'error': return '✕';
      case 'warning': return '⚠';
      default: return 'ℹ';
    }
  }
}
