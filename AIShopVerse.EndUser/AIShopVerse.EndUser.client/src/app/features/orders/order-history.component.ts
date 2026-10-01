import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { OrderService } from '../../core/services/order.service';
import { Order } from '../../core/models';
import { OrderStatusPipe } from '../../shared/pipes/order-status.pipe';
import { PricePipe, LocalizedDatePipe } from '../../shared/pipes/localized-format.pipes';

@Component({
  selector: 'app-order-history',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe, OrderStatusPipe, PricePipe, LocalizedDatePipe],
  template: `
    <div class="orders-container container py-4">
      <h2>{{ 'orders.title' | translate }}</h2>
      <div *ngIf="loading" class="text-center py-5 text-muted">{{ 'orders.loading' | translate }}</div>
      <div *ngIf="!loading && loadError" class="text-center py-5">
        <div class="alert alert-warning mx-auto" style="max-width:480px;">
          {{ 'orders.loadError' | translate }}
          <div class="mt-2"><button class="btn btn-outline-secondary btn-sm" (click)="loadOrders()">{{ 'common.retry' | translate }}</button></div>
        </div>
      </div>
      <div class="card order-card mb-2" *ngFor="let order of orders">
        <a [routerLink]="['/orders', order.id]" class="text-decoration-none text-dark">
          <p><strong>{{ order.orderNumber }}</strong> — {{ order.status | orderStatus }}</p>
          <p>{{ 'orders.total' | translate }}: {{ order.total | price }}</p>
          <p>{{ order.createdAt | localizedDate: 'medium' }}</p>
        </a>
      </div>
      <div *ngIf="!loading && !loadError && orders.length === 0">{{ 'orders.noOrders' | translate }}</div>
    </div>
  `
})
export class OrderHistoryComponent implements OnInit {
  orders: Order[] = [];
  loading = true;
  loadError = false;

  constructor(private orderService: OrderService) {}

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.loading = true;
    this.loadError = false;
    this.orderService.getHistory().subscribe({
      next: orders => {
        this.orders = orders || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
  }
}
