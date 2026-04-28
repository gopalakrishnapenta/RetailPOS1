import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = 'http://localhost:5000/gateway';

  constructor(private http: HttpClient) {}

  // --- AUTH METHODS ---
  login(credentials: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/login`, credentials);
  }

  verifyLoginOtp(verifyDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/verify-login-otp`, verifyDto);
  }

  resendLoginOtp(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/resend-login-otp`, { email });
  }

  register(registerDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/register`, registerDto);
  }

  googleLogin(googleDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/google-login`, googleDto);
  }

  verifyEmail(verifyDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/verify-email`, verifyDto);
  }

  sendPasswordResetOtp(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/send-otp`, { email });
  }

  resetPassword(resetDto: { email: string; otp: string; newPassword: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/reset-password`, resetDto);
  }

  getStores(): Observable<any> {
    return this.http.get(`${this.baseUrl}/auth/stores`);
  }

  // --- ADMIN STORE MANAGEMENT ---
  getAdminStores(): Observable<any> {
    return this.http.get(`${this.baseUrl}/admin/stores/all`);
  }

  createStore(store: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/stores`, store);
  }

  updateStore(id: number, store: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/admin/stores/${id}`, store);
  }

  deleteStore(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/admin/stores/${id}`);
  }

  getUsers(): Observable<any> {
    return this.http.get(`${this.baseUrl}/auth/users`);
  }

  getStaffManagement(): Observable<any> {
    return this.http.get(`${this.baseUrl}/admin/staff`);
  }

  assignStaff(dto: { userId: number; storeId: number; role: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/staff/assign`, dto);
  }

  reSyncStaff(): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/staff/sync`, {});
  }

  // --- CATALOG METHODS ---
  getProducts(): Observable<any> {
    return this.http.get(`${this.baseUrl}/catalog/products`);
  }

  getAdminProductsAll(): Observable<any> {
    return this.http.get(`${this.baseUrl}/catalog/products/all`);
  }

  createProduct(product: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/catalog/products`, product);
  }

  updateProduct(id: number, product: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/catalog/products/${id}`, product);
  }

  deleteProduct(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/catalog/products/${id}`);
  }

  getCategories(): Observable<any> {
    return this.http.get(`${this.baseUrl}/catalog/categories`);
  }

  getAdminCategoriesAll(): Observable<any> {
    return this.http.get(`${this.baseUrl}/catalog/categories/all`);
  }

  createCategory(category: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/catalog/categories`, category);
  }

  updateCategory(id: number, category: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/catalog/categories/${id}`, category);
  }

  deleteCategory(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/catalog/categories/${id}`);
  }

  // --- ORDERS METHODS ---
  createBill(billData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/orders/bills`, billData);
  }

  finalizeBill(id: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/orders/bills/${id}/finalize`, {});
  }

  getBills(): Observable<any> {
    return this.http.get(`${this.baseUrl}/orders/bills`);
  }

  getBillDetails(id: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/orders/bills/${id}`);
  }

  holdBill(id: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/orders/bills/${id}/hold`, {});
  }

  // --- ADMIN METHODS ---
  getDashboardStats(storeId?: number): Observable<any> {
    let url = `${this.baseUrl}/admin/dashboard/stats`;
    if (storeId && storeId !== 0) {
      url += `?storeId=${storeId}`;
    }
    return this.http.get(url);
  }

  getReports(): Observable<any> {
    return this.http.get(`${this.baseUrl}/admin/reports`);
  }

  getInventory(): Observable<any> {
    return this.http.get(`${this.baseUrl}/admin/inventory`);
  }

  getReturns(): Observable<any> {
    return this.http.get(`${this.baseUrl}/returns`);
  }

  approveReturn(id: number, note: string = ''): Observable<any> {
    return this.http.post(`${this.baseUrl}/returns/${id}/approve`, { note });
  }

  rejectReturn(id: number, note: string = ''): Observable<any> {
    return this.http.post(`${this.baseUrl}/returns/${id}/reject`, { note });
  }
  adjustInventory(adjustment: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/inventory/adjust`, adjustment);
  }

  // --- RETURNS METHODS ---
  initiateReturn(returnData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/returns`, returnData);
  }

  // --- PAYMENT METHODS ---
  collectPayment(paymentData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/payments/collect`, paymentData);
  }

  createRazorpayOrder(request: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/payments/create-order`, request);
  }

  verifyRazorpayPayment(verifyData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/payments/verify`, verifyData);
  }

  chatWithAI(message: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/ai/chat`, { message });
  }
}
