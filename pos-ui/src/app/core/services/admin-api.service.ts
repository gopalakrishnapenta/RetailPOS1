import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Store, DashboardStats } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AdminApiService {
  private readonly baseUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getAdminStores(): Observable<Store[]> {
    return this.http.get<Store[]>(`${this.baseUrl}/stores/all`);
  }

  createStore(store: Partial<Store>): Observable<Store> {
    return this.http.post<Store>(`${this.baseUrl}/stores`, store);
  }

  updateStore(id: number, store: Partial<Store>): Observable<Store> {
    return this.http.put<Store>(`${this.baseUrl}/stores/${id}`, store);
  }

  deleteStore(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/stores/${id}`);
  }

  getStaffManagement(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/staff`);
  }

  assignStaff(dto: { userId: number; storeId: number; role: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/staff/assign`, dto);
  }

  reSyncStaff(): Observable<any> {
    return this.http.post(`${this.baseUrl}/staff/sync`, {});
  }

  getDashboardStats(storeId?: number): Observable<DashboardStats> {
    let url = `${this.baseUrl}/dashboard/stats`;
    if (storeId && storeId !== 0) {
      url += `?storeId=${storeId}`;
    }
    return this.http.get<DashboardStats>(url);
  }

  getReports(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/reports`);
  }

  getInventory(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/inventory`);
  }

  adjustInventory(adjustment: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/inventory/adjust`, adjustment);
  }

  // Returns management (Admin view)
  getReturns(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/returns`);
  }

  approveReturn(id: number, note: string = ''): Observable<any> {
    return this.http.post(`${environment.apiUrl}/returns/${id}/approve`, { note });
  }

  rejectReturn(id: number, note: string = ''): Observable<any> {
    return this.http.post(`${environment.apiUrl}/returns/${id}/reject`, { note });
  }
}
