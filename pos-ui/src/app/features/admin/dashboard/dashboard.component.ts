import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  stats: any = {
    totalSales: 0,
    totalBills: 0,
    activeCashiers: 0,
    salesChangePercent: 0
  };
  
  stores: any[] = [];
  selectedStoreId: number = 0;
  storeRevenue: any[] = [];
  alerts: any[] = [];
  recentTransactions: any[] = [];
  isLoading = true;
  isAdmin = false;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadInitialData();
  }

  loadInitialData() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const role = (user.role || user.Role || '').toLowerCase();
    this.isAdmin = role.includes('admin');

    if (this.isAdmin) {
      this.api.getStores().subscribe({
        next: (data: any[]) => {
          this.stores = data;
          this.loadStats();
        },
        error: (err: any) => console.error('Error loading stores', err)
      });
    } else {
      this.selectedStoreId = user.storeId || user.StoreId || 0;
      this.loadStats();
    }
  }

  loadStats() {
    this.isLoading = true;
    this.api.getDashboardStats(this.selectedStoreId).subscribe({
      next: (data: any) => {
        this.stats = data;
        this.storeRevenue = data.categoryBreakdown || [];
        this.alerts = data.lowStockItems || [];
        this.recentTransactions = data.recentBills || [];
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Error loading dashboard stats', err);
        this.isLoading = false;
      }
    });
  }

  onStoreChange() {
    this.loadStats();
  }

  getTrendClass(percent: number): string {
    if (!percent) return '';
    return percent >= 0 ? 'up' : 'down';
  }
}
