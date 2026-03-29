import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Toast {
  id: number;
  title: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  closing?: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private toasts$ = new BehaviorSubject<Toast[]>([]);
  public toasts = this.toasts$.asObservable();
  private counter = 0;

  show(title: string, message: string, type: Toast['type'] = 'info') {
    const id = ++this.counter;
    const toast: Toast = { id, title, message, type };
    this.toasts$.next([...this.toasts$.value, toast]);

    // Auto-dismiss after 5 seconds
    setTimeout(() => this.remove(id), 5000);
  }

  success(message: string, title: string = 'Success') {
    this.show(title, message, 'success');
  }

  error(message: string, title: string = 'Error') {
    this.show(title, message, 'error');
  }

  warning(message: string, title: string = 'Warning') {
    this.show(title, message, 'warning');
  }

  remove(id: number) {
    const current = this.toasts$.value;
    const index = current.findIndex(t => t.id === id);
    if (index > -1) {
      // Mark as closing for animation
      current[index].closing = true;
      this.toasts$.next([...current]);
      
      // Remove from list after animation completes
      setTimeout(() => {
        this.toasts$.next(this.toasts$.value.filter(t => t.id !== id));
      }, 300);
    }
  }
}
