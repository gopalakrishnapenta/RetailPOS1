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
                  <button class="btn-icon edit" title="Adjust Stock">
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
  `]
})
export class InventoryComponent implements OnInit {
  inventoryItems: any[] = [];
  filteredInventory: any[] = [];
  searchQuery: string = '';
  isLoading = true;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadInventory();
  }

  loadInventory() {
    this.isLoading = true;
    this.api.getProducts().subscribe({
      next: (items) => {
        this.inventoryItems = items;
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
