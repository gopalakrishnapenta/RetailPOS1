import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

interface StoreReturnGroup {
  storeId: number;
  storeName: string;
  returns: any[];
  isExpanded: boolean;
  totalCount: number;
  pendingCount: number;
  totalRefund: number;
}

@Component({
  selector: 'app-returns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="returns-wrapper">
      <!-- Page Header -->
      <header class="page-header glass-panel">
        <div class="header-left">
          <h2 class="page-title">Returns Overview</h2>
          <p class="page-subtitle">Cross-store return requests grouped by location</p>
        </div>
        <div class="header-right">
          <div class="summary-chips">
            <div class="chip chip-total">
              <span class="chip-label">Total Returns</span>
              <span class="chip-val">{{ totalReturns }}</span>
            </div>
            <div class="chip chip-pending">
              <span class="chip-label">Pending</span>
              <span class="chip-val">{{ totalPending }}</span>
            </div>
            <div class="chip chip-amount">
              <span class="chip-label">Total Refunded</span>
              <span class="chip-val">{{ totalRefunded | currency }}</span>
            </div>
          </div>
          <button class="refresh-btn" (click)="loadData()" [class.spinning]="isLoading">
            <span>🔄</span> Refresh
          </button>
        </div>
      </header>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="isLoading">
        <div class="spinner"></div>
        <p>Loading returns data...</p>
      </div>

      <!-- Empty State -->
      <div class="empty-state glass-panel" *ngIf="!isLoading && storeGroups.length === 0">
        <span class="empty-icon">📦</span>
        <h3>No Returns Found</h3>
        <p>All stores have a clean return record.</p>
      </div>

      <!-- Store Groups -->
      <div class="store-groups" *ngIf="!isLoading && storeGroups.length > 0">
        <div class="store-card glass-panel animate-fade-in" *ngFor="let group of storeGroups">

          <!-- Store Card Header (Clickable to expand) -->
          <div class="store-header" (click)="toggleGroup(group)">
            <div class="store-identity">
              <div class="store-icon">🏪</div>
              <div class="store-info">
                <h3 class="store-name">{{ group.storeName }}</h3>
                <span class="store-id">Store ID: {{ group.storeId }}</span>
              </div>
            </div>
            <div class="store-meta">
              <div class="meta-pill pill-total">
                <span>{{ group.totalCount }}</span>
                <small>Returns</small>
              </div>
              <div class="meta-pill pill-pending" *ngIf="group.pendingCount > 0">
                <span>{{ group.pendingCount }}</span>
                <small>Pending</small>
              </div>
              <div class="meta-pill pill-refund">
                <span>{{ group.totalRefund | currency }}</span>
                <small>Refunded</small>
              </div>
              <div class="expand-btn" [class.expanded]="group.isExpanded">
                <span>›</span>
              </div>
            </div>
          </div>

          <!-- Returns Table (Collapsible) -->
          <div class="store-body" [class.expanded]="group.isExpanded">
            <table class="returns-table">
              <thead>
                <tr>
                  <th>Return #</th>
                  <th>Order #</th>
                  <th>Customer</th>
                  <th>Refund</th>
                  <th>Reason</th>
                  <th>Status</th>
                  <th>Date</th>
                  <th class="text-center">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let ret of group.returns" class="animate-fade-in">
                  <td><span class="badge-id">#{{ ret.id }}</span></td>
                  <td><span class="badge-order">Bill #{{ ret.originalBillId }}</span></td>
                  <td>
                    <span class="customer-mobile">📱 {{ ret.customerMobile || '—' }}</span>
                  </td>
                  <td class="amount-cell">{{ ret.refundAmount | currency }}</td>
                  <td class="reason-cell" [title]="ret.reason">{{ ret.reason }}</td>
                  <td>
                    <span class="status-badge" [ngClass]="getStatusClass(ret.status)">
                      {{ ret.status }}
                    </span>
                  </td>
                  <td class="date-cell">{{ ret.date | date:'dd MMM, hh:mm a' }}</td>
                  <td class="text-center">
                    <div class="action-group" *ngIf="ret.status === 'Initiated'">
                      <button class="action-btn approve-btn" (click)="onApprove(ret)" title="Approve Return">
                        ✅ Approve
                      </button>
                      <button class="action-btn reject-btn" (click)="onReject(ret)" title="Reject Return">
                        ❌ Reject
                      </button>
                    </div>
                    <div class="approval-note" *ngIf="ret.status !== 'Initiated' && ret.managerApprovalNote">
                      <small class="note-text">💬 {{ ret.managerApprovalNote }}</small>
                    </div>
                    <span class="finalized-label" *ngIf="ret.status !== 'Initiated' && !ret.managerApprovalNote">—</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

        </div>
      </div>
    </div>

    <!-- Approval/Rejection Modal -->
    <div class="modal-overlay" *ngIf="showNoteModal" (click)="closeModal()">
      <div class="modal-card glass-panel animate-slide-up" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <div class="modal-icon" [class.approve-icon]="actionType === 'approve'" [class.reject-icon]="actionType === 'reject'">
            {{ actionType === 'approve' ? '✅' : '❌' }}
          </div>
          <h3>{{ actionType === 'approve' ? 'Approve' : 'Reject' }} Return #{{ selectedReturn?.id }}</h3>
          <p class="modal-subtitle">Return for Bill #{{ selectedReturn?.originalBillId }} — {{ selectedReturn?.refundAmount | currency }}</p>
        </div>
        <div class="modal-body">
          <label class="modal-label">Manager Note <span class="optional">(optional)</span></label>
          <textarea [(ngModel)]="actionNote" 
                    [placeholder]="actionType === 'approve' ? 'e.g. Items verified, defect confirmed...' : 'e.g. Item not in returnable condition...'"
                    class="modal-textarea"></textarea>
        </div>
        <div class="modal-actions">
          <button class="btn-cancel" (click)="closeModal()">Cancel</button>
          <button class="btn-confirm" 
                  [class.btn-approve]="actionType === 'approve'" 
                  [class.btn-reject]="actionType === 'reject'"
                  (click)="confirmAction()" 
                  [disabled]="isActionLoading">
            {{ isActionLoading ? 'Processing...' : (actionType === 'approve' ? 'Confirm Approval' : 'Confirm Rejection') }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* ── Layout ── */
    .returns-wrapper { padding: 2rem; display: flex; flex-direction: column; gap: 20px; }

    /* ── Header ── */
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px 28px; border-radius: 16px; flex-wrap: wrap; gap: 16px; }
    .page-title { font-size: 1.4rem; font-weight: 800; color: var(--text-primary); margin: 0; }
    .page-subtitle { font-size: 0.85rem; color: var(--text-muted); margin: 4px 0 0; }
    .header-right { display: flex; align-items: center; gap: 16px; flex-wrap: wrap; }
    .summary-chips { display: flex; gap: 12px; flex-wrap: wrap; }
    .chip { display: flex; flex-direction: column; align-items: center; padding: 10px 20px; border-radius: 12px; min-width: 90px; }
    .chip-label { font-size: 0.7rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; opacity: 0.7; }
    .chip-val { font-size: 1.1rem; font-weight: 800; }
    .chip-total { background: rgba(59,130,246,0.1); color: #3b82f6; }
    .chip-pending { background: rgba(245,158,11,0.1); color: #d97706; }
    .chip-amount { background: rgba(16,185,129,0.1); color: #059669; }
    .refresh-btn { display: flex; align-items: center; gap: 8px; padding: 10px 18px; border-radius: 10px; border: 1px solid var(--border-color); background: var(--bg-secondary); color: var(--text-primary); font-weight: 600; font-size: 0.85rem; cursor: pointer; transition: all 0.2s; }
    .refresh-btn:hover { border-color: #3b82f6; color: #3b82f6; }
    .refresh-btn.spinning span { display: inline-block; animation: spin 1s linear infinite; }

    /* ── Loading & Empty ── */
    .loading-state { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 64px; color: var(--text-muted); }
    .spinner { width: 40px; height: 40px; border: 3px solid var(--border-color); border-top-color: #3b82f6; border-radius: 50%; animation: spin 0.8s linear infinite; }
    .empty-state { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 64px; border-radius: 16px; text-align: center; color: var(--text-muted); }
    .empty-icon { font-size: 3rem; }

    /* ── Store Card ── */
    .store-groups { display: flex; flex-direction: column; gap: 16px; }
    .store-card { border-radius: 16px; overflow: hidden; transition: box-shadow 0.2s; }
    .store-card:hover { box-shadow: 0 8px 32px rgba(0,0,0,0.08); }

    /* ── Store Header ── */
    .store-header { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; cursor: pointer; user-select: none; transition: background 0.2s; border-bottom: 1px solid transparent; }
    .store-header:hover { background: rgba(59,130,246,0.04); }
    .store-identity { display: flex; align-items: center; gap: 16px; }
    .store-icon { width: 48px; height: 48px; background: linear-gradient(135deg, #3b82f6, #6366f1); border-radius: 12px; display: flex; align-items: center; justify-content: center; font-size: 1.4rem; }
    .store-name { font-size: 1.05rem; font-weight: 700; color: var(--text-primary); margin: 0; }
    .store-id { font-size: 0.75rem; color: var(--text-muted); font-family: monospace; }
    .store-meta { display: flex; align-items: center; gap: 12px; }
    .meta-pill { display: flex; flex-direction: column; align-items: center; padding: 8px 16px; border-radius: 10px; min-width: 70px; text-align: center; }
    .meta-pill span { font-size: 1rem; font-weight: 800; line-height: 1; }
    .meta-pill small { font-size: 0.65rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-top: 2px; opacity: 0.7; }
    .pill-total { background: rgba(100,116,139,0.1); color: #64748b; }
    .pill-pending { background: rgba(245,158,11,0.12); color: #d97706; }
    .pill-refund { background: rgba(16,185,129,0.1); color: #059669; }
    .expand-btn { width: 32px; height: 32px; border-radius: 8px; background: var(--bg-tertiary); display: flex; align-items: center; justify-content: center; font-size: 1.2rem; font-weight: 700; color: var(--text-muted); transition: all 0.3s; }
    .expand-btn.expanded { transform: rotate(90deg); color: #3b82f6; background: rgba(59,130,246,0.1); }

    /* ── Store Body (Collapsible) ── */
    .store-body { max-height: 0; overflow: hidden; transition: max-height 0.4s ease; border-top: 0px solid var(--border-color); }
    .store-body.expanded { max-height: 2000px; border-top: 1px solid var(--border-color); }

    /* ── Returns Table ── */
    .returns-table { width: 100%; border-collapse: collapse; text-align: left; }
    .returns-table th { padding: 12px 20px; background: var(--bg-tertiary); color: var(--text-muted); font-weight: 700; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.5px; }
    .returns-table td { padding: 14px 20px; border-bottom: 1px solid var(--border-color); font-size: 0.85rem; vertical-align: middle; color: var(--text-primary); }
    .returns-table tr:last-child td { border-bottom: none; }
    .returns-table tr:hover td { background: rgba(59,130,246,0.02); }

    .badge-id { font-family: monospace; font-size: 0.78rem; font-weight: 700; background: var(--bg-tertiary); color: var(--text-muted); padding: 3px 8px; border-radius: 6px; }
    .badge-order { font-size: 0.78rem; font-weight: 700; background: rgba(59,130,246,0.08); color: #3b82f6; padding: 3px 10px; border-radius: 20px; }
    .customer-mobile { font-size: 0.82rem; color: var(--text-secondary); }
    .amount-cell { font-weight: 700; color: var(--text-primary); }
    .reason-cell { max-width: 180px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: var(--text-muted); font-style: italic; font-size: 0.8rem; }
    .date-cell { font-size: 0.78rem; color: var(--text-muted); white-space: nowrap; }

    .status-badge { padding: 4px 12px; border-radius: 20px; font-size: 0.72rem; font-weight: 700; display: inline-block; }
    .status-badge.warning { background: #fffbeb; color: #d97706; }
    .status-badge.success { background: #ecfdf5; color: #059669; }
    .status-badge.danger  { background: #fef2f2; color: #dc2626; }
    .status-badge.info    { background: #eff6ff; color: #3b82f6; }

    .action-group { display: flex; gap: 6px; justify-content: center; flex-wrap: wrap; }
    .action-btn { display: flex; align-items: center; gap: 4px; padding: 6px 12px; border-radius: 8px; border: none; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2s; }
    .approve-btn { background: #ecfdf5; color: #059669; }
    .approve-btn:hover { background: #d1fae5; }
    .reject-btn { background: #fef2f2; color: #dc2626; }
    .reject-btn:hover { background: #fee2e2; }
    .note-text { font-size: 0.75rem; color: var(--text-muted); font-style: italic; }
    .finalized-label { color: var(--text-muted); font-size: 0.85rem; }
    .text-center { text-align: center; }

    /* ── Modal ── */
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { width: 480px; border-radius: 20px; overflow: hidden; }
    .modal-header { padding: 28px 28px 0; display: flex; flex-direction: column; gap: 8px; }
    .modal-icon { font-size: 2rem; }
    .modal-header h3 { margin: 0; font-size: 1.2rem; font-weight: 800; color: var(--text-primary); }
    .modal-subtitle { margin: 0; font-size: 0.85rem; color: var(--text-muted); }
    .modal-body { padding: 20px 28px; }
    .modal-label { display: block; font-size: 0.8rem; font-weight: 700; color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; }
    .optional { font-weight: 400; opacity: 0.6; text-transform: none; letter-spacing: 0; }
    .modal-textarea { width: 100%; height: 100px; padding: 12px; border-radius: 10px; border: 1px solid var(--border-color); background: var(--bg-secondary); color: var(--text-primary); font-family: inherit; font-size: 0.9rem; outline: none; resize: none; box-sizing: border-box; transition: border-color 0.2s; }
    .modal-textarea:focus { border-color: #3b82f6; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 12px; padding: 0 28px 28px; }
    .btn-cancel { padding: 10px 20px; border-radius: 10px; border: 1px solid var(--border-color); background: transparent; color: var(--text-secondary); font-weight: 600; cursor: pointer; }
    .btn-confirm { padding: 10px 24px; border-radius: 10px; border: none; font-weight: 700; cursor: pointer; transition: all 0.2s; }
    .btn-confirm[disabled] { opacity: 0.6; cursor: not-allowed; }
    .btn-approve { background: #059669; color: white; }
    .btn-approve:hover:not([disabled]) { background: #047857; }
    .btn-reject { background: #dc2626; color: white; }
    .btn-reject:hover:not([disabled]) { background: #b91c1c; }

    @keyframes spin { to { transform: rotate(360deg); } }
    .animate-fade-in { animation: fadeIn 0.3s ease; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(6px); } to { opacity: 1; transform: translateY(0); } }
    .animate-slide-up { animation: slideUp 0.25s ease; }
    @keyframes slideUp { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
  `]
})
export class ReturnsManagementComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  storeGroups: StoreReturnGroup[] = [];
  allStores: any[] = [];
  isLoading = true;

  // Modal
  showNoteModal = false;
  actionNote = '';
  actionType: 'approve' | 'reject' = 'approve';
  selectedReturn: any = null;
  isActionLoading = false;

  // Summary
  get totalReturns(): number { return this.storeGroups.reduce((s, g) => s + g.totalCount, 0); }
  get totalPending(): number { return this.storeGroups.reduce((s, g) => s + g.pendingCount, 0); }
  get totalRefunded(): number { return this.storeGroups.reduce((s, g) => s + g.totalRefund, 0); }

  constructor(
    private api: ApiService,
    private cdr: ChangeDetectorRef,
    private signalr: SignalrService
  ) {}

  ngOnInit() {
    this.loadData();
    this.signalr.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe((notif) => {
        if (notif.message?.includes('RETURN') || notif.message?.includes('REFUND')) {
          this.loadData(true);
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(silent = false) {
    if (!silent) this.isLoading = true;

    // Load stores and returns in parallel
    this.api.getStores().subscribe({
      next: (stores: any[]) => {
        this.allStores = stores;
        this.api.getReturns().subscribe({
          next: (returns: any[]) => {
            this.buildGroups(returns);
            this.isLoading = false;
            this.cdr.detectChanges();
          },
          error: () => { this.isLoading = false; this.cdr.detectChanges(); }
        });
      },
      error: () => {
        // Fallback: load returns without store names
        this.api.getReturns().subscribe({
          next: (returns: any[]) => {
            this.buildGroups(returns);
            this.isLoading = false;
            this.cdr.detectChanges();
          },
          error: () => { this.isLoading = false; this.cdr.detectChanges(); }
        });
      }
    });
  }

  buildGroups(returns: any[]) {
    const groupMap = new Map<number, StoreReturnGroup>();

    for (const ret of returns) {
      const sid = ret.storeId ?? 0;
      if (!groupMap.has(sid)) {
        const store = this.allStores.find(s => s.id === sid);
        groupMap.set(sid, {
          storeId: sid,
          storeName: store?.name ?? `Store #${sid}`,
          returns: [],
          isExpanded: false,
          totalCount: 0,
          pendingCount: 0,
          totalRefund: 0
        });
      }
      const group = groupMap.get(sid)!;
      group.returns.push(ret);
      group.totalCount++;
      if (ret.status === 'Initiated') group.pendingCount++;
      if (ret.status === 'Approved' || ret.status === 'Refunded') {
        group.totalRefund += ret.refundAmount ?? 0;
      }
    }

    this.storeGroups = Array.from(groupMap.values())
      .sort((a, b) => b.pendingCount - a.pendingCount); // Stores with pending first

    // Auto-expand if there's only one store or if there are pending returns
    if (this.storeGroups.length === 1) {
      this.storeGroups[0].isExpanded = true;
    } else {
      this.storeGroups.forEach(g => { if (g.pendingCount > 0) g.isExpanded = true; });
    }
  }

  toggleGroup(group: StoreReturnGroup) {
    group.isExpanded = !group.isExpanded;
  }

  getStatusClass(status: string): string {
    switch ((status || '').toLowerCase()) {
      case 'initiated': return 'warning';
      case 'approved':
      case 'refunded': return 'success';
      case 'refunding': return 'info';
      case 'rejected': return 'danger';
      default: return 'info';
    }
  }

  onApprove(ret: any) {
    this.selectedReturn = ret;
    this.actionType = 'approve';
    this.actionNote = '';
    this.showNoteModal = true;
  }

  onReject(ret: any) {
    this.selectedReturn = ret;
    this.actionType = 'reject';
    this.actionNote = '';
    this.showNoteModal = true;
  }

  closeModal() {
    this.showNoteModal = false;
    this.selectedReturn = null;
  }

  confirmAction() {
    if (!this.selectedReturn) return;
    this.isActionLoading = true;

    const obs = this.actionType === 'approve'
      ? this.api.approveReturn(this.selectedReturn.id, this.actionNote)
      : this.api.rejectReturn(this.selectedReturn.id, this.actionNote);

    obs.subscribe({
      next: () => {
        this.isActionLoading = false;
        this.closeModal();
        this.loadData(true);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error processing return', err);
        alert('Failed to process return: ' + (err.error?.message || 'Unknown error'));
        this.isActionLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
}
