import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-stores',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AdminSidebarComponent],
  templateUrl: './stores.component.html'
})
export class StoresComponent implements OnInit {
  stores: any[] = [];
  newStore = { name: '', storeCode: '', location: '' };

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadStores();
  }

  loadStores() {
    this.api.getStores().subscribe(res => this.stores = res);
  }

  onSubmit() {
    if (!this.newStore.name || !this.newStore.storeCode) return;
    this.api.addStore(this.newStore).subscribe(() => {
      this.loadStores();
      this.newStore = { name: '', storeCode: '', location: '' };
      alert('Store added successfully');
    });
  }
}
