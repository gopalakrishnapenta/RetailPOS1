import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-container">
      <header class="admin-header glass-panel">
        <div class="title-section">
          <h2>Inventory Management</h2>
          <p>Monitor and track stock levels across the store</p>
        </div>
        <div class="actions">
          <div class="search-box">
            <span class="search-icon">🔍</span>
            <input type="text" [(ngModel)]="searchQuery" (input)="applyFilters()" placeholder="Search SKU or Name..." class="input-field search-input">
          </div>
          <button class="btn btn-secondary" (click)="syncFromCatalog()" [disabled]="isLoading">
            {{ isLoading ? 'Checking...' : '🔄 Sync Catalog' }}
          </button>
        </div>
      </header>


      <section class="table-section glass-panel">
        <div class="table-container">
          <table class="admin-table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Product Name</th>
                <th>Category</th>
                <th>Stock Level</th>
                <th>Price</th>
                <th>Status</th>
                <th class="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of filteredInventory" class="animate-fade-in">
                <td><span class="badge-sku">{{ item.sku }}</span></td>
                <td>
                  <div class="product-info">
                    <span class="p-name">{{ item.name }}</span>
                  </div>
                </td>
                <td><span class="badge-tag">{{ item.categoryName || 'General' }}</span></td>
                <td>
                  <div class="stock-display" [class.low-stock]="item.stockQuantity < 10">
                    <span class="stock-count">{{ item.stockQuantity }}</span>
                    <span class="stock-unit">Units</span>
                  </div>
                </td>
                <td class="text-muted">{{ item.sellingPrice | currency:'INR' }}</td>
                <td>
                  <span class="badge" [ngClass]="item.stockQuantity > 0 ? 'success' : 'danger'">
                    {{ item.stockQuantity > 0 ? 'In Stock' : 'Out of Stock' }}
                  </span>
                </td>
                <td class="text-center">
                  <button class="btn-icon edit" title="Adjust Stock" (click)="openAdjustModal(item)">
                    <i class="icon">⚙️</i>
                  </button>
                </td>
              </tr>
              <tr *ngIf="!filteredInventory.length && !isLoading">
                <td colspan="7" class="empty-state">
                  <div class="empty-content">
                    <span>📦</span>
                    <p>No matching inventory found.</p>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- Adjustment Modal -->
      <div class="modal-overlay" *ngIf="showAdjustmentModal" (click)="closeModal()">
        <div class="modal-card glass-panel animate-slide-up" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div class="modal-icon">⚙️</div>
            <div class="modal-title-wrap">
              <h3>Stock Adjustment</h3>
              <p>Adjusting units for <strong>{{ selectedItem?.name }}</strong></p>
            </div>
          </div>

          <div class="modal-body">
            <div class="form-grid">
              <div class="form-group">
                <label>Current Stock</label>
                <div class="static-val">{{ selectedItem?.stockQuantity }} Units</div>
              </div>
              <div class="form-group">
                <label>Adjustment Type</label>
                <div class="type-selector">
                  <button [class.active]="adjType === 'add'" (click)="adjType = 'add'">(+) Add</button>
                  <button [class.active]="adjType === 'remove'" (click)="adjType = 'remove'">(-) Remove</button>
                </div>
              </div>
            </div>

            <div class="form-group mt-16">
              <label>Quantity to {{ adjType === 'add' ? 'Add' : 'Remove' }}</label>
              <input type="number" [(ngModel)]="adjQty" class="input-field main-input" placeholder="Enter amount...">
            </div>

            <div class="form-group mt-16">
              <label>Reason for Adjustment</label>
              <select [(ngModel)]="adjReason" class="input-field">
                <option value="Inward">Inward (Restock)</option>
                <option value="Return">Return (Customer Return)</option>
                <option value="Damage">Damage (Broken/Expired)</option>
                <option value="Shrinkage">Shrinkage (Loss/Theft)</option>
                <option value="Correction">Data Correction</option>
              </select>
            </div>

            <div class="form-group mt-16">
              <label>Reference # / Doc ID</label>
              <input type="text" [(ngModel)]="adjRef" class="input-field" placeholder="e.g. PO-12345">
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn-cancel" (click)="closeModal()">Cancel</button>
            <button class="btn-primary btn-save" (click)="saveAdjustment()" [disabled]="isSaving || !adjQty">
              {{ isSaving ? 'Updating...' : 'Confirm Adjustment' }}
            </button>
          </div>
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
    
    .badge-sku { background: var(--bg-tertiary); color: var(--text-secondary); padding: 4px 8px; border-radius: var(--radius-sm); font-family: monospace; font-size: 0.75rem; font-weight: 600; border: 1px solid var(--border-color); }
    .badge-tag { background: rgba(59, 130, 246, 0.1); color: #3b82f6; padding: 4px 10px; border-radius: var(--radius-full); font-size: 0.75rem; font-weight: 600; }
    
    .product-info { font-weight: 600; color: var(--text-primary); }
    
    .stock-display { display: flex; flex-direction: column; }
    .stock-count { font-weight: 700; color: var(--accent-success); font-size: 1rem; }
    .stock-unit { font-size: 10px; text-transform: uppercase; color: var(--text-muted); margin-top: -4px; }
    .stock-display.low-stock .stock-count { color: var(--accent-danger); }

    .badge.success { background: #ecfdf5; color: #059669; }
    .badge.danger { background: #fef2f2; color: #dc2626; }
    .badge { padding: 4px 12px; border-radius: var(--radius-full); font-size: 0.75rem; font-weight: 600; }

    .btn-icon { width: 32px; height: 32px; border-radius: var(--radius-sm); border: 1px solid var(--border-color); background: white; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
    .btn-icon:hover { background: var(--bg-tertiary); border-color: var(--accent-primary); color: var(--accent-primary); }

    .text-center { text-align: center; }
    .text-muted { color: var(--text-muted); }

    /* Modal Styles */
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 2000; }
    .modal-card { width: 450px; padding: 32px; border-radius: var(--radius-lg); }
    .modal-header { display: flex; gap: 16px; margin-bottom: 24px; }
    .modal-icon { width: 48px; height: 48px; background: var(--bg-tertiary); border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; font-size: 24px; }
    .modal-title-wrap h3 { margin: 0; font-size: 1.25rem; font-weight: 700; }
    .modal-title-wrap p { margin: 4px 0 0; color: var(--text-secondary); font-size: 0.875rem; }
    
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .mt-16 { margin-top: 16px; }
    label { display: block; font-size: 0.75rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 6px; letter-spacing: 0.5px; }
    .static-val { padding: 10px 14px; background: var(--bg-tertiary); border-radius: var(--radius-sm); font-weight: 600; }
    
    .type-selector { display: flex; border: 1px solid var(--border-color); border-radius: var(--radius-sm); overflow: hidden; }
    .type-selector button { flex: 1; border: none; padding: 10px; background: transparent; cursor: pointer; font-size: 0.875rem; font-weight: 600; color: var(--text-secondary); }
    .type-selector button.active { background: var(--accent-primary); color: white; }

    .main-input { font-size: 1.5rem !important; font-weight: 700 !important; text-align: center; height: 60px !important; }
    .modal-footer { display: flex; justify-content: flex-end; gap: 12px; margin-top: 32px; }
    .btn-cancel { padding: 10px 20px; border: 1px solid var(--border-color); background: transparent; border-radius: var(--radius-sm); font-weight: 600; cursor: pointer; }
    .btn-save { padding: 10px 24px; border: none; background: var(--accent-primary); color: white; border-radius: var(--radius-sm); font-weight: 600; cursor: pointer; }
    .btn-save:disabled { opacity: 0.5; cursor: not-allowed; }

    @keyframes slideUp { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
    .animate-slide-up { animation: slideUp 0.3s ease-out; }
  `]
})
export class InventoryComponent implements OnInit {
  inventoryItems: any[] = [];
  filteredInventory: any[] = [];
  searchQuery: string = '';
  isLoading = true;

  // Modal State
  showAdjustmentModal = false;
  selectedItem: any = null;
  adjType: 'add' | 'remove' = 'add';
  adjQty: number | null = null;
  adjReason = 'Correction';
  adjRef = '';
  isSaving = false;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadInventory();
  }

  loadInventory() {
    this.isLoading = true;
    this.api.getProducts().subscribe({
      next: (items) => {
        this.inventoryItems = items.map((i: any) => ({
          ...i,
          sku: i.sku || i.productSku || 'SKU-0000',
          name: i.name || i.productName || 'Unnamed Product'
        }));
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading inventory', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    if (!this.searchQuery) {
      this.filteredInventory = [...this.inventoryItems];
      return;
    }
    const query = this.searchQuery.toLowerCase();
    this.filteredInventory = this.inventoryItems.filter(item => 
      (item.name || '').toLowerCase().includes(query) || 
      (item.sku || '').toLowerCase().includes(query)
    );
  }

  openAdjustModal(item: any) {
    this.selectedItem = item;
    this.showAdjustmentModal = true;
    this.adjQty = null;
    this.adjType = 'add';
    this.adjReason = 'Inward';
    this.adjRef = '';
  }

  closeModal() {
    this.showAdjustmentModal = false;
    this.selectedItem = null;
  }

  saveAdjustment() {
    if (!this.selectedItem || !this.adjQty) return;
    
    this.isSaving = true;
    const finalQty = this.adjType === 'add' ? Math.abs(this.adjQty) : -Math.abs(this.adjQty);
    
    const dto = {
      productId: this.selectedItem.id,
      quantity: finalQty,
      reasonCode: this.adjReason,
      documentReference: this.adjRef,
      adjustmentDate: new Date()
    };

    this.api.adjustInventory(dto).subscribe({
      next: () => {
        // Success feedback
        alert(`Successfully adjusted stock for ${this.selectedItem.name}!`);
        
        this.closeModal();
        
        // Give the backend async event bus a moment to process the adjustment
        // before we re-fetch the product list from CatalogService.
        setTimeout(() => {
          this.loadInventory(); 
          this.isSaving = false;
          this.cdr.detectChanges();
        }, 800);
      },
      error: (err) => {
        console.error('Adjustment failed', err);
        alert('Failed to adjust stock: ' + (err.error?.message || 'Unknown error'));
        this.isSaving = false;
        this.cdr.detectChanges();
      }
    });
  }

  syncFromCatalog() {
    this.isLoading = true;
    this.api.getProducts().subscribe({
      next: (prods) => {
        console.log('Synced products from catalog', prods);
        this.loadInventory();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Sync failed', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
}

