import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-returns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './returns.component.html',
  styleUrl: './returns.component.css'
})
export class ReturnsComponent implements OnInit {
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

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadUserInfo();
    this.loadBills();
  }

  loadUserInfo() {
    try {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      this.currentStoreId = user.storeId || 0;
    } catch (e) {
      console.warn('Failed to load store context', e);
    }
  }

  loadBills() {
    this.isLoading = true;
    this.api.getBills().subscribe({
      next: (data) => {
        // Enforce store-level isolation (Only show bills belonging to the user's store)
        this.bills = data.filter((b: any) => b.storeId === this.currentStoreId);
        this.applyFilters();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading bills', err);
        this.isLoading = false;
      }
    });
  }

  applyFilters() {
    const query = this.searchQuery.toLowerCase().trim();
    
    this.filteredBills = this.bills.filter(bill => {
      // 1. Tab Filtering (Status mapping)
      let matchesTab = true;
      if (this.activeTab === 'Held Bills') {
        matchesTab = bill.status === 'Held';
      } else if (this.activeTab === 'Past Bills') {
        matchesTab = bill.status === 'Completed' || bill.status === 'Paid';
      } else if (this.activeTab === 'Return Requests') {
        matchesTab = bill.status === 'ReturnRequested' || bill.status === 'Returned';
      }

      // 2. Search Query (Bill ID, Customer Name, Mobile)
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
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading bill details', err);
        this.isLoading = false;
      }
    });
  }

  initiateReturn() {
    if (!this.selectedBill) return;
    this.isProcessing = true;

    const returnData = {
      BillId: this.selectedBill.id,
      Reason: this.returnReason,
      RefundMode: this.refundMode,
      CustomerMobile: this.selectedBill.customerMobile,
      Items: this.selectedBill.items.filter((i: any) => i.selected).map((i: any) => ({
        BillItemId: i.productId || i.id,
        Quantity: i.quantity,
        RefundAmount: i.unitPrice * i.quantity
      }))
    };

    this.api.initiateReturn(returnData).subscribe({
      next: (res) => {
        alert('Return initiated successfully! ID: ' + res.id);
        this.selectedBill = null;
        this.loadBills();
        this.isProcessing = false;
      },
      error: (err) => {
        console.error('Return initiation failed', err);
        alert('Return failed: ' + (err.error?.message || 'Unknown error'));
        this.isProcessing = false;
      }
    });
  }

  getDisplayStatus(): string {
    if (!this.selectedBill) return '';
    const status = this.selectedBill.status?.toLowerCase();
    
    if (status === 'returnrequested') return 'Pending Manager Approval';
    if (status === 'returned') return 'Approved & Refunded';
    if (status === 'rejected') return 'Rejected by Manager';
    if (status === 'completed' || status === 'paid') return 'Ready for Return';
    return this.selectedBill.status || 'Unknown';
  }

  getStatusClass(): string {
    if (!this.selectedBill) return '';
    const status = this.selectedBill.status?.toLowerCase();
    
    if (status === 'returnrequested') return 'warning';
    if (status === 'returned') return 'success';
    if (status === 'rejected') return 'danger';
    return 'info';
  }

  canInitiate(): boolean {
    if (!this.selectedBill) return false;
    const status = this.selectedBill.status?.toLowerCase();
    return status === 'completed' || status === 'paid';
  }
}
