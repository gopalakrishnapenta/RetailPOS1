import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { catchError, finalize, map, of, tap, timeout } from 'rxjs';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './payment.component.html',
  styleUrl: './payment.component.css'
})
export class PaymentComponent implements OnInit {
  billId: string = '';
  bill: any = null;
  isLoading = true;
  isProcessing = false;
  paymentSuccess = false;

  paymentMode: 'Cash' | 'Online' = 'Cash';
  tenderedAmount: number = 0;
  changeAmount: number = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.billId = params.get('id') || '';
      if (this.billId) {
        this.loadBillDetails();
      } else {
        this.router.navigate(['/pos/billing']);
      }
    });
    this.loadRazorpayScript();
  }

  loadBillDetails() {
    this.isLoading = true;
    this.bill = null;
    console.log('[Payment] Loading bill id:', this.billId);
    this.api.getBillDetails(this.billId).pipe(
      timeout({ each: 8000 }),
      tap(raw => console.log('[Payment] Raw bill payload:', raw)),
      map(raw => this.normalizeBill(raw)),
      tap(bill => {
        if (!bill || !bill.id) {
          throw new Error('Bill payload is missing or invalid.');
        }
        this.bill = bill;
        this.tenderedAmount = this.bill.totalAmount || 0;
        this.calculateChange();
      }),
      catchError((err) => {
        console.error('Error loading bill', err);
        alert('Failed to load bill details. Returning to terminal.');
        this.router.navigate(['/pos/billing']);
        return of(null);
      }),
      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
        console.log('[Payment] Loading complete. isLoading:', this.isLoading, 'bill:', this.bill);
      })
    ).subscribe();
  }

  calculateChange() {
    this.changeAmount = Math.max(0, this.tenderedAmount - (this.bill?.totalAmount || 0));
  }

  processPayment() {
    if (this.isProcessing) return;
    this.isProcessing = true;

    if (this.paymentMode === 'Cash') {
      this.handleCashPayment();
    } else {
      this.handleOnlinePayment();
    }
  }

  handleCashPayment() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const paymentData = {
      billId: this.bill.id,
      amount: this.bill.totalAmount,
      paymentMode: 'Cash',
      storeId: user.storeId,
      referenceNumber: 'CASH-' + Date.now()
    };

    console.log('Initiating cash payment collection for bill:', this.bill.id);
    this.api.collectPayment(paymentData).subscribe({
      next: (res) => {
        console.log('Payment collection successful:', res);
        this.onPaymentSuccess('Cash', paymentData.referenceNumber);
      },
      error: (err) => {
        console.error('Payment collection error response:', err);
        this.isProcessing = false;
        alert('Cash payment collection failed: ' + (err.error?.message || 'Server error'));
      }
    });
  }

  handleOnlinePayment() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const orderRequest = {
      amount: this.bill.totalAmount,
      currency: 'INR',
      receipt: 'bill_' + this.bill.id
    };

    this.api.createRazorpayOrder(orderRequest).subscribe({
      next: (orderRes) => {
        const options = {
          key: orderRes.keyId,
          amount: orderRes.amount * 100,
          currency: orderRes.currency,
          name: 'NexusPOS Retail',
          description: 'Payment for Bill #' + this.bill.id,
          order_id: orderRes.orderId,
          handler: (response: any) => {
            this.verifyOnlinePayment(response, user);
          },
          prefill: {
            name: this.bill.customerName,
            contact: this.bill.customerMobile,
            email: user.email
          },
          theme: { color: '#6366f1' },
          modal: { ondismiss: () => this.isProcessing = false }
        };
        const rzp = new (window as any).Razorpay(options);
        rzp.open();
      },
      error: (err) => {
        this.isProcessing = false;
        alert('Razorpay initiation failed');
      }
    });
  }

  verifyOnlinePayment(rzpRes: any, user: any) {
    const verifyRequest = {
      razorpayOrderId: rzpRes.razorpay_order_id,
      razorpayPaymentId: rzpRes.razorpay_payment_id,
      razorpaySignature: rzpRes.razorpay_signature
    };

    this.api.verifyRazorpayPayment(verifyRequest).subscribe({
      next: () => {
        const paymentData = {
          billId: this.bill.id,
          amount: this.bill.totalAmount,
          paymentMode: 'Online',
          storeId: user.storeId,
          referenceNumber: rzpRes.razorpay_payment_id
        };
        this.api.collectPayment(paymentData).subscribe({
          next: () => this.onPaymentSuccess('Online', rzpRes.razorpay_payment_id),
          error: (err) => {
            this.isProcessing = false;
            alert('Online payment verified but collection update failed');
          }
        });
      },
      error: (err) => {
        this.isProcessing = false;
        alert('Payment verification failed');
      }
    });
  }

  onPaymentSuccess(mode: string, ref: string) {
    this.paymentSuccess = true;
    this.isProcessing = false;
    this.bill.status = 'Paid';
    this.bill.paymentMode = mode;
    this.bill.referenceNumber = ref;
    
    this.cdr.detectChanges();
    
    // Automatic Printing
    setTimeout(() => {
      window.print();
    }, 500);
  }

  newSale() {
    this.router.navigate(['/pos/billing']);
  }

  private loadRazorpayScript() {
    if ((window as any).hasOwnProperty('Razorpay')) return;
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.async = true;
    document.body.appendChild(script);
  }

  private normalizeBill(raw: any) {
    if (!raw) return null;
    const data = raw.bill ?? raw.Bill ?? raw.data ?? raw.Data ?? raw;
    const items = data.items ?? data.Items ?? [];
    return {
      id: data.id ?? data.Id ?? 0,
      billNumber: data.billNumber ?? data.BillNumber ?? '',
      date: data.date ?? data.Date ?? null,
      customerMobile: data.customerMobile ?? data.CustomerMobile ?? '',
      customerName: data.customerName ?? data.CustomerName ?? '',
      totalAmount: data.totalAmount ?? data.TotalAmount ?? 0,
      taxAmount: data.taxAmount ?? data.TaxAmount ?? 0,
      status: data.status ?? data.Status ?? '',
      storeId: data.storeId ?? data.StoreId ?? 0,
      cashierId: data.cashierId ?? data.CashierId ?? 0,
      items: Array.isArray(items) ? items.map((i: any) => ({
        id: i.id ?? i.Id ?? 0,
        productId: i.productId ?? i.ProductId ?? 0,
        productName: i.productName ?? i.ProductName ?? '',
        quantity: i.quantity ?? i.Quantity ?? 0,
        unitPrice: i.unitPrice ?? i.UnitPrice ?? 0,
        subTotal: i.subTotal ?? i.SubTotal ?? 0
      })) : []
    };
  }
}
