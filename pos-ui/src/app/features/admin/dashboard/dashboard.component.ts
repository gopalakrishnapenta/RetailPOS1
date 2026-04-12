import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  // Receipt Modal State
  showReceiptModal = false;
  selectedBill: any = null;
  isReceiptLoading = false;

  constructor(private api: ApiService, private signalr: SignalrService) {}

  ngOnInit() {
    this.loadInitialData();
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

  viewBillReceipt(id: any) {
    if (!id) return;
    this.isReceiptLoading = true;
    this.showReceiptModal = true;
    this.selectedBill = null;

    console.log('Dashboard: Requesting bill details for ID:', id);
    this.api.getBillDetails(id).subscribe({
      next: (data: any) => {
        console.log('Dashboard: Received bill details:', data);
        if (data && (data.items || data.Items)) {
          this.selectedBill = data;
          this.isReceiptLoading = false;
        } else {
          console.warn('Dashboard: Bill details found but no items present.');
          this.isReceiptLoading = false;
          alert('This order exists but has no line items.');
          this.closeReceiptModal();
        }
      },
      error: (err: any) => {
        console.error('Dashboard: Receipt API Error', err);
        this.isReceiptLoading = false;
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
}
