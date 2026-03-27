import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminSidebarComponent],
  templateUrl: './reports.component.html',
  styleUrls: []
})
export class ReportsComponent implements OnInit {
  salesData: any[] = [];
  taxData: any = null;
  isLoading = false;
  fiscalYear = '';

  constructor(private api: ApiService, private auth: AuthService, private router: Router) { }

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

  ngOnInit() {
    // Compute current fiscal year label (Apr–Mar cycle)
    const now = new Date();
    const yr = now.getMonth() >= 3 ? now.getFullYear() : now.getFullYear() - 1;
    this.fiscalYear = `FY ${yr}-${String(yr + 1).slice(-2)}`;
    this.loadReports();
  }

  loadReports() {
    this.isLoading = true;
    this.salesData = [];
    this.taxData = null;

    this.api.getSalesReport().subscribe({
      next: (sales) => {
        this.salesData = sales;
        this.api.getTaxReport().subscribe({
          next: (tax) => { this.taxData = tax; this.isLoading = false; },
          error: () => { this.isLoading = false; }
        });
      },
      error: () => { this.isLoading = false; }
    });
  }
}
