import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminSidebarComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: []
})
export class DashboardComponent implements OnInit, OnDestroy {
  kpis: any = null;
  selectedBill: any = null;
  private pollInterval: any = null;

  constructor(private api: ApiService, public auth: AuthService, private router: Router) { }

  ngOnInit() {
    this.loadDashboard();
    // Poll every 10 seconds for real-time updates
    this.pollInterval = setInterval(() => this.loadDashboard(), 10000);
  }

  ngOnDestroy() {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  loadDashboard() {
    this.api.getDashboard().subscribe({
      next: (res) => { this.kpis = res; },
      error: () => { } // keep last data on error
    });
  }

  showBillDetails(billId: number) {
    this.api.getBillById(billId).subscribe(res => {
      this.selectedBill = res;
    });
  }

  closeDetails() {
    this.selectedBill = null;
  }

  /** Returns a bar height % (10–100) relative to the max sales hour */
  getBarHeight(sales: number, trend: any[]): number {
    if (!trend || trend.length === 0) return 10;
    const max = Math.max(...trend.map((h: any) => h.sales ?? 0));
    if (max === 0) return 10;
    return Math.max(10, Math.round((sales / max) * 90) + 10);
  }

  isAdmin(): boolean {
    return this.auth.getRole() === 'Admin';
  }

  isManagerOrHigher(): boolean {
    const role = this.auth.getRole();
    return role === 'Admin' || role === 'Manager' || role === 'StoreManager';
  }

  getRoleHeader(): string {
    const role = this.auth.getRole();
    if (role === 'Admin') return 'Super Admin';
    if (role === 'StoreManager') return 'Store Manager';
    if (role === 'Manager') return 'Manager';
    return 'Staff';
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
