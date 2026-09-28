import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { superAdminGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent), canActivate: [authGuard] },
  { path: 'products', loadComponent: () => import('./features/products/product-list.component').then(m => m.ProductListComponent), canActivate: [authGuard] },
  { path: 'categories', loadComponent: () => import('./features/categories/category-list.component').then(m => m.CategoryListComponent), canActivate: [authGuard] },
  { path: 'brands', loadComponent: () => import('./features/brands/brand-list.component').then(m => m.BrandListComponent), canActivate: [authGuard] },
  { path: 'orders', loadComponent: () => import('./features/orders-management/order-list.component').then(m => m.OrderListComponent), canActivate: [authGuard] },
  { path: 'promotions', loadComponent: () => import('./features/promotions/promotion-list.component').then(m => m.PromotionListComponent), canActivate: [authGuard] },
  { path: 'inventory', loadComponent: () => import('./features/inventory/inventory.component').then(m => m.InventoryComponent), canActivate: [authGuard] },
  { path: 'users', loadComponent: () => import('./features/users/user-management.component').then(m => m.UserManagementComponent), canActivate: [authGuard, superAdminGuard] },
  { path: '**', redirectTo: '' }
];
