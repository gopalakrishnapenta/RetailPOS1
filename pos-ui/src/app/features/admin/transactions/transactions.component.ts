import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

interface StoreTransactionGroup {
  storeId: number;
  storeName: string;
  bills: any[];
  isExpanded: boolean;
  totalAmount: number;
  returnsAmount: number;
  count: number;
  currentPage: number;
  pageSize: number;
}

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="transactions-wrapper animate-fade-in">
      <!-- Premium Header -->
      <header class="page-header glass-panel">
        <div class="header-left">
          <h2 class="page-title"><span class="icon">📜</span> Transaction Ledger</h2>
          <p class="page-subtitle">Advanced audit and financial history categorized by store</p>
        </div>
        <div class="header-right">
          <div class="search-box">
             <span class="search-icon">🔍</span>
             <input type="text" [(ngModel)]="searchQuery" (input)="onFilterChange()" placeholder="Search by ID, Phone, Name...">
          </div>
          <button class="export-btn" (click)="exportAll()">
            <span class="icon">📥</span> Export CSV
          </button>
        </div>
      </header>

      <!-- Global Insight Summary Bar -->
      <div class="insight-bar">
        <div class="insight-card glass-panel">
          <div class="i-label">Gross Value (All Stores)</div>
          <div class="i-value">{{ globalTotal | currency:'INR' }}</div>
        </div>
        <div class="insight-card glass-panel warning">
          <div class="i-label">Total Returns</div>
          <div class="i-value">{{ globalReturns | currency:'INR' }}</div>
        </div>
        <div class="insight-card glass-panel info">
          <div class="i-label">Net Realized</div>
          <div class="i-value">{{ (globalTotal - globalReturns) | currency:'INR' }}</div>
        </div>
      </div>

      <!-- Filter Controls -->
      <div class="filter-controls">
        <div class="chip-group">
          <button class="status-chip" [class.active]="statusFilter === 'all'" (click)="setStatusFilter('all')">All Records</button>
          <button class="status-chip completed" [class.active]="statusFilter === 'completed'" (click)="setStatusFilter('completed')">Completed</button>
          <button class="status-chip returned" [class.active]="statusFilter === 'returned'" (click)="setStatusFilter('returned')">Returns</button>
          <button class="status-chip pending" [class.active]="statusFilter === 'pending'" (click)="setStatusFilter('pending')">Pending</button>
        </div>
        <div class="record-meta">
          Showing <strong>{{ bills.length }}</strong> transactions across <strong>{{ storeGroups.length }}</strong> locations
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state glass-panel" *ngIf="storeGroups.length === 0">
        <span class="empty-icon">🔎</span>
        <h3>No records found</h3>
        <p>Try adjusting your search or filters.</p>
      </div>

      <!-- Store Groups -->
      <div class="store-groups">
        <div class="store-card glass-panel animate-fade-in" *ngFor="let group of storeGroups">
          
          <!-- Store Header (Expandable) -->
          <div class="store-header" (click)="toggleGroup(group)">
            <div class="store-identity">
              <div class="store-icon">🏪</div>
              <div class="store-info">
                <h3 class="store-name">{{ group.storeName }}</h3>
                <span class="store-id">ID: {{ group.storeId }} • {{ group.count }} Records</span>
              </div>
            </div>
            <div class="store-summary-meta">
              <div class="meta-pill">
                <small>Gross</small>
                <span>{{ group.totalAmount | currency:'INR' }}</span>
              </div>
              <div class="meta-pill warning" *ngIf="group.returnsAmount > 0">
                <small>Returns</small>
                <span>{{ group.returnsAmount | currency:'INR' }}</span>
              </div>
              <div class="meta-pill info">
                <small>Net</small>
                <span>{{ (group.totalAmount - group.returnsAmount) | currency:'INR' }}</span>
              </div>
              <div class="expand-btn" [class.expanded]="group.isExpanded">›</div>
            </div>
          </div>

          <!-- Store Body (Table) -->
          <div class="store-body" [class.expanded]="group.isExpanded">
            <div class="table-scroll">
              <table class="ledger-table">
                <thead>
                  <tr>
                    <th>Ref #</th>
                    <th>Timestamp</th>
                    <th>Customer</th>
                    <th class="text-right">Tax</th>
                    <th class="text-right">Total</th>
                    <th class="text-center">Status</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let bill of getPaginatedBills(group)" 
                      (click)="viewBillReceipt(bill.id)"
                      [class.row-returned]="normalizeStatus(bill.status) === 'returned'"
                      [class.row-pending]="normalizeStatus(bill.status) === 'pending'"
                      class="ledger-row">
                    <td class="id-cell"><strong>#{{ bill.billNumber || bill.id }}</strong></td>
                    <td class="date-cell">
                      <div class="d-info">
                        <span class="d-main">{{ bill.date | date:'dd MMM yyyy' }}</span>
                        <span class="d-sub">{{ bill.date | date:'hh:mm a' }}</span>
                      </div>
                    </td>
                    <td class="client-cell">
                      <div class="c-info">
                        <span class="c-name">{{ bill.customerName || 'Walk-in' }}</span>
                        <span class="c-sub">{{ bill.customerMobile || '—' }}</span>
                      </div>
                    </td>
                    <td class="text-right tax-text">{{ bill.taxAmount | currency:'INR' }}</td>
                    <td class="text-right total-text"><strong>{{ bill.totalAmount | currency:'INR' }}</strong></td>
                    <td class="text-center">
                      <span class="status-indicator" [ngClass]="normalizeStatus(bill.status)">
                        {{ bill.status }}
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Pagination Footer -->
            <div class="pagination-footer" *ngIf="group.count > group.pageSize">
              <div class="p-info">
                Showing <strong>{{ (group.currentPage - 1) * group.pageSize + 1 }}</strong> - 
                <strong>{{ Math.min(group.currentPage * group.pageSize, group.count) }}</strong> of 
                <strong>{{ group.count }}</strong> transactions
              </div>
              <div class="p-controls">
                <button class="p-btn" [disabled]="group.currentPage === 1" (click)="changePage(group, -1)">
                  <span class="icon">‹</span> Prev
                </button>
                <div class="p-pages">
                  Page <strong>{{ group.currentPage }}</strong> of <strong>{{ getTotalPages(group) }}</strong>
                </div>
                <button class="p-btn" [disabled]="group.currentPage === getTotalPages(group)" (click)="changePage(group, 1)">
                  Next <span class="icon">›</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Receipt Modal -->
      <div class="modal-overlay" *ngIf="showReceiptModal" (click)="closeReceiptModal()">
        <div class="receipt-card glass-panel animate-slide-up" (click)="$event.stopPropagation()">
          <div class="receipt-header">
            <div class="logo">POS<span>Retail</span></div>
            <h4>Sales Receipt</h4>
            <p class="bill-num">Order #{{ selectedBill?.billNumber || selectedBill?.id }}</p>
            <p class="date">{{ selectedBill?.date | date:'medium' }}</p>
          </div>

          <div class="receipt-body" *ngIf="selectedBill">
            <div class="customer-info">
              <span>Customer:</span> {{ selectedBill.customerName || 'Walk-in' }} ({{ selectedBill.customerMobile }})
            </div>
            
            <table class="receipt-table">
              <thead>
                <tr>
                  <th>Item</th>
                  <th>Qty</th>
                  <th class="text-right">Price</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of selectedBill.items">
                  <td>{{ item.productName }}</td>
                  <td>{{ item.quantity }}</td>
                  <td class="text-right">{{ item.subTotal | currency:'INR' }}</td>
                </tr>
              </tbody>
            </table>

            <div class="receipt-summary">
              <div class="summary-line">
                <span>Tax Breakdown</span>
                <span>{{ selectedBill.taxAmount | currency:'INR' }}</span>
              </div>
              <div class="summary-line total">
                <span>Total Amount</span>
                <span>{{ selectedBill.totalAmount | currency:'INR' }}</span>
              </div>
            </div>
          </div>

          <div class="receipt-footer">
            <button class="btn-close-receipt" (click)="closeReceiptModal()">Close Receipt</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .transactions-wrapper { padding: 32px; display: flex; flex-direction: column; gap: 24px; background: #f8fafc; min-height: 100vh; }
    
    /* Header */
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px; border-radius: 20px; }
    .page-title { margin: 0; font-size: 1.5rem; font-weight: 800; color: #1e293b; }
    .page-subtitle { margin: 4px 0 0; font-size: 0.9rem; color: #64748b; }
    .header-right { display: flex; gap: 16px; align-items: center; }
    
    .search-box { position: relative; display: flex; align-items: center; }
    .search-icon { position: absolute; left: 16px; color: #94a3b8; }
    .search-box input { 
      padding: 12px 16px 12px 48px; border-radius: 12px; border: 1px solid #e2e8f0; 
      background: white; width: 320px; font-weight: 600; outline: none; transition: 0.2s;
    }
    .search-box input:focus { border-color: #3b82f6; box-shadow: 0 0 0 4px rgba(59,130,246,0.1); }
    
    .export-btn { 
      padding: 12px 20px; border-radius: 12px; border: none; background: #1e293b; color: white; 
      font-weight: 700; cursor: pointer; transition: 0.2s; 
    }
    .export-btn:hover { background: #0f172a; transform: translateY(-1px); }

    /* Insight Bar */
    .insight-bar { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; }
    .insight-card { padding: 24px; border-radius: 18px; border-left: 6px solid #3b82f6; }
    .insight-card.warning { border-left-color: #ef4444; }
    .insight-card.info { border-left-color: #10b981; }
    .i-label { font-size: 0.75rem; font-weight: 800; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
    .i-value { font-size: 1.5rem; font-weight: 800; color: #1e293b; }

    /* Filter Controls */
    .filter-controls { display: flex; justify-content: space-between; align-items: center; }
    .chip-group { display: flex; gap: 8px; }
    .status-chip { 
      padding: 8px 16px; border-radius: 10px; border: 1px solid #e2e8f0; background: white;
      font-size: 0.85rem; font-weight: 700; cursor: pointer; transition: 0.2s;
    }
    .status-chip.active { background: #1e293b; color: white; border-color: #1e293b; }
    .record-meta { font-size: 0.9rem; color: #64748b; }

    /* Store Cards */
    .store-groups { display: flex; flex-direction: column; gap: 16px; }
    .store-card { border-radius: 20px; overflow: hidden; transition: 0.3s; }
    .store-card:hover { transform: translateY(-2px); box-shadow: 0 12px 24px -8px rgba(0,0,0,0.1); }
    
    .store-header { 
      display: flex; justify-content: space-between; align-items: center; 
      padding: 20px 24px; cursor: pointer; background: rgba(255,255,255,0.5);
    }
    .store-identity { display: flex; align-items: center; gap: 16px; }
    .store-icon { 
      width: 48px; height: 48px; border-radius: 14px; background: #3b82f6; 
      display: flex; align-items: center; justify-content: center; font-size: 1.5rem; color: white;
    }
    .store-name { margin: 0; font-size: 1.1rem; font-weight: 800; color: #1e293b; }
    .store-id { font-size: 0.8rem; color: #64748b; font-weight: 600; }
    
    .store-summary-meta { display: flex; align-items: center; gap: 16px; }
    .meta-pill { 
      display: flex; flex-direction: column; align-items: flex-end; 
      padding: 6px 14px; border-radius: 10px; background: #f1f5f9; min-width: 100px;
    }
    .meta-pill small { font-size: 0.65rem; font-weight: 800; color: #64748b; text-transform: uppercase; }
    .meta-pill span { font-size: 0.95rem; font-weight: 800; color: #1e293b; }
    .meta-pill.warning { background: #fef2f2; }
    .meta-pill.warning span { color: #dc2626; }
    .meta-pill.info { background: #f0fdf4; }
    .meta-pill.info span { color: #166534; }
    
    .expand-btn { 
      width: 32px; height: 32px; border-radius: 80px; background: #e2e8f0; 
      display: flex; align-items: center; justify-content: center; 
      font-size: 1.5rem; color: #64748b; transition: 0.3s;
    }
    .expand-btn.expanded { transform: rotate(90deg); background: #1e293b; color: white; }

    /* Store Body */
    .store-body { max-height: 0; overflow: hidden; transition: max-height 0.5s ease-in-out; }
    .store-body.expanded { max-height: 2000px; border-top: 1px solid #f1f5f9; }
    .table-scroll { overflow-x: auto; }

    /* Table Styles */
    .ledger-table { width: 100%; border-collapse: collapse; background: white; }
    .ledger-table th { 
      padding: 14px 20px; text-align: left; background: #f8fafc; 
      font-size: 0.7rem; font-weight: 800; color: #94a3b8; text-transform: uppercase; letter-spacing: 1px;
    }
    .ledger-table td { padding: 16px 20px; border-bottom: 1px solid #f1f5f9; font-size: 0.9rem; }
    .ledger-row { cursor: pointer; transition: 0.2s; }
    .ledger-row:hover { background: #f8fafc; }
    
    .row-returned { background: #fff1f2; }
    .row-pending { background: #fffbeb; }
    
    .id-cell strong { color: #1e293b; font-family: monospace; }
    .d-main { display: block; font-weight: 700; color: #1e293b; }
    .d-sub { font-size: 0.75rem; color: #94a3b8; }
    .c-name { display: block; font-weight: 700; color: #1e293b; }
    .c-sub { font-size: 0.75rem; color: #94a3b8; }
    .tax-text { color: #94a3b8; font-size: 0.8rem; }
    .total-text { color: #1e293b; font-size: 1rem; }
    
    .status-indicator { 
      padding: 4px 12px; border-radius: 20px; font-size: 0.7rem; font-weight: 800; 
      text-transform: uppercase; display: inline-block;
    }
    .status-indicator.completed { background: #dcfce7; color: #166534; }
    .status-indicator.returned { background: #fee2e2; color: #991b1b; }
    .status-indicator.pending { background: #fef3c7; color: #92400e; }

    /* Modal */
    .modal-overlay { 
      position: fixed; inset: 0; background: rgba(0,0,0,0.5); backdrop-filter: blur(8px);
      display: flex; align-items: center; justify-content: center; z-index: 1000;
    }
    .receipt-card { width: 440px; background: white; border-radius: 24px; padding: 32px; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.25); }
    .receipt-header { text-align: center; border-bottom: 2px dashed #e2e8f0; padding-bottom: 20px; margin-bottom: 20px; }
    .receipt-header h4 { margin: 8px 0; font-size: 1.2rem; }
    .bill-num { font-family: monospace; color: #64748b; margin: 0; }
    .logo { font-size: 1.5rem; font-weight: 800; color: #1e293b; }
    .logo span { color: #3b82f6; }
    
    .customer-info { background: #f8fafc; padding: 12px; border-radius: 12px; margin-bottom: 20px; font-size: 0.85rem; }
    .receipt-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
    .receipt-table th { text-align: left; font-size: 0.75rem; color: #94a3b8; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px; }
    .receipt-table td { padding: 8px 0; font-size: 0.9rem; font-weight: 600; }
    
    .summary-line { display: flex; justify-content: space-between; font-size: 0.9rem; margin-bottom: 4px; color: #64748b; }
    .summary-line.total { border-top: 2px solid #1e293b; padding-top: 12px; margin-top: 12px; font-size: 1.25rem; font-weight: 800; color: #0f172a; }
    
    .btn-close-receipt { width: 100%; padding: 12px; border-radius: 12px; border: none; background: #3b82f6; color: white; font-weight: 700; cursor: pointer; }

    .empty-state { padding: 64px; text-align: center; color: #94a3b8; }
    .empty-icon { font-size: 3rem; margin-bottom: 16px; display: block; }

    /* Pagination */
    .pagination-footer { 
      padding: 16px 24px; display: flex; justify-content: space-between; align-items: center; 
      background: #f8fafc; border-top: 1px solid #f1f5f9; 
    }
    .p-info { font-size: 0.85rem; color: #64748b; }
    .p-info strong { color: #1e293b; }
    .p-controls { display: flex; align-items: center; gap: 16px; }
    .p-pages { font-size: 0.85rem; color: #64748b; font-weight: 600; }
    .p-pages strong { color: #1e293b; }
    .p-btn { 
      display: flex; align-items: center; gap: 6px; padding: 6px 14px; border-radius: 8px; 
      border: 1px solid #e2e8f0; background: white; color: #1e293b; font-size: 0.8rem; 
      font-weight: 700; cursor: pointer; transition: 0.2s;
    }
    .p-btn:hover:not(:disabled) { border-color: #3b82f6; color: #3b82f6; background: #eff6ff; }
    .p-btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .p-btn .icon { font-size: 1.1rem; line-height: 1; }
    
    .animate-fade-in { animation: fadeIn 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    .animate-slide-up { animation: slideUp 0.3s ease-out; }
    @keyframes slideUp { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
  `]
})
export class TransactionsComponent implements OnInit {
  bills: any[] = [];
  allStores: any[] = [];
  storeGroups: StoreTransactionGroup[] = [];
  Math = Math;
  
  searchQuery = '';
  statusFilter = 'all';
  
  globalTotal = 0;
  globalReturns = 0;

  // Receipt Modal State
  showReceiptModal = false;
  selectedBill: any = null;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.api.getStores().subscribe({
      next: (stores) => {
        this.allStores = stores;
        this.api.getBills().subscribe({
          next: (data) => {
            this.bills = data;
            this.applyFilters();
            this.cdr.detectChanges();
          },
          error: (err) => console.error('Error loading bills', err)
        });
      },
      error: (err) => console.error('Error loading stores', err)
    });
  }

  applyFilters() {
    let filtered = [...this.bills];

    // Status Filter
    if (this.statusFilter !== 'all') {
      filtered = filtered.filter(b => {
        const s = this.normalizeStatus(b.status);
        return s === this.statusFilter;
      });
    }

    // Search Filter
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase().trim();
      filtered = filtered.filter(b => 
        (b.billNumber || b.id || '').toString().toLowerCase().includes(q) ||
        (b.customerName || '').toLowerCase().includes(q) ||
        (b.customerMobile || '').toLowerCase().includes(q)
      );
    }

    // Group by Store
    const groupMap = new Map<number, StoreTransactionGroup>();
    this.globalTotal = 0;
    this.globalReturns = 0;

    filtered.forEach(bill => {
      const sid = bill.storeId || 0;
      if (!groupMap.has(sid)) {
        const store = this.allStores.find(s => s.id === sid);
        groupMap.set(sid, {
          storeId: sid,
          storeName: store?.name || `Store #${sid}`,
          bills: [],
          isExpanded: false,
          totalAmount: 0,
          returnsAmount: 0,
          count: 0,
          currentPage: 1,
          pageSize: 10
        });
      }
      
      const group = groupMap.get(sid)!;
      group.bills.push(bill);
      group.count++;
      group.totalAmount += (bill.totalAmount || 0);
      
      const status = this.normalizeStatus(bill.status);
      if (status === 'returned') {
        group.returnsAmount += (bill.totalAmount || 0);
      }

      this.globalTotal += (bill.totalAmount || 0);
      if (status === 'returned') this.globalReturns += (bill.totalAmount || 0);
    });

    this.storeGroups = Array.from(groupMap.values())
      .sort((a, b) => b.totalAmount - a.totalAmount);

    // Auto-expand if only one store
    if (this.storeGroups.length === 1) {
      this.storeGroups[0].isExpanded = true;
    }
  }

  toggleGroup(group: StoreTransactionGroup) {
    group.isExpanded = !group.isExpanded;
  }

  getPaginatedBills(group: StoreTransactionGroup) {
    const start = (group.currentPage - 1) * group.pageSize;
    return group.bills.slice(start, start + group.pageSize);
  }

  getTotalPages(group: StoreTransactionGroup) {
    return Math.ceil(group.count / group.pageSize);
  }

  changePage(group: StoreTransactionGroup, delta: number) {
    const next = group.currentPage + delta;
    if (next >= 1 && next <= this.getTotalPages(group)) {
      group.currentPage = next;
    }
  }

  setStatusFilter(status: string) {
    this.statusFilter = status;
    this.applyFilters();
  }

  onFilterChange() {
    this.applyFilters();
  }

  normalizeStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (['returned', 'rejected', 'returnrequested'].includes(s)) return 'returned';
    if (['completed', 'paid', 'finalized'].includes(s)) return 'completed';
    if (['held', 'draft', 'pendingpayment'].includes(s)) return 'pending';
    return s;
  }

  viewBillReceipt(id: any) {
    if (!id) return;
    this.showReceiptModal = true;
    this.selectedBill = null;

    this.api.getBillDetails(id).subscribe({
      next: (data: any) => {
        if (data) {
          data.items = data.items || data.Items || [];
          this.selectedBill = data;
        } else {
          alert('Could not find order details.');
          this.closeReceiptModal();
        }
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        alert('Could not load receipt: ' + (err.error?.message || 'Server Error'));
        this.closeReceiptModal();
      }
    });
  }

  closeReceiptModal() {
    this.showReceiptModal = false;
    this.selectedBill = null;
  }

  exportAll() {
    if (!this.bills.length) return;
    const headers = ['Store', 'ID', 'Date', 'Customer', 'Mobile', 'Amount', 'Tax', 'Status'];
    const rows: any[] = [];
    
    this.storeGroups.forEach(g => {
      g.bills.forEach(b => {
        rows.push([
          g.storeName,
          b.billNumber || b.id,
          new Date(b.date).toLocaleString(),
          b.customerName || 'Walk-in',
          b.customerMobile || '',
          b.totalAmount,
          b.taxAmount,
          b.status
        ]);
      });
    });

    const csvContent = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `Transactions_Report_${new Date().toLocaleDateString()}.csv`);
    link.click();
  }
}
