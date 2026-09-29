import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { OrderService } from '../../core/services/order.service';
import { Order } from '../../core/models';

@Component({
  selector: 'app-order-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="orders-container">
      <h2>Order History</h2>
      <div *ngIf="loading" class="text-center py-5 text-muted">Loading your orders...</div>
      <div *ngIf="!loading && loadError" class="text-center py-5">
        <div class="alert alert-warning mx-auto" style="max-width:480px;">
          Unable to load your orders. Please try again.
          <div class="mt-2"><button class="btn btn-outline-secondary btn-sm" (click)="loadOrders()">Retry</button></div>
        </div>
      </div>
      <div *ngFor="let order of orders" class="order-card">
        <a [routerLink]="['/orders', order.id]">
          <p><strong>{{ order.orderNumber }}</strong> - {{ order.status }}</p>
          <p>Total: {{ order.total | currency }}</p>
          <p>{{ order.createdAt | date }}</p>
        </a>
      </div>
      <div *ngIf="!loading && !loadError && orders.length === 0">No orders found.</div>
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