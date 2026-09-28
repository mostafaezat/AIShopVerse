import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { SignalRService, NewOrderPayload, LowStockPayload } from './core/services/signalr.service';

interface RealtimeAlert { title: string; message: string; kind: string; }

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="app-layout" *ngIf="auth.isLoggedIn(); else loginPage">
      <aside class="sidebar">
        <h2>AIShopVerse Admin</h2>
        <nav>
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/products" routerLinkActive="active">Products</a>
          <a routerLink="/categories" routerLinkActive="active">Categories</a>
          <a routerLink="/brands" routerLinkActive="active">Brands</a>
          <a routerLink="/inventory" routerLinkActive="active">Inventory</a>
          <a routerLink="/orders" routerLinkActive="active">Orders</a>
          <a routerLink="/reviews" routerLinkActive="active">Reviews</a>
          <a routerLink="/promotions" routerLinkActive="active">Promotions</a>
          <button class="notification-btn" (click)="openNotifications()">Notifications</button>
        </nav>
        <button class="logout-btn" (click)="auth.logout()">Logout</button>
      </aside>
      <div class="content">
        <div class="alert-stack">
          <div *ngFor="let alert of alerts; let i = index" class="rt-alert" [class.alert-lowstock]="alert.kind === 'lowstock'">
            <div class="rt-alert-title">{{ alert.title }}</div>
            <div class="rt-alert-msg">{{ alert.message }}</div>
            <button class="rt-alert-close" (click)="dismissAlert(i)">&times;</button>
          </div>
        </div>
        <main><router-outlet></router-outlet></main>
      </div>
    </div>
    <ng-template #loginPage><router-outlet></router-outlet></ng-template>
  `,
  styles: [`
    .app-layout { display: flex; min-height: 100vh; }
    .sidebar { width: 240px; background: #1a1a2e; color: white; padding: 1rem; display: flex; flex-direction: column; }
    .sidebar h2 { font-size: 1.1rem; margin-bottom: 2rem; }
    .sidebar nav { display: flex; flex-direction: column; gap: 0.5rem; flex: 1; }
    .sidebar nav a { color: #ccc; text-decoration: none; padding: 0.5rem; border-radius: 4px; }
    .sidebar nav a:hover, .sidebar nav a.active { background: #16213e; color: white; }
    .logout-btn { background: #e74c3c; color: white; border: none; padding: 0.5rem; border-radius: 4px; cursor: pointer; margin-top: 0.5rem; }
    .notification-btn { background: #16213e; color: #ccc; border: 1px solid #2a3a5c; padding: 0.5rem; border-radius: 4px; cursor: pointer; margin-top: 0.5rem; }
    .notification-btn:hover { color: white; }
    .content { flex: 1; display: flex; flex-direction: column; }
    main { flex: 1; padding: 2rem; background: #f5f6fa; }
    .alert-stack { position: fixed; top: 16px; right: 16px; z-index: 3000; display: flex; flex-direction: column; gap: 8px; max-width: 360px; }
    .rt-alert { background: #fff; border: 1px solid #e0e0e0; border-left: 5px solid #3b82f6; border-radius: 6px; padding: 10px 32px 10px 12px; box-shadow: 0 4px 12px rgba(0,0,0,.15); position: relative; }
    .rt-alert.alert-lowstock { border-left-color: #e74c3c; }
    .rt-alert-title { font-weight: 700; }
    .rt-alert-msg { font-size: 0.85rem; color: #555; }
    .rt-alert-close { position: absolute; top: 4px; right: 8px; background: none; border: none; font-size: 1.2rem; cursor: pointer; color: #888; }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  alerts: RealtimeAlert[] = [];
  private subs: Subscription[] = [];

  constructor(public auth: AuthService, private signalR: SignalRService, private router: Router) {}

  ngOnInit() {
    this.subs.push(this.signalR.newOrder$.subscribe((o: NewOrderPayload | null) => {
      if (o) this.addAlert(`New order ${o.orderNumber}`, `A new order was placed ($${o.total.toFixed(2)}).`, 'neworder');
    }));
    this.subs.push(this.signalR.lowStock$.subscribe((p: LowStockPayload | null) => {
      if (p) this.addAlert(`Low stock: ${p.nameEN}`, `${p.nameEN} (${p.sku}) is at ${p.stockQuantity}, threshold ${p.threshold}.`, 'lowstock');
    }));
  }

  ngOnDestroy() {
    this.subs.forEach(s => s.unsubscribe());
  }

  private addAlert(title: string, message: string, kind: string) {
    this.alerts.push({ title, message, kind });
    if (this.alerts.length > 5) this.alerts.shift();
  }

  dismissAlert(index: number) {
    this.alerts.splice(index, 1);
  }

  openNotifications() {
    this.router.navigate(['/dashboard']);
  }
}
