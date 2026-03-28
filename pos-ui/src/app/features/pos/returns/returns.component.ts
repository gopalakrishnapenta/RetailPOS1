import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-pos-returns',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div style="min-height: 100vh; background: #f8fafc; padding: 2rem;">
      <h1 style="color: #1e293b;">POS Return Initiation</h1>
      <button routerLink="/pos/billing" style="margin-bottom: 1rem; padding: 0.5rem 1rem; background: #64748b; color: white; border: none; border-radius: 4px; cursor: pointer;">Back to Billing</button>
      
      <div style="background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); max-width: 600px;">
        <h3>Search Bill ID</h3>
        <div style="display: flex; gap: 0.5rem;">
          <input type="text" [(ngModel)]="billId" placeholder="Bill ID" style="flex: 1; padding: 0.5rem; border: 1px solid #e2e8f0; border-radius: 4px;">
          <button (click)="lookupBill()" style="padding: 0.5rem 1rem; background: #2563eb; color: white; border: none; border-radius: 4px; cursor: pointer;">Search</button>
        </div>
      </div>

      <div *ngIf="bill" style="margin-top: 1.5rem; background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); max-width: 600px;">
        <h3>Bill #{{ bill.id }} - {{ bill.customerName }}</h3>
        <table style="width: 100%; border-collapse: collapse; margin-top: 1rem;">
          <tr *ngFor="let item of bill.items" style="border-bottom: 1px solid #f1f5f9;">
            <td style="padding: 0.5rem;">{{ item.productName }}</td>
            <td style="padding: 0.5rem;">Qty: {{ item.quantity }}</td>
            <td style="padding: 0.5rem; text-align: right;">
              <button (click)="itemToReturn = item" style="padding: 0.25rem 0.5rem; background: #f1f5f9; border: 1px solid #e2e8f0; border-radius: 4px; cursor: pointer;">Select</button>
            </td>
          </tr>
        </table>

        <div *ngIf="itemToReturn" style="margin-top: 1.5rem; border-top: 1px solid #e2e8f0; padding-top: 1rem;">
          <p>Requesting return for: <strong>{{ itemToReturn.productName }}</strong></p>
          <textarea [(ngModel)]="reason" style="width: 100%; padding: 0.5rem; border: 1px solid #e2e8f0; border-radius: 4px;" placeholder="Reason for return"></textarea>
          <button (click)="submitReturn()" style="margin-top: 1rem; width: 100%; padding: 0.75rem; background: #2563eb; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 600;">Initiate Return</button>
        </div>
      </div>
    </div>
  `
})
export class PosReturnsComponent {
  billId = '';
  bill: any = null;
  itemToReturn: any = null;
  reason = '';

  constructor(private api: ApiService, private router: Router) { }

  lookupBill() {
    if (!this.billId) return;
    this.api.getBillById(parseInt(this.billId)).subscribe(res => {
      this.bill = res;
      this.itemToReturn = null;
    });
  }

  submitReturn() {
    if (!this.reason) return alert('Please enter a reason');
    const payload = { originalBillId: this.bill.id, productId: this.itemToReturn.productId, quantity: this.itemToReturn.quantity, reason: this.reason };
    this.api.initiateReturn(payload).subscribe(() => {
      alert('Return initiated!');
      this.router.navigate(['/pos/billing']);
    });
  }
}
