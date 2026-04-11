import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-returns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-container" [class.is-loading]="isLoading">
      <header class="admin-header glass-panel">
        <div class="title-section">
          <h2>Returns Management</h2>
          <p>Accept or reject return requests initiated by cashiers</p>
        </div>
        <div class="actions-section">
          <button class="btn btn-secondary" (click)="loadReturns()" [class.btn-loading]="isLoading">
            <span class="icon">🔄</span> Refresh List
          </button>
        </div>
      </header>

      <section class="table-section glass-panel">
        <div class="table-container">
          <table class="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Customer</th>
                <th>Product</th>
                <th>Qty</th>
                <th>Refund Amt</th>
                <th>Reason</th>
                <th>Status</th>
                <th>Date</th>
                <th class="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let ret of returns" class="animate-fade-in">
                <td><span class="badge-sku">#{{ ret.id }}</span></td>
                <td>
                  <div class="user-cell">
                    <span class="user-name">{{ ret.customerMobile }}</span>
                  </div>
                </td>
                <td><span class="badge-tag">{{ ret.productName || 'Product #' + ret.productId }}</span></td>
                <td>{{ ret.quantity }}</td>
                <td class="amount">{{ ret.refundAmount | currency:'INR' }}</td>
                <td class="reason" [title]="ret.reason">{{ ret.reason }}</td>
                <td>
                  <span class="badge" [ngClass]="getStatusClass(ret.status)">
                    {{ ret.status }}
                  </span>
                </td>
                <td class="text-muted">{{ ret.date | date:'short' }}</td>
                <td class="text-center">
                  <div class="action-buttons" *ngIf="ret.status === 'Initiated'">
                    <button class="btn-icon approve" (click)="onApprove(ret)" title="Approve">
                      <span class="icon">✅</span>
                    </button>
                    <button class="btn-icon reject" (click)="onReject(ret)" title="Reject">
                      <span class="icon">❌</span>
                    </button>
                  </div>
                  <div class="approval-note" *ngIf="ret.status !== 'Initiated' && ret.managerApprovalNote">
                    <small class="text-muted">{{ ret.managerApprovalNote }}</small>
                  </div>
                </td>
              </tr>
              <tr *ngIf="!returns.length && !isLoading">
                <td colspan="9" class="empty-state">
                  <div class="empty-content">
                    <span>📦</span>
                    <p>No return requests found.</p>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>

    <!-- Modal for Approval/Rejection Note -->
    <div class="modal-overlay" *ngIf="showNoteModal">
      <div class="modal-card glass-panel animate-slide-up">
         <h3>{{ actionType === 'approve' ? 'Approve' : 'Reject' }} Return Request</h3>
         <p>Add a note for this decision (optional):</p>
         <textarea [(ngModel)]="actionNote" placeholder="e.g. Verified defective, items returned..."></textarea>
         <div class="modal-actions">
           <button class="btn btn-secondary" (click)="closeModal()">Cancel</button>
           <button class="btn" [ngClass]="actionType === 'approve' ? 'btn-primary' : 'btn-danger'" 
                   (click)="confirmAction()" [disabled]="isActionLoading">
              {{ isActionLoading ? 'Processing...' : 'Confirm ' + (actionType === 'approve' ? 'Approval' : 'Rejection') }}
           </button>
         </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-container { padding: 2rem; display: flex; flex-direction: column; gap: 24px; }
    .admin-header { display: flex; justify-content: space-between; align-items: center; padding: 24px; }
    .table-section { padding: 0; overflow: hidden; border-radius: 12px; }
    
    .actions-section { display: flex; gap: 12px; align-items: center; }

    .admin-table { width: 100%; border-collapse: collapse; text-align: left; }
    .admin-table th { padding: 16px 24px; background: #f8fafc; color: #64748b; font-weight: 600; font-size: 0.75rem; text-transform: uppercase; border-bottom: 1px solid #e2e8f0; }
    .admin-table td { padding: 16px 24px; border-bottom: 1px solid #e2e8f0; font-size: 0.875rem; vertical-align: middle; }
    
    .badge-sku { background: #f1f5f9; color: #64748b; padding: 4px 8px; border-radius: 6px; font-family: monospace; font-size: 0.75rem; font-weight: 600; }
    .badge-tag { background: rgba(59, 130, 246, 0.1); color: #3b82f6; padding: 4px 10px; border-radius: 20px; font-size: 0.75rem; font-weight: 600; }
    
    .amount { font-weight: 700; color: #1e293b; }
    .reason { max-width: 200px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #64748b; font-style: italic; }

    .badge { padding: 4px 12px; border-radius: 20px; font-size: 0.75rem; font-weight: 600; display: inline-block; }
    .badge.warning { background: #fffbeb; color: #d97706; } /* Initiated */
    .badge.success { background: #ecfdf5; color: #059669; } /* Approved */
    .badge.danger { background: #fef2f2; color: #dc2626; }  /* Rejected */

    .action-buttons { display: flex; gap: 8px; justify-content: center; }
    .btn-icon { width: 36px; height: 36px; border-radius: 8px; border: 1px solid #e2e8f0; background: white; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
    .btn-icon:hover { background: #f8fafc; border-color: #2563eb; }
    .btn-icon.approve:hover { color: #10b981; border-color: #10b981; }
    .btn-icon.reject:hover { color: #ef4444; border-color: #ef4444; }

    .text-center { text-align: center; }
    .text-muted { color: #94a3b8; font-size: 0.75rem; }

    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.4); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { width: 450px; padding: 32px; border-radius: 16px; background: white; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1); }
    textarea { width: 100%; height: 120px; margin: 16px 0; padding: 12px; border-radius: 8px; border: 1px solid #e2e8f0; outline: none; transition: border-color 0.2s; font-family: inherit; }
    textarea:focus { border-color: #2563eb; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 12px; }

    .empty-state { text-align: center; padding: 64px 0; color: #94a3b8; }
    .empty-content span { font-size: 48px; display: block; margin-bottom: 16px; }
  `]
})
export class ReturnsManagementComponent implements OnInit {
  returns: any[] = [];
  isLoading = true;
  showNoteModal = false;
  actionNote = '';
  actionType: 'approve' | 'reject' = 'approve';
  currentReturn: any = null;
  isActionLoading = false;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadReturns();
  }

  loadReturns() {
    this.isLoading = true;
    this.api.getReturns().subscribe({
      next: (data: any[]) => {
        this.returns = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading returns', err);
        this.isLoading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'initiated': return 'warning';
      case 'approved': return 'success';
      case 'rejected': return 'danger';
      default: return 'info';
    }
  }

  onApprove(ret: any) {
    this.currentReturn = ret;
    this.actionType = 'approve';
    this.showNoteModal = true;
    this.actionNote = '';
  }

  onReject(ret: any) {
    this.currentReturn = ret;
    this.actionType = 'reject';
    this.showNoteModal = true;
    this.actionNote = '';
  }

  closeModal() {
    this.showNoteModal = false;
    this.currentReturn = null;
  }

  confirmAction() {
    if (!this.currentReturn) return;
    this.isActionLoading = true;
    
    const obs = this.actionType === 'approve' 
      ? this.api.approveReturn(this.currentReturn.id, this.actionNote)
      : this.api.rejectReturn(this.currentReturn.id, this.actionNote);

    obs.subscribe({
      next: () => {
        this.isActionLoading = false;
        this.closeModal();
        this.loadReturns();
      },
      error: (err) => {
        console.error('Error processing return', err);
        alert('Failed to process return: ' + (err.error?.message || err.message || 'Unknown error'));
        this.isActionLoading = false;
      }
    });
  }
}
