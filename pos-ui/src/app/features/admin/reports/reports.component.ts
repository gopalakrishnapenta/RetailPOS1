import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminSidebarComponent],
  template: `
    <div style="display: flex; min-height: 100vh;">
      <app-admin-sidebar active="reports"></app-admin-sidebar>
      <div style="flex: 1; padding: 2rem; background: #f8fafc;">
        <h1 style="color: #1e293b;">Financial Reports & Analytics</h1>
        <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 1.5rem; margin-top: 2rem;">
          <div style="background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
            <p style="font-size: 0.8rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Gross Sales</p>
            <p style="font-size: 2rem; font-weight: 700; color: #1e293b; margin: 0.5rem 0;">{{ (data?.grossSales || 0) | currency }}</p>
          </div>
          <div style="background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
            <p style="font-size: 0.8rem; font-weight: 600; color: #64748b; text-transform: uppercase;">Total Refunded</p>
            <p style="font-size: 2rem; font-weight: 700; color: #ef4444; margin: 0.5rem 0;">{{ (data?.refundedAmount || 0) | currency }}</p>
          </div>
          <div style="background: #1e293b; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); color: white;">
            <p style="font-size: 0.8rem; font-weight: 600; color: #94a3b8; text-transform: uppercase;">Net Revenue</p>
            <p style="font-size: 2rem; font-weight: 700; color: white; margin: 0.5rem 0;">{{ (data?.totalSales || 0) | currency }}</p>
          </div>
        </div>

        <div style="background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); margin-top: 2rem;">
          <h3>Refund Analytics</h3>
          <p style="color: #64748b;">Refund Rate: <strong>{{ refundRate }}%</strong> of total volume.</p>
          <div style="height: 10px; width: 100%; background: #f1f5f9; border-radius: 5px; margin-top: 0.5rem;">
            <div [style.width]="refundRate + '%'" style="height: 100%; background: #ef4444; border-radius: 5px;"></div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminReportsComponent implements OnInit {
  data: any = null;
  refundRate = 0;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.api.getDashboard().subscribe(res => {
      this.data = res;
      if (res && res.grossSales > 0) {
        this.refundRate = Math.round((res.refundedAmount / res.grossSales) * 100);
      }
    });
  }
}
