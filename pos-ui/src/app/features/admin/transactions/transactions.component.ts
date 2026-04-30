import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-container animate-fade-in">
      <!-- Premium Header -->
      <header class="admin-header glass-panel">
        <div class="title-section">
          <h2><span class="icon">📜</span> Transaction Ledger</h2>
          <p>Advanced audit and financial history</p>
        </div>
        <div class="actions-section">
          <div class="search-host">
             <span class="search-icon">🔍</span>
             <input type="text" [(ngModel)]="searchQuery" (input)="onFilterChange()" placeholder="Filter by ID, Phone, Name...">
          </div>
          <button class="btn btn-primary export-btn" (click)="exportAll()">
            <span class="icon">📥</span> Export Ledger (CSV)
          </button>
        </div>
      </header>

      <!-- Insight Summary Bar -->
      <div class="insight-bar">
        <div class="insight-card glass-panel">
          <div class="i-label">Gross Value (In View)</div>
          <div class="i-value">{{ getViewTotal() | currency:'INR' }}</div>
        </div>
        <div class="insight-card glass-panel warning">
          <div class="i-label">Returns (In View)</div>
          <div class="i-value">{{ getViewReturns() | currency:'INR' }}</div>
        </div>
        <div class="insight-card glass-panel info">
          <div class="i-label">Net Realized</div>
          <div class="i-value">{{ (getViewTotal() - getViewReturns()) | currency:'INR' }}</div>
        </div>
      </div>

      <div class="history-table-container glass-panel">
        <!-- Filter Bar -->
        <div class="table-top-bar">
           <div class="filter-group">
             <div class="chip-group">
               <button class="status-chip" [class.active]="statusFilter === 'all'" (click)="setStatusFilter('all')">All Records</button>
               <button class="status-chip completed" [class.active]="statusFilter === 'completed'" (click)="setStatusFilter('completed')">Completed Only</button>
               <button class="status-chip returned" [class.active]="statusFilter === 'returned'" (click)="setStatusFilter('returned')">Returns Only</button>
               <button class="status-chip pending" [class.active]="statusFilter === 'pending'" (click)="setStatusFilter('pending')">Pending Only</button>
             </div>
           </div>
           <div class="record-count">
             Records <strong>{{ getStartIndex() }} - {{ getEndIndex() }}</strong> of {{ filteredBills.length }}
           </div>
        </div>

        <!-- Ledger Table -->
        <div class="ledger-wrapper">
          <table class="ledger-table">
            <thead>
              <tr>
                <th>Reference</th>
                <th>Timestamp</th>
                <th>Client Details</th>
                <th class="text-right">Tax (GST)</th>
                <th class="text-right">Grand Total</th>
                <th class="text-center">Status Audit</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let bill of paginatedBills" 
                  [class.row-returned]="normalizeStatus(bill.status) === 'returned' || normalizeStatus(bill.status) === 'rejected'"
                  [class.row-pending]="normalizeStatus(bill.status) === 'pending'"
                  [class.row-completed]="normalizeStatus(bill.status) === 'completed'"
                  (click)="viewBillReceipt(bill.id)"
                  style="cursor: pointer;"
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
                    <span class="c-name">{{ bill.customerName || 'Walking Customer' }}</span>
                    <span class="c-sub">{{ bill.customerMobile || 'N/A' }}</span>
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
              <tr *ngIf="!filteredBills.length">
                <td colspan="6" class="empty-state">
                  <div class="empty-content">
                    <span class="empty-icon">🔎</span>
                    <p>No historical records match your current filters.</p>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Professional Pagination -->
        <div class="pagination-footer">
           <div class="page-info">Ledger Page {{ currentPage }} of {{ totalPages }}</div>
           <div class="page-controls">
             <button class="page-nav-btn" [disabled]="currentPage === 1" (click)="prevPage()">← Back</button>
             <div class="page-dots">
                <button *ngFor="let p of getPageRange()" 
                        class="dot-btn" 
                        [class.active]="p === currentPage"
                        (click)="goToPage(p)">
                  {{ p }}
                </button>
             </div>
             <button class="page-nav-btn" [disabled]="currentPage === totalPages" (click)="nextPage()">Next →</button>
           </div>
        </div>
      </div>

      <!-- Receipt Modal -->
      <div class="modal-overlay" *ngIf="showReceiptModal">
        <div class="receipt-card glass-panel animate-slide-up" [class.loading]="isReceiptLoading">
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
            <div class="loading-spinner" *ngIf="isReceiptLoading">
              <span>🔄 Fetching Details...</span>
            </div>
            <button class="btn btn-primary w-100" (click)="closeReceiptModal()">Close Receipt</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-container { padding: 40px; display: flex; flex-direction: column; gap: 32px; background: #f8fafc; min-height: 100vh; }
    
    /* Insight Bar */
    .insight-bar { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; }
    .insight-card { padding: 20px; border-radius: 16px; border-left: 6px solid var(--accent-primary); }
    .insight-card.warning { border-left-color: #ef4444; }
    .insight-card.info { border-left-color: #10b981; }
    .i-label { font-size: 13px; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 8px; }
    .i-value { font-size: 24px; font-weight: 800; color: var(--text-primary); }

    /* Header & Search */
    .search-host { position: relative; display: flex; align-items: center; }
    .search-icon { position: absolute; left: 16px; color: #94a3b8; }
    .search-host input { 
      padding: 12px 16px 12px 48px; border-radius: 14px; border: 2px solid #e2e8f0; 
      background: white; color: #1e293b; width: 380px; font-weight: 600;
      transition: all 0.3s;
    }
    .search-host input:focus { border-color: var(--accent-primary); box-shadow: 0 0 0 4px rgba(59,130,246,0.1); outline: none; }

    /* Table Styles */
    .table-top-bar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    .chip-group { display: flex; gap: 10px; }
    .status-chip { 
      padding: 8px 18px; border-radius: 12px; border: 2px solid #e2e8f0; 
      background: white; font-size: 13px; font-weight: 700; cursor: pointer; transition: 0.2s;
    }
    .status-chip.active { background: #1e293b; color: white; border-color: #1e293b; }
    
    .ledger-wrapper { overflow-x: auto; border: 1.5px solid #e2e8f0; border-radius: 12px; }
    .ledger-table { width: 100%; border-collapse: collapse; background: white; }
    .ledger-table th { background: #f1f5f9; padding: 16px; text-align: left; font-size: 12px; font-weight: 800; color: #64748b; text-transform: uppercase; border-bottom: 2px solid #e2e8f0; }
    .ledger-table td { padding: 18px 16px; border-bottom: 1.5px solid #f1f5f9; font-size: 14px; color: #334155; }
    
    /* Row Highlighting */
    .ledger-row:hover { background: #f8fafc; }
    .row-returned { background: #fff1f2 !important; }
    .row-returned td { border-bottom-color: #fecaca; }
    .row-returned:hover { background: #ffe4e6 !important; }
    
    .row-pending { background: #fffbeb !important; }
    .row-pending td { border-bottom-color: #fef3c7; }
    .row-pending:hover { background: #fef3c7 !important; }

    .row-completed { background: #f0fdf4 !important; }
    .row-completed td { border-bottom-color: #dcfce7; }
    .row-completed:hover { background: #dcfce7 !important; }

    .id-cell strong { color: #1e293b; letter-spacing: -0.5px; }
    .d-info, .c-info { display: flex; flex-direction: column; gap: 2px; }
    .d-main, .c-name { font-weight: 700; color: #1e293b; }
    .d-sub, .c-sub, .tax-text { font-size: 12px; color: #64748b; font-weight: 500; }
    .total-text { font-size: 15px; color: #0f172a; }

    .status-indicator { padding: 5px 12px; border-radius: 8px; font-size: 11px; font-weight: 800; text-transform: uppercase; display: inline-block; }
    .status-indicator.completed, .status-indicator.finalized { background: #dcfce7; color: #166534; }
    .status-indicator.returned, .status-indicator.rejected { background: #ef4444; color: white; }
    .status-indicator.pending { background: #f59e0b; color: white; }

    /* Pagination */
    .pagination-footer { display: flex; justify-content: space-between; align-items: center; padding-top: 32px; }
    .page-nav-btn { padding: 10px 20px; border-radius: 12px; border: 1.5px solid #e2e8f0; background: white; font-weight: 700; cursor: pointer; transition: 0.2s; }
    .page-nav-btn:hover:not(:disabled) { border-color: #1e293b; background: #f8fafc; }
    .dot-btn { width: 40px; height: 40px; border-radius: 10px; border: 1.5px solid #e2e8f0; background: white; font-weight: 800; cursor: pointer; }
    .dot-btn.active { background: #1e293b; color: white; border-color: #1e293b; }

    /* Modal Overlay - FIX FOR POPUP */
    .modal-overlay {
      position: fixed; top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(0, 0, 0, 0.6); backdrop-filter: blur(10px);
      display: flex; align-items: center; justify-content: center; z-index: 9999;
    }
    .receipt-card { 
      width: 440px; background: white; border-radius: 24px; padding: 36px; 
      box-shadow: 0 30px 60px -12px rgba(0, 0, 0, 0.35); position: relative;
    }
    .receipt-header { text-align: center; border-bottom: 2px dashed #cbd5e1; padding-bottom: 24px; margin-bottom: 24px; }
    .receipt-header .logo { font-size: 24px; font-weight: 800; color: #1e293b; margin-bottom: 4px; }
    .receipt-header .logo span { color: #3b82f6; }
    .bill-num { font-family: 'Courier New', Courier, monospace; font-weight: 700; color: #64748b; font-size: 15px; }
    
    .customer-info { 
      font-size: 14px; background: #f1f5f9; padding: 14px; border-radius: 12px; 
      margin-bottom: 24px; border-left: 5px solid #3b82f6; color: #334155;
    }
    .receipt-table { width: 100%; margin-bottom: 24px; border-collapse: collapse; }
    .receipt-table th { font-size: 11px; text-transform: uppercase; color: #94a3b8; padding-bottom: 12px; text-align: left; letter-spacing: 1px; }
    .receipt-table td { font-size: 14px; padding: 8px 0; color: #1e293b; font-weight: 600; border-top: 1px solid #f1f5f9; }
    
    .summary-line { display: flex; justify-content: space-between; font-size: 14px; margin-bottom: 6px; color: #64748b; font-weight: 500; }
    .summary-line.total { 
      margin-top: 16px; padding-top: 16px; border-top: 2px solid #1e293b; 
      font-size: 22px; font-weight: 800; color: #0f172a; 
    }

    .loading-spinner { text-align: center; padding: 30px; color: #3b82f6; font-weight: 700; }
    .animate-slide-up { animation: slideUp 0.4s cubic-bezier(0.16, 1, 0.3, 1); }
    @keyframes slideUp { from { opacity: 0; transform: translateY(30px); } to { opacity: 1; transform: translateY(0); } }

    .animate-fade-in { animation: fadeIn 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
  `]
})
export class TransactionsComponent implements OnInit {
  bills: any[] = [];
  filteredBills: any[] = [];
  searchQuery = '';
  statusFilter = 'all';
  
  // Pagination
  currentPage = 1;
  pageSize = 15; // Increased to 15
  totalPages = 1;

  // Receipt Modal State
  showReceiptModal = false;
  selectedBill: any = null;
  isReceiptLoading = false;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadAllTransactions();
  }

  loadAllTransactions() {
    this.api.getBills().subscribe({
      next: (data) => {
        this.bills = data;
        this.applyFilters();
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading ledger', err)
    });
  }

  viewBillReceipt(id: any) {
    if (!id) return;
    this.isReceiptLoading = true;
    this.showReceiptModal = true;
    this.selectedBill = null;

    this.api.getBillDetails(id).subscribe({
      next: (data: any) => {
        this.isReceiptLoading = false;
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

  onFilterChange() {
    this.currentPage = 1;
    this.applyFilters();
  }

  setStatusFilter(status: string) {
    this.statusFilter = status;
    this.onFilterChange();
  }

  applyFilters() {
    let results = [...this.bills];

    if (this.statusFilter !== 'all') {
      results = results.filter(b => {
        const s = b.status.toLowerCase();
        if (this.statusFilter === 'pending') return ['held', 'draft', 'pendingpayment', 'returnrequested'].includes(s);
        return s === this.statusFilter;
      });
    }

    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase().trim();
      results = results.filter(b => 
        (b.billNumber || b.id || '').toString().toLowerCase().includes(q) ||
        (b.customerName || '').toLowerCase().includes(q) ||
        (b.customerMobile || '').toLowerCase().includes(q)
      );
    }

    this.filteredBills = results.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
    this.totalPages = Math.ceil(this.filteredBills.length / this.pageSize) || 1;
  }

  get paginatedBills() {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredBills.slice(start, start + this.pageSize);
  }

  // Insight Helpers
  getViewTotal() { return this.paginatedBills.reduce((acc, b) => acc + (b.totalAmount || 0), 0); }
  getViewReturns() { 
    return this.paginatedBills
      .filter(b => ['returned', 'rejected'].includes(this.normalizeStatus(b.status)))
      .reduce((acc, b) => acc + (b.totalAmount || 0), 0); 
  }

  // Pagination Helpers
  nextPage() { if (this.currentPage < this.totalPages) this.currentPage++; }
  prevPage() { if (this.currentPage > 1) this.currentPage--; }
  goToPage(p: number) { this.currentPage = p; }

  getPageRange() {
    const pages = [];
    for (let i = 1; i <= this.totalPages; i++) pages.push(i);
    const start = Math.max(0, this.currentPage - 3);
    const end = Math.min(this.totalPages, this.currentPage + 2);
    return pages.slice(start, end);
  }

  getStartIndex() { return this.filteredBills.length ? (this.currentPage - 1) * this.pageSize + 1 : 0; }
  getEndIndex() { return Math.min(this.currentPage * this.pageSize, this.filteredBills.length); }

  normalizeStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (['held', 'draft', 'pendingpayment', 'returnrequested'].includes(s)) return 'pending';
    if (['completed', 'paid', 'finalized'].includes(s)) return 'completed';
    return s;
  }

  exportAll() {
    if (!this.filteredBills.length) return;
    const headers = ['ID', 'Date', 'Customer', 'Mobile', 'Amount', 'Tax', 'Status'];
    const rows = this.filteredBills.map(b => [
      b.billNumber || b.id,
      new Date(b.date).toLocaleString(),
      b.customerName || 'Walk-in',
      b.customerMobile || '',
      b.totalAmount,
      b.taxAmount,
      b.status
    ]);
    const csvContent = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `Store_Ledger_${new Date().toLocaleDateString()}.csv`);
    link.click();
  }
}
