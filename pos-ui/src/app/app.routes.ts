import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { SignupComponent } from './features/auth/signup/signup.component';
import { OtpVerificationComponent } from './features/auth/otp-verification/otp-verification.component';
import { PendingApprovalComponent } from './features/auth/pending-approval/pending-approval.component';
import { LandingComponent } from './features/marketing/landing/landing.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingComponent },
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },
  { path: 'otp-verification', component: OtpVerificationComponent },
  { path: 'pending-approval', component: PendingApprovalComponent },
  { 
    path: 'pos', 
    canActivate: [authGuard],
    children: [
      { path: 'billing', loadComponent: () => import('./features/pos/billing/billing.component').then(m => m.BillingComponent) },
      { path: 'returns', loadComponent: () => import('./features/pos/returns/returns.component').then(m => m.ReturnsComponent) },
      { path: 'payment/:id', loadComponent: () => import('./features/pos/payment/payment.component').then(m => m.PaymentComponent) }
    ]
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'inventory', loadComponent: () => import('./features/admin/inventory/inventory.component').then(m => m.InventoryComponent), data: { roles: ['Admin', 'StoreManager'] } },
      { path: 'products', loadComponent: () => import('./features/admin/products/products.component').then(m => m.ProductsComponent), data: { roles: ['Admin', 'StoreManager'] } },
      { path: 'categories', loadComponent: () => import('./features/admin/categories/categories.component').then(m => m.CategoriesComponent), data: { roles: ['Admin'] } },
      { path: 'staff', loadComponent: () => import('./features/admin/staff/staff.component').then(m => m.StaffComponent), data: { roles: ['Admin'] } },
      { path: 'stores', loadComponent: () => import('./features/admin/stores/stores.component').then(m => m.StoresComponent), data: { roles: ['Admin'] } },
      { path: 'returns', loadComponent: () => import('./features/admin/returns/returns.component').then(m => m.ReturnsManagementComponent), data: { roles: ['Admin', 'StoreManager'] } },
      { path: 'transactions', loadComponent: () => import('./features/admin/transactions/transactions.component').then(m => m.TransactionsComponent), data: { roles: ['Admin', 'StoreManager'] } },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];

