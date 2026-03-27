import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class StoreContextService {
  private selectedStoreIdSubject = new BehaviorSubject<number | null>(null);
  selectedStoreId$ = this.selectedStoreIdSubject.asObservable();

  constructor() {
    const saved = localStorage.getItem('admin_selected_store_id');
    if (saved) {
      this.selectedStoreIdSubject.next(parseInt(saved));
    }
  }

  setStoreId(id: number | null) {
    if (id === null) {
      localStorage.removeItem('admin_selected_store_id');
    } else {
      localStorage.setItem('admin_selected_store_id', id.toString());
    }
    this.selectedStoreIdSubject.next(id);
  }

  getStoreId(): number | null {
    return this.selectedStoreIdSubject.value;
  }
}
