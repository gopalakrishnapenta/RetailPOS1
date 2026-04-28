import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { SignupComponent } from './features/auth/signup/signup.component';
import { OtpVerificationComponent } from './features/auth/otp-verification/otp-verification.component';
import { PendingApprovalComponent } from './features/auth/pending-approval/pending-approval.component';
import { BillingComponent } from './features/pos/billing/billing.component';
import { DashboardComponent } from './features/admin/dashboard/dashboard.component';
import { InventoryComponent } from './features/admin/inventory/inventory.component';
import { ProductsComponent } from './features/admin/products/products.component';
import { CategoriesComponent } from './features/admin/categories/categories.component';
import { StaffComponent } from './features/admin/staff/staff.component';
import { StoresComponent } from './features/admin/stores/stores.component';
import { ReturnsComponent } from './features/pos/returns/returns.component';
import { ReturnsManagementComponent } from './features/admin/returns/returns.component';
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
      { path: 'billing', component: BillingComponent },
      { path: 'returns', component: ReturnsComponent },
      { path: 'payment/:id', loadComponent: () => import('./features/pos/payment/payment.component').then(m => m.PaymentComponent) }
    ]
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'inventory', component: InventoryComponent, data: { roles: ['Admin', 'StoreManager'] } },
      { path: 'products', component: ProductsComponent, data: { roles: ['Admin', 'StoreManager'] } },
      { path: 'categories', component: CategoriesComponent, data: { roles: ['Admin'] } },
      { path: 'staff', component: StaffComponent, data: { roles: ['Admin'] } },
      { path: 'stores', component: StoresComponent, data: { roles: ['Admin'] } },
      { path: 'returns', component: ReturnsManagementComponent, data: { roles: ['Admin', 'StoreManager'] } },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];

