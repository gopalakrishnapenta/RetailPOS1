import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-container">
      <header class="admin-header glass-panel">
        <div class="title-section">
          <h2>Staff Management</h2>
          <p>Manage users, roles and store assignments</p>
        </div>
        <div class="actions">
          <div class="search-box">
            <span class="search-icon">🔍</span>
            <input type="text" [(ngModel)]="searchQuery" (input)="applyFilters()" placeholder="Search Name or Email..." class="input-field search-input">
          </div>
          <button class="btn btn-primary" (click)="openPendingModal()">+ Add New Staff</button>
        </div>
      </header>

      <section class="table-section glass-panel">
        <div class="table-container">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Member</th>
                <th>Role</th>
                <th>Assigned Store</th>
                <th>Status</th>
                <th class="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let user of paginatedStaff" class="animate-fade-in">
                <td>
                  <div class="user-cell">
                    <div class="avatar">{{ (user.fullName || user.email || '?').charAt(0).toUpperCase() }}</div>
                    <div class="user-meta">
                      <span class="user-name">{{ user.fullName || 'New User' }}</span>
                      <span class="user-email text-muted">{{ user.email }}</span>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="badge-role" [ngClass]="getRoleClass(user.assignedRole)">
                    {{ user.assignedRole || 'Pending' }}
                  </span>
                </td>
                <td>
                  <div class="store-info">
                    <span class="store-name">{{ user.assignedStoreId ? 'Store #' + user.assignedStoreId : 'Global Access' }}</span>
                  </div>
                </td>
                <td>
                  <div class="status-pill" [class.active]="user.isAssigned">
                    <span class="dot"></span>
                    {{ user.isAssigned ? 'Active' : 'Pending' }}
                  </div>
                </td>
                <td class="text-center">
                  <div class="action-buttons">
                    <button class="btn-icon edit" title="Assign Role & Store" (click)="openEditModal(user)">
                      <i class="icon">✏️</i>
                    </button>
                    <button class="btn-icon delete" title="Unassign" (click)="unassignStaff(user)"
                      [disabled]="!user.isAssigned">
                      <i class="icon">🚫</i>
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="!paginatedStaff.length && !isLoading">
                <td colspan="5" class="empty-state">
                  <div class="empty-content">
                    <span>👥</span>
                    <p>No staff members found.</p>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- Pagination Controls -->
      <div class="pagination-controls glass-panel" *ngIf="filteredStaff.length > itemsPerPage">
        <button class="btn btn-secondary" [disabled]="currentPage === 1" (click)="currentPage = currentPage - 1">Previous</button>
        <span class="page-info">Page {{ currentPage }} of {{ totalPages || 1 }}</span>
        <button class="btn btn-secondary" [disabled]="currentPage === totalPages" (click)="currentPage = currentPage + 1">Next</button>
      </div>

      <!-- Assign/Edit Staff Modal -->
      <div class="modal-overlay" *ngIf="showModal" (click)="closeModal()">
        <div class="modal-content glass-panel animate-slide-up" (click)="$event.stopPropagation()">
          <header class="modal-header">
            <h3>{{ modalTitle }}</h3>
            <button class="close-btn" (click)="closeModal()">×</button>
          </header>

          <div class="modal-body">
            <!-- User info banner -->
            <div class="user-banner" *ngIf="selectedUser">
              <div class="avatar-lg">{{ (selectedUser.fullName || selectedUser.email || '?').charAt(0).toUpperCase() }}</div>
              <div>
                <div class="user-name-lg">{{ selectedUser.fullName || 'New User' }}</div>
                <div class="user-email-sm">{{ selectedUser.email }}</div>
              </div>
            </div>

            <div class="form-container">
              <div class="form-group">
                <label>Role</label>
                <select class="input-field" [(ngModel)]="assignForm.role">
                  <option value="Admin">Admin</option>
                  <option value="StoreManager">Store Manager</option>
                  <option value="Cashier">Cashier</option>
                </select>
              </div>

              <div class="form-group" *ngIf="assignForm.role !== 'Admin'">
                <label>Store</label>
                <select class="input-field" [(ngModel)]="assignForm.storeId">
                  <option [value]="0">Global Access</option>
                  <option *ngFor="let store of stores" [value]="store.id">{{ store.name }}</option>
                </select>
                <p class="hint" *ngIf="isLoadingStores">Loading stores...</p>
              </div>

              <div class="form-group" *ngIf="assignForm.role === 'Admin'">
                <label>Store</label>
                <div class="input-field readonly-field">Global Access (Admin is always global)</div>
              </div>
            </div>
          </div>

          <footer class="modal-footer">
            <button class="btn btn-secondary" (click)="closeModal()">Cancel</button>
            <button class="btn btn-primary" (click)="saveAssignment()" [disabled]="isSaving">
              {{ isSaving ? 'Saving...' : 'Save Assignment' }}
            </button>
          </footer>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-container { padding: var(--spacing-xl); display: flex; flex-direction: column; gap: var(--spacing-xl); }
    .admin-header { display: flex; justify-content: space-between; align-items: center; padding: var(--spacing-lg); flex-wrap: wrap; gap: 16px; }
    .table-section { padding: 0; overflow: hidden; }
    
    .actions { display: flex; gap: 12px; align-items: center; }
    .search-box { position: relative; min-width: 250px; }
    .search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); color: var(--text-muted); font-size: 14px; }
    .search-input { padding-left: 36px !important; }

    .pagination-controls { display: flex; justify-content: center; align-items: center; gap: 16px; padding: var(--spacing-md); margin-top: -10px; }
    .page-info { font-weight: 600; color: var(--text-secondary); font-size: 0.875rem; }

    .admin-table { width: 100%; border-collapse: collapse; text-align: left; }
    .admin-table th { padding: var(--spacing-md) var(--spacing-lg); background: var(--bg-tertiary); color: var(--text-secondary); font-weight: 600; font-size: 0.75rem; text-transform: uppercase; border-bottom: 1px solid var(--border-color); }
    .admin-table td { padding: var(--spacing-md) var(--spacing-lg); border-bottom: 1px solid var(--border-color); font-size: 0.875rem; vertical-align: middle; }
    
    .user-cell { display: flex; align-items: center; gap: 12px; }
    .avatar { width: 36px; height: 36px; border-radius: 50%; background: var(--accent-primary); color: white; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px; flex-shrink: 0; }
    .user-meta { display: flex; flex-direction: column; }
    .user-name { font-weight: 600; color: var(--text-primary); }
    .user-email { font-size: 12px; }

    .badge-role { padding: 4px 10px; border-radius: var(--radius-sm); font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; }
    .role-admin { background: #fee2e2; color: #991b1b; }
    .role-manager { background: #fef3c7; color: #92400e; }
    .role-staff { background: #dcfce7; color: #166534; }
    .role-default { background: var(--bg-tertiary); color: var(--text-secondary); }

    .status-pill { display: inline-flex; align-items: center; gap: 6px; padding: 4px 10px; border-radius: var(--radius-full); font-size: 12px; font-weight: 500; background: #f0f9ff; color: #0369a1; }
    .status-pill.active { background: #ecfdf5; color: #047857; }
    .status-pill .dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }

    .action-buttons { display: flex; gap: 8px; justify-content: center; }
    .btn-icon { width: 30px; height: 30px; border-radius: 6px; border: 1px solid var(--border-color); background: white; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
    .btn-icon:hover { background: var(--bg-tertiary); color: var(--accent-primary); border-color: var(--accent-primary); }
    .btn-icon.delete:hover { background: #fef2f2; color: var(--accent-danger); border-color: var(--accent-danger); }
    .btn-icon:disabled { opacity: 0.4; cursor: not-allowed; }

    .text-center { text-align: center; }
    .text-muted { color: var(--text-muted); }

    /* Modal */
    .modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.45); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-content { width: 100%; max-width: 480px; padding: 24px; }
    .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; padding-bottom: 16px; border-bottom: 1px solid var(--border-color); }
    .modal-header h3 { margin: 0; font-size: 1.1rem; }
    .close-btn { background: none; border: none; font-size: 24px; cursor: pointer; color: var(--text-muted); line-height: 1; }
    .modal-body { display: flex; flex-direction: column; gap: 20px; }
    .modal-footer { margin-top: 24px; display: flex; justify-content: flex-end; gap: 12px; padding-top: 16px; border-top: 1px solid var(--border-color); }

    .user-banner { display: flex; align-items: center; gap: 14px; padding: 14px 16px; background: var(--bg-tertiary); border-radius: var(--radius-md); border: 1px solid var(--border-color); }
    .avatar-lg { width: 44px; height: 44px; border-radius: 50%; background: var(--accent-primary); color: white; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 18px; flex-shrink: 0; }
    .user-name-lg { font-weight: 700; font-size: 0.95rem; color: var(--text-primary); }
    .user-email-sm { font-size: 0.8rem; color: var(--text-muted); margin-top: 2px; }

    .form-container { display: flex; flex-direction: column; gap: 16px; }
    .form-group { display: flex; flex-direction: column; gap: 6px; }
    .form-group label { font-size: 0.8rem; font-weight: 600; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.5px; }
    .readonly-field { background: var(--bg-tertiary); color: var(--text-muted); cursor: default; font-style: italic; }
    .hint { margin: 4px 0 0; font-size: 0.75rem; color: var(--text-muted); }

    .empty-state { text-align: center; padding: 48px; }
    .empty-content { display: flex; flex-direction: column; align-items: center; gap: 12px; color: var(--text-muted); }
    .empty-content span { font-size: 2.5rem; }
  `]
})
export class StaffComponent implements OnInit {
  staffList: any[] = [];
  filteredStaff: any[] = [];
  stores: any[] = [];
  isLoading = true;
  isLoadingStores = false;
  isSaving = false;
  showModal = false;
  modalTitle = 'Assign Role & Store';
  selectedUser: any = null;

  searchQuery: string = '';
  currentPage: number = 1;
  itemsPerPage: number = 5;

  get totalPages(): number {
    return Math.ceil(this.filteredStaff.length / this.itemsPerPage) || 1;
  }

  get paginatedStaff(): any[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredStaff.slice(start, start + this.itemsPerPage);
  }

  applyFilters() {
    if (!this.searchQuery) {
      this.filteredStaff = [...this.staffList];
    } else {
      const q = this.searchQuery.toLowerCase();
      this.filteredStaff = this.staffList.filter(u => 
        (u.fullName || '').toLowerCase().includes(q) || 
        (u.email || '').toLowerCase().includes(q)
      );
    }
    this.currentPage = 1; // reset to 1 on filter
  }

  assignForm = {
    userId: 0,
    storeId: 0,
    role: 'Cashier'
  };

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadStaff();
    this.loadStores();
  }

  loadStaff() {
    this.isLoading = true;
    this.api.getStaffManagement().subscribe({
      next: (users: any[]) => {
        this.staffList = users;
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Error loading staff', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadStores() {
    this.isLoadingStores = true;
    this.api.getStores().subscribe({
      next: (stores: any[]) => {
        this.stores = stores;
        this.isLoadingStores = false;
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingStores = false; }
    });
  }

  openEditModal(user: any) {
    this.selectedUser = user;
    this.modalTitle = `Assign Role & Store`;
    this.assignForm = {
      userId: user.userId,
      storeId: user.assignedStoreId || 0,
      role: user.assignedRole || 'Cashier'
    };
    this.showModal = true;
  }

  openPendingModal() {
    // Opens modal for first unassigned user, or blank
    const pending = this.staffList.find(u => !u.isAssigned);
    if (pending) {
      this.openEditModal(pending);
    } else {
      alert('No pending (unassigned) staff members found.');
    }
  }

  closeModal() {
    this.showModal = false;
    this.selectedUser = null;
  }

  saveAssignment() {
    this.isSaving = true;
    const dto = {
      userId: this.assignForm.userId,
      storeId: this.assignForm.role === 'Admin' ? 0 : this.assignForm.storeId,
      role: this.assignForm.role
    };

    this.api.assignStaff(dto).subscribe({
      next: (res: any) => {
        alert(res.message || 'Assignment saved successfully!');
        this.closeModal();
        this.loadStaff();
        this.isSaving = false;
      },
      error: (err: any) => {
        alert('Error saving assignment: ' + (err.error?.message || err.message || 'Server error'));
        this.isSaving = false;
      }
    });
  }

  unassignStaff(user: any) {
    if (!confirm(`Unassign ${user.fullName || user.email}? They will lose their current role.`)) return;
    this.api.assignStaff({ userId: user.userId, storeId: 0, role: 'Cashier' }).subscribe({
      next: () => {
        this.loadStaff();
      },
      error: (err: any) => alert('Error unassigning: ' + (err.error?.message || 'Server error'))
    });
  }

  getRoleClass(role?: string): string {
    if (!role) return 'role-default';
    const r = role.toLowerCase();
    if (r.includes('admin')) return 'role-admin';
    if (r.includes('manager')) return 'role-manager';
    return 'role-staff';
  }
}
