import { Routes } from '@angular/router';
import { authGuard, superAdminGuard } from './core/guards/auth.guard';

// superAdminGuard is defined but not yet applied — use it when adding admin-user-management
// routes that call POST /api/auth/create-admin (backend requires [Authorize(Roles="SuperAdmin")]).

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent), canActivate: [authGuard] },
  { path: 'products', loadComponent: () => import('./features/products/product-list.component').then(m => m.ProductListComponent), canActivate: [authGuard] },
  { path: 'categories', loadComponent: () => import('./features/categories/category-list.component').then(m => m.CategoryListComponent), canActivate: [authGuard] },
  { path: 'brands', loadComponent: () => import('./features/brands/brand-list.component').then(m => m.BrandListComponent), canActivate: [authGuard] },
  { path: 'orders', loadComponent: () => import('./features/orders-management/order-list.component').then(m => m.OrderListComponent), canActivate: [authGuard] },
  { path: 'orders/:id', loadComponent: () => import('./features/orders-management/order-detail.component').then(m => m.OrderDetailComponent), canActivate: [authGuard] },
  { path: 'promotions', loadComponent: () => import('./features/promotions/promotion-list.component').then(m => m.PromotionListComponent), canActivate: [authGuard] },
  { path: 'reviews', loadComponent: () => import('./features/reviews/review-list.component').then(m => m.ReviewListComponent), canActivate: [authGuard] },
  { path: 'inventory', loadComponent: () => import('./features/inventory/inventory.component').then(m => m.InventoryComponent), canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
