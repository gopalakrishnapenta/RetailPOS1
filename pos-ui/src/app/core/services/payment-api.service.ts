import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PaymentRequest } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PaymentApiService {
  private readonly baseUrl = `${environment.apiUrl}/payments`;

  constructor(private http: HttpClient) {}

  collectPayment(paymentData: PaymentRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/collect`, paymentData);
  }

  createRazorpayOrder(request: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/create-order`, request);
  }

  verifyRazorpayPayment(verifyData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/verify`, verifyData);
  }
}
