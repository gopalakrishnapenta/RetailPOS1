import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './billing.component.html',
  styleUrls: []
})
export class BillingComponent implements OnInit, OnDestroy {
  products: any[] = [];
  categories: any[] = [];
  searchQuery = '';
  cartItems: any[] = [];
  activeCategory = 'All';
  originalProducts: any[] = [];
  storeContext: any = null;
  
  constructor(private api: ApiService, private router: Router, public auth: AuthService) {}

  private pollInterval: any = null;

  ngOnInit() {
    this.search();
    this.loadCategories();
    const storedContext = localStorage.getItem('storeContext');
    if (storedContext) {
      this.storeContext = JSON.parse(storedContext);
    }
    // Poll for catalog updates every 10 seconds
    this.pollInterval = setInterval(() => {
      // Only auto-refresh if search box is empty to avoid interrupting user search
      if (!this.searchQuery) {
        this.search();
      }
    }, 10000);
  }

  ngOnDestroy() {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  isManagerOrHigher(): boolean {
    const role = this.auth.getRole();
    return role === 'Admin' || role === 'Manager' || role === 'StoreManager';
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }

  loadCategories() {
    this.api.getCategories().subscribe(res => {
      this.categories = res;
    });
  }

  search() {
    this.api.searchProducts(this.searchQuery).subscribe(res => {
      this.originalProducts = res;
      this.applyFilters();
    });
  }

  filterByCategory(category: string) {
    this.activeCategory = category;
    this.applyFilters();
  }

  applyFilters() {
    if (this.activeCategory === 'All') {
      this.products = this.originalProducts;
    } else {
      this.products = this.originalProducts.filter(p => 
        p.category?.name?.toLowerCase() === this.activeCategory.toLowerCase()
      );
    }
  }

  addToCart(product: any) {
    const existing = this.cartItems.find(i => i.productId === product.id);
    if (existing) {
      this.incrementQuantity(existing);
    } else {
      this.cartItems.push({
        productId: product.id,
        productName: product.name,
        unitPrice: product.sellingPrice || product.mrp,
        quantity: 1,
        subTotal: product.sellingPrice || product.mrp
      });
    }
  }

  incrementQuantity(item: any) {
    item.quantity++;
    item.subTotal = item.quantity * item.unitPrice;
  }

  decrementQuantity(item: any) {
    if (item.quantity > 1) {
      item.quantity--;
      item.subTotal = item.quantity * item.unitPrice;
    } else {
      this.removeItem(item);
    }
  }

  removeItem(item: any) {
    this.cartItems = this.cartItems.filter(i => i !== item);
  }

  clearCart() {
    this.cartItems = [];
    this.customerMobile = '';
    this.customerName = '';
  }

  get cartSubTotal() {
    return this.cartItems.reduce((acc, item) => acc + item.subTotal, 0);
  }

  get taxAmount() {
    return this.cartSubTotal * 0.1; // 10% VAT/GST Mock
  }

  get cartTotal() {
    return this.cartSubTotal + this.taxAmount;
  }

  holdBill() {
    if (this.cartItems.length === 0) return;
    const bill = this.buildBill('Held');
    this.api.addToCart(bill).subscribe({
      next: () => {
        this.clearCart();
        alert('Bill has been put on hold.');
      },
      error: (err) => alert('Failed to hold bill: ' + (err.error?.message || 'Server error'))
    });
  }

  customerMobile = '';
  customerName = '';

  onMobileBlur() {
    if (this.customerMobile.length >= 10) {
      this.api.searchCustomer(this.customerMobile).subscribe({
        next: (c) => { if (c && c.name) this.customerName = c.name; },
        error: () => {} // Not found, ignore
      });
    }
  }

  proceedToPayment() {
    if (this.cartItems.length === 0) return;
    if (!this.customerMobile || !this.customerName) {
      alert('Please enter Customer Mobile and Name to proceed.');
      return;
    }

    const customerData = { 
      mobile: this.customerMobile, 
      name: this.customerName, 
      storeId: this.storeContext?.storeId || 1 
    };
    
    // Auto-save/update customer
    this.api.createCustomer(customerData).subscribe({
      next: () => {
        const bill = this.buildBill('Draft');
        this.api.addToCart(bill).subscribe({
          next: (response) => {
            this.router.navigate(['/pos/payment'], { state: { bill: response.bill } });
          },
          error: (err) => alert('Failed to create bill: ' + (err.error?.message || 'Check backend logs'))
        });
      },
      error: (err) => alert('Failed to sync customer: ' + (err.error?.message || 'Server error'))
    });
  }

  private buildBill(status: string) {
    return {
      billNumber: 'DRF-' + Date.now().toString(),
      customerMobile: this.customerMobile,
      storeId: this.storeContext?.storeId || 1, 
      storeCode: this.storeContext?.storeCode || 'S001',
      shiftDate: this.storeContext?.shiftDate || new Date().toISOString(),
      cashierId: parseInt(localStorage.getItem('userId') || '1'),
      totalAmount: this.cartTotal,
      taxAmount: this.taxAmount,
      status: status,
      items: this.cartItems
    };
  }
}
