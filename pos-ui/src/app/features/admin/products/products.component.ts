import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-container">
      <header class="admin-header glass-panel">
        <div class="title-section">
          <h2>Product Master</h2>
          <p>Maintain the central product catalog</p>
        </div>
        <div class="actions">
          <div class="search-box">
            <span class="search-icon">🔍</span>
            <input type="text" [(ngModel)]="searchQuery" (input)="applyFilters()" placeholder="Search SKU or Name..." class="input-field search-input">
          </div>
          <button class="btn btn-primary" (click)="openModal()">+ Create Product</button>
        </div>
      </header>

      <section class="table-section glass-panel">
        <div class="table-container">
          <table class="admin-table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Name</th>
                <th>Category</th>
                <th>Stock</th>
                <th>MRP</th>
                <th>Selling Price</th>
                <th class="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of paginatedProducts" class="animate-fade-in">
                <td><span class="badge-sku">{{ p.sku }}</span></td>
                <td>
                  <div class="product-info">
                    <span class="p-name">{{ p.name }}</span>
                    <span class="p-barcode" *ngIf="p.barcode">#{{ p.barcode }}</span>
                  </div>
                </td>
                <td><span class="badge-category">{{ p.categoryName || 'Uncategorized' }}</span></td>
                <td>
                  <div class="stock-info" [class.low-stock]="p.stockQuantity <= p.reorderLevel">
                    {{ p.stockQuantity }}
                  </div>
                </td>
                <td class="text-muted">{{ p.mrp | currency:'INR' }}</td>
                <td><strong class="text-primary">{{ p.sellingPrice | currency:'INR' }}</strong></td>
                <td class="text-center">
                  <div class="action-buttons">
                    <button class="btn-icon edit" (click)="openModal(p)" title="Edit">
                      <i class="icon">✏️</i>
                    </button>
                    <button class="btn-icon delete" (click)="deleteProduct(p.id)" title="Delete">
                      <i class="icon">🗑️</i>
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="!paginatedProducts.length && !isLoading">
                <td colspan="7" class="empty-state">
                  <div class="empty-content">
                    <span>🚫</span>
                    <p>No products found in catalog.</p>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- Pagination Controls -->
      <div class="pagination-controls glass-panel" *ngIf="filteredProducts.length > itemsPerPage">
        <button class="btn btn-secondary" [disabled]="currentPage === 1" (click)="currentPage = currentPage - 1">Previous</button>
        <span class="page-info">Page {{ currentPage }} of {{ totalPages || 1 }}</span>
        <button class="btn btn-secondary" [disabled]="currentPage === totalPages" (click)="currentPage = currentPage + 1">Next</button>
      </div>

      <!-- Simple Modal for Create/Edit -->
      <div class="modal-overlay" *ngIf="showModal" (click)="closeModal()">
        <div class="modal-content glass-panel animate-slide-up" (click)="$event.stopPropagation()">
          <header class="modal-header">
            <h3>{{ isEdit ? 'Edit Product' : 'Create New Product' }}</h3>
            <button class="close-btn" (click)="closeModal()">×</button>
          </header>
          
          <form (submit)="saveProduct()" #productForm="ngForm">
            <div class="form-grid">
              <div class="form-group">
                <label>SKU (Barcode/Unique ID)</label>
                <input type="text" name="sku" [(ngModel)]="currentProduct.sku" required class="input-field" placeholder="E.g. PRD-001">
              </div>
              <div class="form-group">
                <label>Product Name</label>
                <input type="text" name="name" [(ngModel)]="currentProduct.name" required class="input-field" placeholder="E.g. Premium Coffee">
              </div>
              <div class="form-group">
                <label>Category</label>
                <select name="categoryId" [(ngModel)]="currentProduct.categoryId" required class="input-field">
                  <option [ngValue]="null">Select Category</option>
                  <option *ngFor="let c of categories" [ngValue]="c.id">{{ c.name }}</option>
                </select>
              </div>
              <div class="form-group">
                <label>Barcode (Optional)</label>
                <input type="text" name="barcode" [(ngModel)]="currentProduct.barcode" class="input-field">
              </div>
              <div class="form-group">
                <label>MRP (Max Retail Price)</label>
                <input type="number" name="mrp" [(ngModel)]="currentProduct.mrp" required class="input-field">
              </div>
              <div class="form-group">
                <label>Selling Price</label>
                <input type="number" name="sellingPrice" [(ngModel)]="currentProduct.sellingPrice" required class="input-field">
              </div>
              <div class="form-group">
                <label>Stock Quantity</label>
                <input type="number" name="stockQuantity" [(ngModel)]="currentProduct.stockQuantity" required class="input-field">
              </div>
              <div class="form-group">
                <label>Reorder Level</label>
                <input type="number" name="reorderLevel" [(ngModel)]="currentProduct.reorderLevel" required class="input-field">
              </div>
            </div>

            <footer class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeModal()">Cancel</button>
              <button type="submit" class="btn btn-primary" [disabled]="!productForm.form.valid || isSaving">
                {{ isSaving ? 'Saving...' : 'Save Product' }}
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

    .pagination-controls { display: flex; justify-content: center; align-items: center; gap: 16px; padding: var(--spacing-md); margin-top: -10px; }
    .page-info { font-weight: 600; color: var(--text-secondary); font-size: 0.875rem; }

    .admin-table { width: 100%; border-collapse: collapse; text-align: left; }
    .admin-table th { padding: var(--spacing-md) var(--spacing-lg); background: var(--bg-tertiary); color: var(--text-secondary); font-weight: 600; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid var(--border-color); }
    .admin-table td { padding: var(--spacing-md) var(--spacing-lg); border-bottom: 1px solid var(--border-color); font-size: 0.875rem; vertical-align: middle; }
    .admin-table tr:hover { background: rgba(248, 250, 252, 0.5); }
    
    .badge-sku { background: var(--bg-tertiary); color: var(--text-secondary); padding: 4px 8px; border-radius: var(--radius-sm); font-family: monospace; font-size: 0.75rem; font-weight: 600; border: 1px solid var(--border-color); }
    .badge-category { background: rgba(37, 99, 235, 0.1); color: var(--accent-primary); padding: 4px 10px; border-radius: var(--radius-full); font-size: 0.75rem; font-weight: 600; }
    
    .product-info { display: flex; flex-direction: column; }
    .p-name { font-weight: 600; color: var(--text-primary); }
    .p-barcode { font-size: 0.75rem; color: var(--text-muted); }
    
    .stock-info { font-weight: 700; color: var(--accent-success); }
    .stock-info.low-stock { color: var(--accent-danger); position: relative; }
    .stock-info.low-stock::after { content: 'Low'; position: absolute; top: -12px; left: 0; font-size: 10px; text-transform: uppercase; }
    
    .action-buttons { display: flex; gap: 8px; justify-content: center; }
    .btn-icon { width: 32px; height: 32px; border-radius: var(--radius-sm); border: 1px solid var(--border-color); background: white; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
    .btn-icon.edit:hover { background: var(--bg-tertiary); color: var(--accent-primary); border-color: var(--accent-primary); }
    .btn-icon.delete:hover { background: #fef2f2; color: var(--accent-danger); border-color: var(--accent-danger); }
    
    /* Modal Styles */
    .modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.4); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-content { width: 100%; max-width: 650px; padding: 24px; }
    .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid var(--border-color); }
    .close-btn { background: none; border: none; font-size: 24px; cursor: pointer; color: var(--text-muted); }
    
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
    .modal-footer { margin-top: 32px; display: flex; justify-content: flex-end; gap: 12px; padding-top: 16px; border-top: 1px solid var(--border-color); }
    
    .text-center { text-align: center; }
    .text-muted { color: var(--text-muted); }
    .text-primary { color: var(--accent-primary); }
  `]
})
export class ProductsComponent implements OnInit {
  products: any[] = [];
  filteredProducts: any[] = [];
  categories: any[] = [];
  isLoading = true;
  isSaving = false;
  showModal = false;
  isEdit = false;

  searchQuery: string = '';
  currentPage: number = 1;
  itemsPerPage: number = 5;

  get totalPages(): number {
    return Math.ceil(this.filteredProducts.length / this.itemsPerPage) || 1;
  }

  get paginatedProducts(): any[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredProducts.slice(start, start + this.itemsPerPage);
  }

  applyFilters() {
    if (!this.searchQuery) {
      this.filteredProducts = [...this.products];
    } else {
      const q = this.searchQuery.toLowerCase();
      this.filteredProducts = this.products.filter(p => 
        (p.name || '').toLowerCase().includes(q) || 
        (p.sku || '').toLowerCase().includes(q)
      );
    }
    this.currentPage = 1; // reset to 1 on filter
  }

  currentProduct: any = this.resetProduct();

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadProducts();
    this.loadCategories();
  }

  resetProduct() {
    return {
      sku: '',
      barcode: '',
      name: '',
      mrp: 0,
      sellingPrice: 0,
      taxCode: 'GST18',
      reorderLevel: 5,
      stockQuantity: 0,
      categoryId: null,
      isActive: true
    };
  }

  loadProducts() {
    this.isLoading = true;
    this.api.getProducts().subscribe({
      next: (data) => {
        this.products = data;
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading products', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadCategories() {
    this.api.getCategories().subscribe(data => this.categories = data);
  }

  openModal(product?: any) {
    if (product) {
      this.isEdit = true;
      this.currentProduct = { ...product };
    } else {
      this.isEdit = false;
      this.currentProduct = this.resetProduct();
    }
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }

  saveProduct() {
    this.isSaving = true;
    const request = this.isEdit 
      ? this.api.updateProduct(this.currentProduct.id, this.currentProduct)
      : this.api.createProduct(this.currentProduct);

    request.subscribe({
      next: () => {
        alert(this.isEdit ? 'Product updated!' : 'Product created!');
        this.loadProducts();
        this.closeModal();
      },
      error: (err) => {
        alert('Error saving product: ' + (err.error?.message || 'Server error'));
      },
      complete: () => this.isSaving = false
    });
  }

  deleteProduct(id: number) {
    if (confirm('Are you sure you want to delete this product?')) {
      this.api.deleteProduct(id).subscribe({
        next: () => {
          alert('Product deleted');
          this.loadProducts();
        },
        error: (err) => alert('Error deleting product')
      });
    }
  }
}
