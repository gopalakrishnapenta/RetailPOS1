import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { StoreContextService } from '../../../../core/services/store-context.service';
import { ApiService } from '../../../../core/services/api.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="sidebar">
      <div class="sidebar-brand">
        <div class="brand-logo"></div>
        <h3>{{ getRoleHeader() }}</h3>
      </div>

      <!-- Store Switcher for Super Admin -->
      <div class="store-switcher" *ngIf="isAdmin()" style="padding: 0 1rem 1.5rem 1rem;">
        <label style="font-size: 0.75rem; text-transform: uppercase; color: #94a3b8; display: block; margin-bottom: 0.5rem;">View Data For Store:</label>
        <select 
          class="form-control" 
          style="background: #1e293b; color: white; border: 1px solid #334155; font-size: 0.875rem;"
          [(ngModel)]="selectedStoreId" 
          (change)="onStoreChange()">
          <option [ngValue]="null">Select Store (All)</option>
          <option *ngFor="let s of stores" [ngValue]="s.id">{{ s.name }} ({{ s.storeCode }})</option>
        </select>
      </div>
      <a routerLink="/admin/dashboard" [class.active]="active === 'dashboard'">📊 Dashboard</a>
      <a routerLink="/admin/stores" *ngIf="isAdmin()" [class.active]="active === 'stores'">🏢 Store Management</a>
      <a routerLink="/admin/categories" *ngIf="isAdmin()" [class.active]="active === 'categories'">🏷️ Categories</a>
      <a routerLink="/admin/inventory" *ngIf="isOnlyManager()" [class.active]="active === 'inventory'">📦 Inventory Control</a>
      <a routerLink="/admin/products" *ngIf="isOnlyManager()" [class.active]="active === 'products'">🏷️ Product Master</a>
      <a routerLink="/admin/returns" *ngIf="isManagerOrHigher()" [class.active]="active === 'returns'">↩️ Returns Queue</a>
      <a routerLink="/admin/reports" *ngIf="isManagerOrHigher()" [class.active]="active === 'reports'">📈 Reports</a>

      <div style="margin-top: auto; padding-top: 2rem; border-top: 1px solid var(--panel-border);">
        <a routerLink="/pos/billing" class="btn-sidebar-switch">🖥️ Open POS Terminal</a>
        <button (click)="logout()" class="btn btn-secondary btn-block" style="margin-top: 1rem;">Logout</button>
      </div>
    </div>
  `
})
export class AdminSidebarComponent {
  @Input() active: string = '';
  stores: any[] = [];
  selectedStoreId: number | null = null;

  constructor(
    private auth: AuthService, 
    private router: Router, 
    private storeContext: StoreContextService,
    private api: ApiService
  ) {}

  ngOnInit() {
    this.selectedStoreId = this.storeContext.getStoreId();
    if (this.isAdmin()) {
      this.loadStores();
    }
  }

  loadStores() {
    this.api.getStores().subscribe(res => {
      this.stores = res;
    });
  }

  onStoreChange() {
    this.storeContext.setStoreId(this.selectedStoreId);
    // Reload current route to refresh data
    const currentUrl = this.router.url;
    this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
      this.router.navigate([currentUrl]);
    });
  }

  isAdmin(): boolean {
    return this.auth.getRole() === 'Admin';
  }

  isManagerOrHigher(): boolean {
    const role = this.auth.getRole();
    return role === 'Admin' || role === 'Manager' || role === 'StoreManager';
  }

  isOnlyManager(): boolean {
    const role = this.auth.getRole();
    return role === 'Manager' || role === 'StoreManager';
  }

  getRoleHeader(): string {
    const role = this.auth.getRole();
    if (role === 'Admin') return 'Super Admin';
    if (role === 'StoreManager') return 'Store Manager';
    if (role === 'Manager') return 'Manager';
    return 'Staff';
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
