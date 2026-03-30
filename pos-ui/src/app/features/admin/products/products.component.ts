import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AdminSidebarComponent],
  templateUrl: './products.component.html',
  styleUrls: []
})
export class ProductsComponent implements OnInit {
  products: any[] = [];
  categories: any[] = [];
  isEditing = false;
  skuPrefix = 'SKU';
  skuSuffix = '';

  newProduct = {
    id: 0,
    name: '',
    sku: '',
    barcode: '',
    mrp: 0,
    sellingPrice: 0,
    categoryId: 1,
    stockQuantity: 0,
    categoryName: '',
    taxCode: 'GST_18',
    reorderLevel: 5,
    isActive: true
  };

  constructor(private api: ApiService, private http: HttpClient, private auth: AuthService, private router: Router) { }

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }

  isAdmin(): boolean {
    return this.auth.getRole() === 'Admin';
  }

  isManagerOrHigher(): boolean {
    const role = this.auth.getRole();
    return role === 'Admin' || role === 'Manager' || role === 'StoreManager';
  }

  getRoleHeader(): string {
    const role = this.auth.getRole();
    if (role === 'Admin') return 'Super Admin';
    if (role === 'StoreManager') return 'Store Manager';
    if (role === 'Manager') return 'Manager';
    return 'Staff';
  }

  ngOnInit() {
    this.loadProducts();
    this.loadCategories();
  }

  loadCategories() {
    this.api.getCategories().subscribe(res => {
      this.categories = res;
    });
  }

  loadProducts() {
    this.api.searchProducts('').subscribe(res => {
      this.products = res;
    });
  }

  editProduct(p: any) {
    this.isEditing = true;
    this.newProduct = { ...p };
    if (this.newProduct.sku && this.newProduct.sku.includes('-')) {
      const parts = this.newProduct.sku.split('-');
      this.skuPrefix = parts[0];
      this.skuSuffix = parts[1];
    } else if (this.newProduct.sku) {
      // Handle old SKUs without dash
      this.skuPrefix = this.newProduct.sku.replace(/\d+$/, '') || 'SKU';
      this.skuSuffix = this.newProduct.sku.match(/\d+$/)?.[0] || '';
    } else {
      this.skuPrefix = 'SKU';
      this.skuSuffix = '';
    }
  }

  onCategoryChange() {
    const category = this.categories.find(c => c.id == this.newProduct.categoryId);
    if (!category) return;

    const name = category.name.toLowerCase();
    if (name.includes('elec')) this.skuPrefix = 'ELC';
    else if (name.includes('groc')) this.skuPrefix = 'GRC';
    else if (name.includes('bev')) this.skuPrefix = 'BEV';
    else if (name.includes('fruit')) this.skuPrefix = 'FRU';
    else if (name.includes('veg')) this.skuPrefix = 'VEG';
    else this.skuPrefix = name.substring(0, 3).toUpperCase();

    this.updateSku();
  }

  updateSku() {
    this.newProduct.sku = `${this.skuPrefix}-${this.skuSuffix}`;
  }

  deleteProduct(id: number) {
    this.api.deleteProduct(id).subscribe(() => {
      this.loadProducts();
    });
  }

  onSubmit() {
    if (this.isEditing) {
      this.api.updateProduct(this.newProduct.id, this.newProduct).subscribe(() => {
        this.loadProducts();
        this.resetForm();
      });
    } else {
      this.api.createProduct(this.newProduct).subscribe(() => {
        this.loadProducts();
        this.resetForm();
      });
    }
  }

  resetForm() {
    this.isEditing = false;
    this.skuPrefix = 'SKU';
    this.skuSuffix = '';
    this.newProduct = { id: 0, name: '', sku: '', barcode: '', mrp: 0, sellingPrice: 0, categoryId: this.categories[0]?.id || 1, stockQuantity: 0, categoryName: '', taxCode: 'GST_18', reorderLevel: 5, isActive: true };
  }
}
