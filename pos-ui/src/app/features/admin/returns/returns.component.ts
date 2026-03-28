import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-admin-returns',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, AdminSidebarComponent],
  template: `
    <div style="display: flex; min-height: 100vh;">
      <app-admin-sidebar active="returns"></app-admin-sidebar>
      <div style="flex: 1; padding: 2rem; background: #f8fafc;">
        <h1 style="color: #1e293b; margin-bottom: 2rem;">Returns Management Queue</h1>
        
        <div style="background: white; padding: 1.5rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1), 0 2px 4px -1px rgba(0,0,0,0.06); margin-top: 1rem;">
          <table style="width: 100%; border-collapse: separate; border-spacing: 0;">
            <thead>
              <tr style="text-align: left; background: #f8fafc; color: #475569; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.025em;">
                <th style="padding: 1rem; border-bottom: 2px solid #f1f5f9; border-top-left-radius: 8px;">Bill # / Date</th>
                <th style="padding: 1rem; border-bottom: 2px solid #f1f5f9;">Customer</th>
                <th style="padding: 1rem; border-bottom: 2px solid #f1f5f9;">Product & Qty</th>
                <th style="padding: 1rem; border-bottom: 2px solid #f1f5f9;">Reason</th>
                <th style="padding: 1rem; border-bottom: 2px solid #f1f5f9;">Status</th>
                <th style="padding: 1rem; border-bottom: 2px solid #f1f5f9; border-top-right-radius: 8px; text-align: center;">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of returns" style="border-bottom: 1px solid #f1f5f9; transition: background 0.2s;" onmouseover="this.style.background='#fbfcfe'" onmouseout="this.style.background='white'">
                <td style="padding: 1rem;">
                  <div style="font-weight: 600; color: #1e293b;">#{{ r.originalBillId }}</div>
                  <div style="font-size: 0.75rem; color: #94a3b8;">{{ r.date | date:'short' }}</div>
                </td>
                <td style="padding: 1rem; color: #334155; font-weight: 500;">{{ r.customerName }}</td>
                <td style="padding: 1rem;">
                  <div style="color: #1e293b; font-weight: 500;">{{ r.productName }}</div>
                  <div style="font-size: 0.8rem; color: #64748b;">Quantity: <strong>{{ r.quantity }}</strong></div>
                </td>
                <td style="padding: 1rem; color: #64748b; font-style: italic;">"{{ r.reason }}"</td>
                <td style="padding: 1rem;">
                  <span style="padding: 0.35rem 0.75rem; border-radius: 9999px; font-size: 0.75rem; font-weight: 600;"
                    [style.background]="r.status === 'Initiated' ? '#fef3c7' : (r.status === 'Approved' ? '#d1fae5' : '#fee2e2')"
                    [style.color]="r.status === 'Initiated' ? '#92400e' : (r.status === 'Approved' ? '#065f46' : '#991b1b')">
                    {{ r.status }}
                  </span>
                </td>
                <td style="padding: 1rem; text-align: center;">
                  <div *ngIf="r.status === 'Initiated'" style="display: flex; gap: 0.5rem; justify-content: center;">
                    <button (click)="approve(r.id)" style="padding: 0.4rem 0.8rem; background: #2563eb; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 0.85rem; font-weight: 500;">Approve</button>
                    <button (click)="reject(r.id)" style="padding: 0.4rem 0.8rem; background: white; color: #ef4444; border: 1px solid #fee2e2; border-radius: 6px; cursor: pointer; font-size: 0.85rem; font-weight: 500;">Reject</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <div *ngIf="returns.length === 0" style="text-align: center; padding: 4rem 2rem; color: #94a3b8;">
            <div style="font-size: 1.1rem; margin-bottom: 0.5rem;">No pending returns found.</div>
            <div style="font-size: 0.9rem;">Excellent! All refund requests are up to date.</div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminReturnsComponent implements OnInit {
  returns: any[] = [];

  constructor(private api: ApiService) {}

  ngOnInit() { this.load(); }

  load() {
    this.api.getReturns().subscribe(res => {
      this.returns = res;
    });
  }

  approve(id: number) {
    this.api.approveReturn(id, 'Approved by Manager').subscribe(() => this.load());
  }

  reject(id: number) {
    this.api.rejectReturn(id, 'Rejected by Manager').subscribe(() => this.load());
  }
}
