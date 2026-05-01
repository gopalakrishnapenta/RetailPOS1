import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Bill } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class OrderApiService {
  private readonly baseUrl = `${environment.apiUrl}/orders`;

  constructor(private http: HttpClient) {}

  createBill(billData: Bill): Observable<Bill> {
    return this.http.post<Bill>(`${this.baseUrl}/bills`, billData);
  }

  finalizeBill(id: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/bills/${id}/finalize`, {});
  }

  getBills(): Observable<Bill[]> {
    return this.http.get<Bill[]>(`${this.baseUrl}/bills`);
  }

  getBillDetails(id: string): Observable<Bill> {
    return this.http.get<Bill>(`${this.baseUrl}/bills/${id}`);
  }

  holdBill(id: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/bills/${id}/hold`, {});
  }
}
