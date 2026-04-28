import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-stores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-container">
      <header class="admin-header glass-panel">
        <div class="title-section">
          <h2>Store Management</h2>
          <p>Manage retail outlets and locations</p>
        </div>
        <div class="actions">
          <div class="search-box">
            <span class="search-icon">🔍</span>
            <input type="text" [(ngModel)]="searchQuery" (input)="applyFilters()" placeholder="Search Code or Name..." class="input-field search-input">
          </div>
          <button class="btn btn-primary" (click)="openModal()">+ Add Store</button>
        </div>
      </header>

      <section class="table-section glass-panel">
        <div class="table-container">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Store Name</th>
                <th>Location</th>
                <th>Status</th>
                <th class="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let s of paginatedStores" class="animate-fade-in" [class.inactive-row]="!s.isActive">
                <td><span class="badge-sku">{{ s.storeCode }}</span></td>
                <td><strong class="text-primary">{{ s.name }}</strong></td>
                <td>{{ s.location }}</td>
                <td>
                  <span class="badge" [class.badge-success]="s.isActive" [class.badge-danger]="!s.isActive">
                    {{ s.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="text-center">
                  <div class="action-buttons">
                    <button class="btn-icon edit" (click)="openModal(s)" title="Edit">
                      <i class="icon">✏️</i>
                    </button>
                    <button class="btn-icon delete" (click)="deleteStore(s.id)" title="Deactivate" *ngIf="s.isActive">
                      <i class="icon">🗑️</i>
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="!paginatedStores.length && !isLoading">
                <td colspan="5" class="empty-state">
                  <div class="empty-content">
                    <span>🏬</span>
                    <p>No stores found.</p>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- Pagination Controls -->
      <div class="pagination-controls glass-panel" *ngIf="filteredStores.length > itemsPerPage">
        <button class="btn btn-secondary" [disabled]="currentPage === 1" (click)="currentPage = currentPage - 1">Previous</button>
        <span class="page-info">Page {{ currentPage }} of {{ totalPages || 1 }}</span>
        <button class="btn btn-secondary" [disabled]="currentPage === totalPages" (click)="currentPage = currentPage + 1">Next</button>
      </div>

      <!-- Store Modal -->
      <div class="modal-overlay" *ngIf="showModal" (click)="closeModal()">
        <div class="modal-content glass-panel animate-slide-up" (click)="$event.stopPropagation()">
          <header class="modal-header">
            <h3>{{ isEdit ? 'Edit Store' : 'Add New Store' }}</h3>
            <button class="close-btn" (click)="closeModal()">×</button>
          </header>
          
          <form (submit)="saveStore()" #storeForm="ngForm">
            <div class="form-grid">
              <div class="form-group">
                <label>Store Code <span class="required-star">*</span></label>
                <input type="text" name="storeCode" [(ngModel)]="currentStore.storeCode" required class="input-field" placeholder="E.g. NY-001">
              </div>
              <div class="form-group">
                <label>Store Name <span class="required-star">*</span></label>
                <input type="text" name="name" [(ngModel)]="currentStore.name" required class="input-field" placeholder="E.g. Manhattan Central">
              </div>
              <div class="form-group" style="grid-column: span 2;">
                <label>Location / Address <span class="required-star">*</span></label>
                <input type="text" name="location" [(ngModel)]="currentStore.location" required class="input-field" placeholder="E.g. New York, NY">
              </div>
              <div class="form-group">
                <label class="checkbox-container">
                  <input type="checkbox" name="isActive" [(ngModel)]="currentStore.isActive">
                  <span class="checkmark"></span>
                  Is Active
                </label>
              </div>
            </div>

            <footer class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeModal()">Cancel</button>
              <button type="submit" class="btn btn-primary" [disabled]="!storeForm.form.valid || isSaving">
                {{ isSaving ? 'Saving...' : 'Save Store' }}
              </button>
            </footer>
          </form>
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

    .admin-table { width: 100%; border-collapse: collapse; text-align: left; }
    .admin-table th { padding: var(--spacing-md) var(--spacing-lg); background: var(--bg-tertiary); color: var(--text-secondary); font-weight: 600; font-size: 0.75rem; text-transform: uppercase; border-bottom: 1px solid var(--border-color); }
    .admin-table td { padding: var(--spacing-md) var(--spacing-lg); border-bottom: 1px solid var(--border-color); font-size: 0.875rem; vertical-align: middle; }
    
    .badge-sku { background: var(--bg-tertiary); color: var(--text-secondary); padding: 4px 8px; border-radius: var(--radius-sm); font-family: monospace; font-size: 0.75rem; border: 1px solid var(--border-color); }
    .badge { padding: 4px 10px; border-radius: var(--radius-full); font-size: 0.75rem; font-weight: 600; }
    .badge-success { background: rgba(34, 197, 94, 0.1); color: var(--accent-success); }
    .badge-danger { background: rgba(239, 68, 68, 0.1); color: var(--accent-danger); }
    
    .action-buttons { display: flex; gap: 8px; justify-content: center; }
    .btn-icon { width: 32px; height: 32px; border-radius: var(--radius-sm); border: 1px solid var(--border-color); background: white; cursor: pointer; display: flex; align-items: center; justify-content: center; }
    
    .inactive-row { opacity: 0.5; background-color: var(--bg-secondary); }
    .inactive-row td { color: var(--text-muted); }
    .inactive-row .badge-sku { background: transparent; }
    
    .modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.4); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-content { width: 100%; max-width: 500px; padding: 24px; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
    
    .checkbox-container { display: flex; align-items: center; gap: 8px; cursor: pointer; font-size: 0.875rem; }
    .required-star { color: var(--accent-danger); }
    .text-primary { color: var(--accent-primary); }
  `]
})
export class StoresComponent implements OnInit {
  stores: any[] = [];
  filteredStores: any[] = [];
  isLoading = true;
  isSaving = false;
  showModal = false;
  isEdit = false;

  searchQuery: string = '';
  currentPage: number = 1;
  itemsPerPage: number = 10;

  currentStore: any = this.resetStore();

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadStores();
  }

  resetStore() {
    return { storeCode: '', name: '', location: '', isActive: true };
  }

  get totalPages(): number {
    return Math.ceil(this.filteredStores.length / this.itemsPerPage) || 1;
  }

  get paginatedStores(): any[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredStores.slice(start, start + this.itemsPerPage);
  }

  applyFilters() {
    if (!this.searchQuery) {
      this.filteredStores = [...this.stores];
    } else {
      const q = this.searchQuery.toLowerCase();
      this.filteredStores = this.stores.filter(s => 
        (s.name || '').toLowerCase().includes(q) || 
        (s.storeCode || '').toLowerCase().includes(q) ||
        (s.location || '').toLowerCase().includes(q)
      );
    }
    this.currentPage = 1;
  }

  loadStores() {
    this.isLoading = true;
    this.api.getAdminStores().subscribe({
      next: (data) => {
        this.stores = data;
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading stores', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openModal(store?: any) {
    if (store) {
      this.isEdit = true;
      this.currentStore = { ...store };
    } else {
      this.isEdit = false;
      this.currentStore = this.resetStore();
    }
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }

  saveStore() {
    this.isSaving = true;
    const request = this.isEdit 
      ? this.api.updateStore(this.currentStore.id, this.currentStore)
      : this.api.createStore(this.currentStore);

    request.subscribe({
      next: () => {
        alert(this.isEdit ? 'Store updated!' : 'Store added!');
        this.loadStores();
        this.closeModal();
      },
      error: (err) => {
        alert('Error saving store: ' + (err.error?.message || 'Server error'));
      },
      complete: () => this.isSaving = false
    });
  }

  deleteStore(id: number) {
    if (confirm('Are you sure you want to deactivate this store?')) {
      this.api.deleteStore(id).subscribe({
        next: () => {
          alert('Store deactivated');
          this.loadStores();
        },
        error: (err) => alert('Error deactivating store')
      });
    }
  }
}
