import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment.component.html',
  styleUrls: []
})
export class PaymentComponent implements OnInit {
  bill: any = null;
  paymentMode = 'Cash';
  cashReceived = 0;
  referenceNumber = '';
  isProcessing = false;
  receiptReady = false;
  storeContext: any = null;
  now = new Date();

  constructor(
    private router: Router, 
    private api: ApiService,
    public auth: AuthService
  ) {
    const nav = this.router.getCurrentNavigation();
    if (nav?.extras.state && nav.extras.state['bill']) {
      this.bill = nav.extras.state['bill'];
      this.cashReceived = this.bill.totalAmount;
    }
  }

  ngOnInit() {
    if (!this.bill) {
      this.router.navigate(['/pos/billing']);
      return;
    }
    const storedContext = localStorage.getItem('storeContext');
    if (storedContext) {
      this.storeContext = JSON.parse(storedContext);
    }
  }

  get balanceAmount() {
    if (this.paymentMode !== 'Cash') return 0;
    return Math.max(0, this.cashReceived - this.bill.totalAmount);
  }

  confirmPayment() {
    this.isProcessing = true;
    
    const payment = {
      billId: this.bill.id,
      paymentMode: this.paymentMode,
      amount: this.bill.totalAmount,
      referenceNo: this.referenceNumber || 'N/A',
      status: 'Paid'
    };

    this.api.collectPayment(payment).subscribe({
      next: () => {
        this.api.finalizeBill(this.bill.id).subscribe({
          next: () => {
            this.receiptReady = true;
            this.isProcessing = false;
          },
          error: (err) => {
            this.isProcessing = false;
            alert('Payment collected but bill finalization failed: ' + (err.error?.message || 'Check stock levels'));
          }
        });
      },
      error: (err) => {
        this.isProcessing = false;
        alert('Payment collection failed: ' + (err.error?.message || 'Unauthorized or server error'));
      }
    });
  }

  printReceipt() {
    window.print();
  }

  newBill() {
    this.router.navigate(['/pos/billing']);
  }
}
