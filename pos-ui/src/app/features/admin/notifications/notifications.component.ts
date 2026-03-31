import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-notifications',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding: 2rem; background: #f8fafc; min-height: 100vh;">
      <div style="max-width: 1000px; margin: 0 auto;">
        <h1 style="display: flex; align-items: center; gap: 0.75rem; color: #1e293b;">
          <span>🔔</span> Security & System Alerts
        </h1>
        <p style="color: #64748b; margin-bottom: 2rem;">Real-time history of system-wide notifications and automated communications.</p>

        <div *ngIf="loading" style="text-align: center; padding: 3rem;">
          <div class="loader"></div>
          <p>Syncing with Notification Service...</p>
        </div>

        <div *ngIf="!loading && notifications.length === 0" style="background: white; padding: 4rem; text-align: center; border-radius: 12px; border: 1px dashed #cbd5e1;">
          <p style="color: #94a3b8; font-size: 1.1rem;">No recent notifications found.</p>
        </div>

        <div *ngIf="notifications.length > 0" class="notification-list">
          <div *ngFor="let n of notifications" class="notification-card" [ngClass]="n.type.toLowerCase()">
            <div class="card-icon">
              <span *ngIf="n.type === 'Sms'">📱</span>
              <span *ngIf="n.type === 'Email'">📧</span>
              <span *ngIf="n.type === 'SignalR'">🛰️</span>
            </div>
            <div class="card-content">
              <div class="card-header">
                <span class="type-badge">{{ n.type }}</span>
                <span class="recipient">To: {{ n.recipient }}</span>
                <span class="time">{{ n.sentAt | date:'medium' }}</span>
              </div>
              <p class="message">{{ n.content }}</p>
              <div class="card-footer">
                <span class="status" [ngClass]="n.status?.toLowerCase()">● {{ n.status || 'Processed' }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <style>
      .notification-list { display: flex; flex-direction: column; gap: 1rem; }
      .notification-card { 
        background: white; padding: 1.25rem; border-radius: 12px; display: flex; gap: 1.25rem;
        box-shadow: 0 1px 3px rgba(0,0,0,0.05); border-left: 4px solid #94a3b8;
        transition: transform 0.2s;
      }
      .notification-card:hover { transform: translateX(5px); }
      .notification-card.sms { border-left-color: #2563eb; }
      .notification-card.signalr { border-left-color: #8b5cf6; }
      .notification-card.email { border-left-color: #0d9488; }
      
      .card-icon { font-size: 1.5rem; display: flex; align-items: center; }
      .card-content { flex: 1; }
      .card-header { display: flex; gap: 1rem; align-items: center; margin-bottom: 0.5rem; font-size: 0.85rem; }
      .type-badge { font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px; }
      .recipient { color: #2563eb; font-family: monospace; }
      .time { margin-left: auto; color: #94a3b8; }
      .message { color: #334155; margin: 0.5rem 0; line-height: 1.5; }
      .status { font-size: 0.75rem; font-weight: 600; text-transform: uppercase; }
      .status.sent { color: #059669; }
      .status.failed { color: #dc2626; }
      
      .loader {
        border: 4px solid #f3f3f3; border-top: 4px solid #2563eb;
        border-radius: 50%; width: 30px; height: 30px;
        animation: spin 1s linear infinite; margin: 0 auto 1rem;
      }
      @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
    </style>
  `
})
export class AdminNotificationsComponent implements OnInit {
  notifications: any[] = [];
  loading = true;

  constructor(private api: ApiService, private auth: AuthService) { }

  ngOnInit() {
    this.loadNotifications();
  }

  loadNotifications() {
    this.loading = true;
    const recipient = this.auth.currentUserValue?.email || 'All';
    this.api.getRecentNotifications(recipient).subscribe({
      next: (res) => {
        this.notifications = res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }
}
