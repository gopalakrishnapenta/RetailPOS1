import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './billing.component.html',
  styleUrl: './billing.component.css'
})
export class BillingComponent implements OnInit {
  // --- PROPERTIES ---
  products: any[] = [];
  categories: any[] = [];
  filteredProducts: any[] = [];
  
  selectedCategory: any = null;
  searchQuery: string = '';
  isLoading = true;
  isProcessing = false;

  userName: string = 'Admin';
  storeCode: string = 'S001';
  shiftDate: string = '26 Mar 2026';
  customerMobile: string = '';
  customerName: string = '';

  cart: any[] = [];
  subtotal = 0;
  tax = 0;
  total = 0;
  paymentMode: 'Cash' | 'Online' = 'Cash';

  constructor(private api: ApiService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadUserInfo();
    this.loadData();
    this.loadRazorpayScript();
  }

  // --- INITIALIZATION ---
  loadUserInfo() {
    try {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      this.userName = user.email || 'admin@gmail.com';
      this.storeCode = user.storeId ? `S00${user.storeId}` : 'S001';
      this.shiftDate = new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    } catch (e) {
      console.warn('Could not load user info', e);
    }
  }

  loadRazorpayScript() {
    if ((window as any).hasOwnProperty('Razorpay')) return;
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.async = true;
    document.body.appendChild(script);
  }

  loadData() {
    this.isLoading = true;
    this.api.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats;
        this.loadProducts();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading categories', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadProducts() {
    this.api.getProducts().subscribe({
      next: (prods) => {
        this.products = prods;
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

  // --- FILTERS & ACTIONS ---
  applyFilters() {
    const query = this.searchQuery?.toLowerCase().trim() || '';
    
    this.filteredProducts = this.products.filter(p => {
      const productName = p.name?.toLowerCase() || '';
      const productSku = p.sku?.toLowerCase() || '';
      
      const matchesSearch = query === '' || productName.includes(query) || productSku.includes(query);
      const matchesCategory = !this.selectedCategory || p.categoryId === this.selectedCategory.id;
      return matchesSearch && matchesCategory;
    });
  }

  selectCategory(cat: any) {
    this.selectedCategory = cat;
    this.applyFilters();
  }

  addToCart(product: any) {
    if (product.stockQuantity <= 0) {
      alert('This product is out of stock.');
      return;
    }

    const existing = this.cart.find(item => item.id === product.id);
    if (existing) {
      if (existing.quantity >= product.stockQuantity) {
        alert(`Cannot add more. Only ${product.stockQuantity} units in stock.`);
        return;
      }
      existing.quantity++;
    } else {
      this.cart.push({ ...product, quantity: 1 });
    }
    this.calculateTotals();
  }

  removeFromCart(index: number) {
    this.cart.splice(index, 1);
    this.calculateTotals();
  }

  updateQuantity(index: number, qty: number) {
    if (qty <= 0) {
      this.removeFromCart(index);
    } else {
      const item = this.cart[index];
      if (qty > item.stockQuantity) {
        alert(`Only ${item.stockQuantity} units available in stock.`);
        return;
      }
      item.quantity = qty;
      this.calculateTotals();
    }
  }

  calculateTotals() {
    this.subtotal = this.cart.reduce((sum, item) => sum + (item.sellingPrice * item.quantity), 0);
    this.tax = this.subtotal * 0.1; // 10% tax
    this.total = this.subtotal + this.tax;
  }

  holdBill() {
    if (this.cart.length === 0) return;
    alert('Bill has been put on hold.');
  }

  resetCart() {
    this.cart = [];
    this.customerMobile = '';
    this.customerName = '';
    this.calculateTotals();
    this.isProcessing = false;
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/login';
  }

  processPayment() {
    if (this.cart.length === 0 || !this.customerMobile) {
      alert('Please ensure cart items and customer mobile are present.');
      return;
    }

    this.isProcessing = true;
    const authData = JSON.parse(localStorage.getItem('user') || '{}');
    
    const billData = {
      billNumber: 'BN-' + Date.now(),
      customerName: this.customerName,
      customerMobile: this.customerMobile,
      totalAmount: this.total,
      taxAmount: this.tax,
      storeId: authData.storeId || 0,
      cashierId: authData.id || 0,
      status: 'PendingPayment',
      items: this.cart.map(item => ({
        productId: item.id,
        productName: item.name,
        quantity: item.quantity,
        unitPrice: item.sellingPrice,
        subTotal: item.sellingPrice * item.quantity
      }))
    };

    this.api.createBill(billData).subscribe({
      next: (billRes) => {
        // Essential Step: Finalize the bill to trigger the backend Checkout Saga before navigating to payment.
        const billId = billRes.id || billRes.Id || 0;
        if (billId === 0) {
          this.isProcessing = false;
          alert('Invalid Bill ID received.');
          return;
        }

        this.api.finalizeBill(billId).subscribe({
          next: () => {
            this.handlePaymentSuccess(billRes);
          },
          error: (finalizeErr) => {
            console.error('Finalization failure:', finalizeErr);
            this.isProcessing = false;
            alert('Checkout initiation failed. Please try again.');
          }
        });
      },
      error: (err) => {
        this.isProcessing = false;
        alert('Failed to initiate order: ' + (err.error?.message || 'Server error'));
      }
    });
  }

  handlePaymentSuccess(bill: any) {
    this.router.navigate(['/pos/payment', bill.id]);
  }

  newTransaction() {
    this.resetCart();
  }

  printBill() {
    window.print();
  }

}
