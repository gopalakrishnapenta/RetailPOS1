// NEXUS-POS: All feature routes synchronized and verified.
import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { SignupComponent } from './features/auth/signup/signup.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { BillingComponent } from './features/pos/billing/billing.component';
import { PaymentComponent } from './features/pos/payment/payment.component';
import { DashboardComponent } from './features/admin/dashboard/dashboard.component';
import { InventoryComponent } from './features/admin/inventory/inventory.component';
import { ProductsComponent } from './features/admin/products/products.component';
import { StoresComponent } from './features/admin/stores/stores.component';
import { CategoriesComponent } from './features/admin/categories/categories.component';
import { AdminNotificationsComponent } from './features/admin/notifications/notifications.component';
import { inject } from '@angular/core';
import { AuthService } from './core/services/auth.service';
import { Router } from '@angular/router';
import { VerifyEmailComponent } from './features/auth/verify-email/verify-email.component';

const authGuard = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() ? true : router.parseUrl('/auth/login');
};

export const routes: Routes = [
  {
    path: 'auth',
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'signup', component: SignupComponent },
      { path: 'verify-email', component: VerifyEmailComponent },
      { path: 'forgot-password', component: ForgotPasswordComponent },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },
  {
    path: 'pos',
    canActivate: [authGuard],
    children: [
      { path: 'billing', component: BillingComponent },
      { path: 'payment', component: PaymentComponent },
      { path: 'returns', loadComponent: () => import('./features/pos/returns/returns.component').then(m => m.PosReturnsComponent) }
    ]
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'inventory', component: InventoryComponent },
      { path: 'products', component: ProductsComponent },
      { path: 'stores', component: StoresComponent },
      { path: 'categories', component: CategoriesComponent },
      { path: 'notifications', component: AdminNotificationsComponent },
      { path: 'returns', loadComponent: () => import('./features/admin/returns/returns.component').then(m => m.AdminReturnsComponent) },
      { path: 'reports', loadComponent: () => import('./features/admin/reports/reports.component').then(m => m.AdminReportsComponent) }
    ]
  },
  { path: '**', redirectTo: 'auth/login' }
];
