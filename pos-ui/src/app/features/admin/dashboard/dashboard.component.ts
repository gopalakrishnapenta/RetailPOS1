import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
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
  maxHourlySales = 0;

  // Receipt Modal State
  showReceiptModal = false;
  selectedBill: any = null;
  isReceiptLoading = false;

  constructor(
    private api: ApiService, 
    private signalr: SignalrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // Small delay to ensure any global auth state is settled
    setTimeout(() => {
      this.loadInitialData();
    }, 0);
    this.startAutoRefresh();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  startAutoRefresh() {
    // Replace timer polling with real-time SignalR listening
    this.signalr.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe((notif) => {
        console.log('Dashboard: SignalR Trigger', notif.message);
        // Refresh stats on any relevant event (Orders, Returns, etc)
        this.loadStats(true);
      });
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

  loadStats(isSilent: boolean = false) {
    if (!isSilent) this.isLoading = true;
    this.api.getDashboardStats(this.selectedStoreId).subscribe({
      next: (data: any) => {
        console.log('[DASHBOARD] Stats data received:', data);
        this.stats = data;
        this.storeRevenue = data.categoryBreakdown || [];
        this.alerts = data.lowStockItems || [];
        this.recentTransactions = data.recentBills || [];
        
        // Calculate max sales for chart scaling
        if (data.hourlyTrend && data.hourlyTrend.length > 0) {
          this.maxHourlySales = Math.max(...data.hourlyTrend.map((h: any) => h.sales));
        } else {
          this.maxHourlySales = 0;
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Error loading dashboard stats', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onStoreChange() {
    this.loadStats();
  }

  viewBillReceipt(id: any) {
    if (!id) return;
    this.isReceiptLoading = true;
    this.showReceiptModal = true;
    this.selectedBill = null;

    console.log('Dashboard: Requesting bill details for ID:', id);
    this.api.getBillDetails(id).subscribe({
      next: (data: any) => {
        console.log('Dashboard: Received bill details:', data);
        this.isReceiptLoading = false;
        
        if (data) {
          // Normalize items property (handle both camelCase and PascalCase)
          data.items = data.items || data.Items || [];
          this.selectedBill = data;
        } else {
          console.warn('Dashboard: Bill details empty.');
          alert('Could not find order details.');
          this.closeReceiptModal();
        }
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Dashboard: Receipt API Error', err);
        this.isReceiptLoading = false;
        this.cdr.detectChanges();
        alert('Could not load receipt: ' + (err.error?.message || err.message || 'Server Error'));
        this.closeReceiptModal();
      }
    });
  }

  closeReceiptModal() {
    this.showReceiptModal = false;
    this.selectedBill = null;
  }

  getTrendClass(percent: number): string {
    if (!percent) return '';
    return percent >= 0 ? 'up' : 'down';
  }

  exportRecentTransactions() {
    if (!this.recentTransactions || this.recentTransactions.length === 0) {
      alert('No transactions to export.');
      return;
    }

    const headers = ['Order ID', 'Customer', 'Amount', 'Time'];
    const rows = this.recentTransactions.map(t => [
      `#${t.id}`,
      t.customer || 'Walking Customer',
      t.total,
      t.time
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(r => r.join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `Recent_Transactions_${new Date().toLocaleDateString()}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
