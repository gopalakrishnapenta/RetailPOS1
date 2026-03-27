import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: []
})
export class LoginComponent implements OnInit {
  credentials = { 
    email: '', 
    password: '',
    storeCode: '',
    shiftDate: new Date().toISOString().split('T')[0]
  };
  
  stores: any[] = [];
  isLoading = false;
  error = '';
  showPassword = false;

  constructor(
    private auth: AuthService, 
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.loadStores();
  }

  loadStores() {
    this.http.get<any[]>('http://localhost:5000/gateway/auth/stores/active').subscribe({
      next: (res) => {
        this.stores = res;
        if (res.length > 0) {
          this.credentials.storeCode = res[0].storeCode;
        }
      },
      error: () => {
        console.error('Failed to load stores');
      }
    });
  }

  onLogin() {
    this.isLoading = true;
    this.error = '';
    
    // Pass everything to the auth service
    this.auth.login(this.credentials).subscribe({
      next: (res) => {
        this.isLoading = false;
        // Auto-select store if matching storeId is found in the stores list
        const userStore = this.stores.find(s => s.id === res.storeId);
        const finalStoreCode = userStore ? userStore.storeCode : this.credentials.storeCode;
        const finalShiftDate = this.credentials.shiftDate || new Date().toISOString().split('T')[0];

        // Save store context for POS usage
        localStorage.setItem('storeContext', JSON.stringify({
          storeId: res.storeId,
          storeCode: finalStoreCode,
          shiftDate: finalShiftDate
        }));
        
        console.log('Login success. Role:', res.role);
        const role = res.role ? res.role.toLowerCase() : '';
        if (role === 'admin' || role === 'storemanager' || role === 'manager') {
          this.router.navigate(['/admin/dashboard']);
        } else {
          this.router.navigate(['/pos/billing']);
        }
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Invalid email or password';
      }
    });
  }
}
