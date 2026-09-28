import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div *ngIf="authService.isLoggedIn(); else loginView" class="container-fluid">
      <div class="row">
        <div class="col-md-2 sidebar p-0">
          <div class="p-3 border-bottom border-secondary">
            <h5 class="text-white mb-0">AIShopVerse Admin</h5>
          </div>
          <nav class="mt-2">
            <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
            <a routerLink="/products" routerLinkActive="active">Products</a>
            <a routerLink="/categories" routerLinkActive="active">Categories</a>
            <a routerLink="/brands" routerLinkActive="active">Brands</a>
            <a routerLink="/orders" routerLinkActive="active">Orders</a>
            <a routerLink="/promotions" routerLinkActive="active">Promotions</a>
            <a routerLink="/inventory" routerLinkActive="active">Inventory</a>
            <a *ngIf="authService.isSuperAdmin()" routerLink="/users" routerLinkActive="active">Users</a>
          </nav>
          <div class="p-3 mt-auto position-absolute bottom-0 w-100">
            <button class="btn btn-outline-light btn-sm w-100" (click)="logout()">Logout</button>
          </div>
        </div>
        <div class="col-md-10 content-area">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
    <ng-template #loginView>
      <router-outlet></router-outlet>
    </ng-template>
  `,
  styles: [`
    :host { display: block; }
    .sidebar { min-height: 100vh; background-color: #2c3e50; }
    .sidebar nav a { color: #ecf0f1; text-decoration: none; padding: 10px 20px; display: block; transition: background-color 0.2s; }
    .sidebar nav a:hover, .sidebar nav a.active { background-color: #34495e; color: #fff; }
    .content-area { padding: 20px; }
  `]
})
export class AppComponent {
  constructor(public authService: AuthService, private router: Router) {}

  logout(): void {
    this.authService.logout();
  }
}
