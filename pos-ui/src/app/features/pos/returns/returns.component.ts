import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { FormsModule } from '@angular/forms';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-returns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './returns.component.html',
  styleUrl: './returns.component.css'
})
export class ReturnsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  bills: any[] = [];
  filteredBills: any[] = [];
  selectedBill: any = null;
  
  isLoading = true;
  isProcessing = false;
  
  searchQuery = '';
  activeTab = 'All Bills';
  currentStoreId = 0;

  returnReason = 'Wrong Item';
  refundMode = 'Original Mode';

  constructor(private api: ApiService, private cdr: ChangeDetectorRef, private signalr: SignalrService) {}

  ngOnInit() {
    this.loadUserInfo();
    this.loadBills();
    this.startAutoRefresh();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  startAutoRefresh() {
    this.signalr.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe((notif) => {
        console.log('POS Returns: SignalR Trigger', notif.message);
        // Refresh when returns are initiated or processed
        if (notif.message.includes('RETURN') || notif.message.includes('REFUND')) {
          this.loadBills(true);
        }
      });
  }

  loadUserInfo() {
    try {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      this.currentStoreId = user.storeId ?? user.StoreId ?? 0;
    } catch (e) {
      console.warn('Failed to load store context', e);
    }
  }

  loadBills(isSilent: boolean = false) {
    if (!isSilent) this.isLoading = true;
    this.api.getBills().subscribe({
      next: (data) => {
        // Enforce store-level isolation (Only show bills belonging to the user's store)
        this.bills = data.filter((b: any) => this.getBillStoreId(b) === this.currentStoreId);
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading bills', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    const query = this.searchQuery.toLowerCase().trim();
    
    this.filteredBills = this.bills.filter(bill => {
      const status = this.normalizeStatus(bill.status);
      let matchesTab = true;
      if (this.activeTab === 'Held Bills') {
        matchesTab = status === 'held' || status === 'draft' || status === 'pendingpayment';
      } else if (this.activeTab === 'Past Bills') {
        matchesTab = this.isReturnEligibleStatus(status);
      } else if (this.activeTab === 'Return Requests') {
        matchesTab = ['returnrequested', 'returned', 'rejected'].includes(status);
      }

      const billId = (bill.billNumber || bill.id || '').toString().toLowerCase();
      const customer = (bill.customerName || '').toLowerCase();
      const mobile = (bill.customerMobile || '').toLowerCase();
      
      const matchesSearch = !query || 
                            billId.includes(query) || 
                            customer.includes(query) || 
                            mobile.includes(query);

      return matchesTab && matchesSearch;
    });
  }

  setTab(tabName: string) {
    this.activeTab = tabName;
    this.selectedBill = null;
    this.applyFilters();
  }

  onSearchChange() {
    this.applyFilters();
  }

  selectBill(bill: any) {
    this.isLoading = true;
    this.api.getBillDetails(bill.id).subscribe({
      next: (details) => {
        this.selectedBill = details;
        this.selectedBill.items.forEach((i: any) => {
          i.returnQuantity = 1;
          i.selected = false;
        });
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading bill details', err);
        this.isLoading = false;
      }
    });
  }

  initiateReturn() {
    if (!this.selectedBill || !this.canInitiate() || !this.hasSelectedItems()) return;
    this.isProcessing = true;

    // Calculate Tax Factor (Proportionate tax share for each item)
    const billTotal = this.selectedBill.totalAmount || 0;
    const billTax = this.selectedBill.taxAmount || 0;
    const billSubTotal = billTotal - billTax;
    const taxFactor = billSubTotal > 0 ? (1 + (billTax / billSubTotal)) : 1;

    const returnData = {
      BillId: this.selectedBill.id,
      Reason: this.returnReason,
      RefundMode: this.refundMode,
      CustomerMobile: this.selectedBill.customerMobile,
      Items: this.selectedBill.items.filter((i: any) => i.selected).map((i: any) => {
        const qty = Math.min(Math.max(Number(i.returnQuantity) || 0, 1), i.quantity);
        const baseRefund = i.unitPrice * qty;
        return {
          BillItemId: i.productId || i.id,
          Quantity: qty,
          RefundAmount: Math.round(baseRefund * taxFactor * 100) / 100 // Include Tax share
        };
      })
    };

    this.api.initiateReturn(returnData).subscribe({
      next: (res) => {
        const returnId = res?.returnRequest?.id ?? res?.id ?? 'N/A';
        console.log('Return initiated successfully! ID: ' + returnId);
        this.selectedBill = null;
        this.loadBills();
        this.isProcessing = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Return initiation failed', err);
        alert('Return failed: ' + (err.error?.message || 'Unknown error'));
        this.isProcessing = false;
        this.cdr.detectChanges();
      }
    });
  }

  getDisplayStatus(): string {
    if (!this.selectedBill) return '';
    const status = this.normalizeStatus(this.selectedBill.status);
    
    if (status === 'returnrequested' || status === 'refunding') return 'Pending Manager Approval';
    if (status === 'returned' || status === 'refunded') return 'Approved & Refunded';
    if (status === 'rejected') return 'Rejected by Manager';
    if (this.isReturnEligibleStatus(status)) return 'Ready for Return';
    if (status === 'pendingpayment') return 'Payment In Progress';
    if (status === 'held' || status === 'draft') return 'Finalize bill before initiating return';
    return this.selectedBill.status || 'Unknown';
  }

  getStatusClass(): string {
    if (!this.selectedBill) return '';
    const status = this.normalizeStatus(this.selectedBill.status);
    
    if (status === 'returnrequested') return 'warning';
    if (status === 'returned') return 'success';
    if (status === 'rejected') return 'danger';
    return 'info';
  }

  canInitiate(): boolean {
    if (!this.selectedBill) return false;
    return this.isReturnEligibleStatus(this.normalizeStatus(this.selectedBill.status));
  }

  hasSelectedItems(): boolean {
    if (!this.selectedBill?.items?.length) return false;

    return this.selectedBill.items.some((item: any) => {
      const quantity = Number(item.returnQuantity) || 0;
      return item.selected && quantity > 0 && quantity <= item.quantity;
    });
  }

  getInitiateButtonLabel(): string {
    if (this.isProcessing) return 'Processing...';
    if (this.canInitiate()) return 'Initiate Return';

    const status = this.normalizeStatus(this.selectedBill?.status);

    if (status === 'draft' || status === 'held' || status === 'pendingpayment') return 'Finalize Bill To Return';
    if (status === 'returnrequested') return 'Return Already Requested';
    if (status === 'returned') return 'Bill Already Returned';
    if (status === 'rejected') return 'Return Was Rejected';

    return 'Return Unavailable';
  }

  getInitiateHint(): string {
    if (!this.selectedBill) return '';

    if (!this.canInitiate()) {
      const status = this.normalizeStatus(this.selectedBill.status);

      if (status === 'draft' || status === 'held' || status === 'pendingpayment') {
        return 'Only finalized bills can be sent for return approval.';
      }

      if (status === 'returnrequested') {
        return 'This bill is already waiting for manager approval.';
      }

      if (status === 'returned') {
        return 'This bill has already been refunded.';
      }

      if (status === 'rejected') {
        return 'This request was rejected. Create a fresh sale if needed and reprocess.';
      }
    }

    if (!this.hasSelectedItems()) {
      return 'Select at least one item and set a valid return quantity.';
    }

    return 'Selected items will be sent to the manager for approval.';
  }

  private isReturnEligibleStatus(status: string): boolean {
    return status === 'completed' || status === 'paid' || status === 'finalized';
  }

  public normalizeStatus(status: string | null | undefined): string {
    return (status || '').toString().trim().toLowerCase();
  }

  private getBillStoreId(bill: any): number {
    return Number(bill?.storeId ?? bill?.StoreId ?? 0);
  }
}
