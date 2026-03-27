import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AdminSidebarComponent],
  templateUrl: './inventory.component.html',
  styleUrls: []
})
export class InventoryComponent implements OnInit, OnDestroy {
  inventory: any[] = [];
  originalInventory: any[] = [];
  adjustments: any[] = [];
  searchQuery = '';
  products: any[] = [];
  isLoading = false;
  kpis: any = null;
  private pollInterval: any = null;

  currentPage = 1;
  pageSize = 5;
  totalAdjustments = 0;
  totalPages = 0;

  newAdj = {
    productId: 0,
    storeId: 1,
    quantity: 0,
    reasonCode: 'Inward',
    documentReference: '',
    adjustedByUserId: 1
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
    this.loadInventory();
    this.loadAdjustmentsAndKpis();
    // Auto-refresh every 10 seconds
    this.pollInterval = setInterval(() => {
      this.loadProducts();
      this.loadInventory();
      this.loadAdjustmentsAndKpis();
    }, 10000);
  }

  ngOnDestroy() {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  loadProducts() {
    // Call CatalogService directly to avoid gateway auth issues
    const token = localStorage.getItem('pos_token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.get<any[]>('http://localhost:5002/api/products', { headers }).subscribe({
      next: (prods) => { this.products = prods; },
      error: (err) => { console.error('Failed to load products', err); }
    });
  }

  loadInventory() {
    this.api.getInventory().subscribe(res => {
      this.originalInventory = res;
      this.searchInventory();
    });
  }

  searchInventory() {
    if (!this.searchQuery) {
      this.inventory = this.originalInventory;
    } else {
      const q = this.searchQuery.toLowerCase();
      this.inventory = this.originalInventory.filter(item =>
        item.name.toLowerCase().includes(q) ||
        item.sku.toLowerCase().includes(q)
      );
    }
  }

  loadAdjustmentsAndKpis() {
    this.api.getAdjustments(this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        this.adjustments = res.items;
        this.totalAdjustments = res.totalCount;
        this.totalPages = res.totalPages;
      },
      error: (err) => { console.error('Failed to load adjustments', err); }
    });
    this.api.getDashboard().subscribe({
      next: (k) => { this.kpis = k; },
      error: (err) => { console.error('Failed to load kpis', err); }
    });
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadAdjustmentsAndKpis();
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadAdjustmentsAndKpis();
    }
  }

  loadData() {
    this.loadProducts();
    this.loadAdjustmentsAndKpis();
  }

  onSubmit() {
    if (this.newAdj.productId === 0 || this.newAdj.quantity === 0) return;
    this.api.adjustInventory(this.newAdj).subscribe({
      next: () => {
        this.loadInventory();
        this.loadAdjustmentsAndKpis();
        this.newAdj.quantity = 0;
        this.newAdj.documentReference = '';
        alert('Stock adjustment successful');
      },
      error: (err) => { alert('Failed to post adjustment: ' + err.message); }
    });
  }

  getProductName(id: number) {
    return this.products.find(p => p.id === id)?.name || 'Unknown Item';
  }
}
