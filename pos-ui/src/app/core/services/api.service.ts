import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { StoreContextService } from './store-context.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = 'http://localhost:5000/gateway';

  constructor(
    private http: HttpClient, 
    private storeContext: StoreContextService,
    private auth: AuthService
  ) {}

  private getHeaders() {
    const token = localStorage.getItem('pos_token');
    let headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    // Add Store Override for Admin
    if (this.auth.getRole() === 'Admin') {
      const selectedStoreId = this.storeContext.getStoreId();
      // Explicitly send '0' for All Stores if null, else send the specific store ID
      const headerVal = selectedStoreId !== null ? selectedStoreId.toString() : '0';
      headers = headers.set('X-Store-Id', headerVal);
    }
    
    return { headers };
  }

  searchProducts(query: string) {
    return this.http.get<any[]>(`${this.baseUrl}/catalog/products/search?q=${query}`, this.getHeaders());
  }

  addToCart(cart: any) {
    return this.http.post<any>(`${this.baseUrl}/orders/bills/cart/items`, cart, this.getHeaders());
  }

  getBillById(id: number) {
    return this.http.get<any>(`${this.baseUrl}/orders/bills/${id}`, this.getHeaders());
  }
  
  collectPayment(payment: any) {
    return this.http.post<any>(`${this.baseUrl}/payments/collect`, payment, this.getHeaders());
  }

  searchCustomer(mobile: string) {
    return this.http.get<any>(`${this.baseUrl}/orders/customers/${mobile}`, this.getHeaders());
  }

  createCustomer(customer: any) {
    return this.http.post<any>(`${this.baseUrl}/orders/customers`, customer, this.getHeaders());
  }

  finalizeBill(id: number) {
    return this.http.post<any>(`${this.baseUrl}/orders/bills/${id}/finalize`, {}, this.getHeaders());
  }
  
  getDashboard() {
    return this.http.get<any>(`${this.baseUrl}/admin/dashboard`, this.getHeaders());
  }
  
  getAdjustments(page: number = 1, pageSize: number = 5) {
    return this.http.get<any>(`${this.baseUrl}/admin/inventory/adjustments?page=${page}&pageSize=${pageSize}`, this.getHeaders());
  }

  getProducts() {
    return this.http.get<any[]>(`${this.baseUrl}/catalog/products`, this.getHeaders());
  }

  getInventory() {
    return this.getProducts();
  }

  getCategories() {
    return this.http.get<any[]>(`${this.baseUrl}/catalog/categories`, this.getHeaders());
  }

  adjustInventory(adjustment: any) {
    return this.http.post<any>(`${this.baseUrl}/admin/inventory/adjustments`, adjustment, this.getHeaders());
  }

  getSalesReport() {
    return this.http.get<any[]>(`${this.baseUrl}/admin/reports/sales`, this.getHeaders());
  }

  getTaxReport() {
    return this.http.get<any>(`${this.baseUrl}/admin/reports/tax`, this.getHeaders());
  }

  // --- Store Management (Admin Only) ---
  getStores() {
    return this.http.get<any[]>(`${this.baseUrl}/admin/stores`, this.getHeaders());
  }
  addStore(store: any) {
    return this.http.post<any>(`${this.baseUrl}/admin/stores`, store, this.getHeaders());
  }

  // --- Product Management ---
  createProduct(product: any) {
    return this.http.post<any>(`${this.baseUrl}/catalog/products`, product, this.getHeaders());
  }
  updateProduct(id: number, product: any) {
    return this.http.put<any>(`${this.baseUrl}/catalog/products/${id}`, product, this.getHeaders());
  }
  deleteProduct(id: number) {
    return this.http.delete<any>(`${this.baseUrl}/catalog/products/${id}`, this.getHeaders());
  }

  // --- Category Management (Admin Only) ---
  getAdminCategories() {
    return this.http.get<any[]>(`${this.baseUrl}/admin/categories`, this.getHeaders());
  }

  addCategory(category: any) {
    return this.http.post<any>(`${this.baseUrl}/admin/categories`, category, this.getHeaders());
  }

  updateCategory(id: number, category: any) {
    return this.http.put<any>(`${this.baseUrl}/admin/categories/${id}`, category, this.getHeaders());
  }

  deleteCategory(id: number) {
    return this.http.delete<any>(`${this.baseUrl}/admin/categories/${id}`, this.getHeaders());
  }

  // --- Return Management ---
  getReturns() {
    return this.http.get<any[]>(`${this.baseUrl}/returns`, this.getHeaders());
  }

  initiateReturn(payload: any) {
    return this.http.post<any>(`${this.baseUrl}/returns/initiate`, payload, this.getHeaders());
  }

  approveReturn(id: number, note?: string) {
    return this.http.post<any>(`${this.baseUrl}/returns/${id}/approve`, note ? `"${note}"` : null, {
      ...this.getHeaders(),
      headers: this.getHeaders().headers.set('Content-Type', 'application/json')
    });
  }

  rejectReturn(id: number, note?: string) {
    return this.http.post<any>(`${this.baseUrl}/returns/${id}/reject`, note ? `"${note}"` : null, {
      ...this.getHeaders(),
      headers: this.getHeaders().headers.set('Content-Type', 'application/json')
    });
  }

  getBillByNumber(billNumber: string) {
    return this.http.get<any>(`${this.baseUrl}/orders/bills/search/${billNumber}`, this.getHeaders());
  }
}
