import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../core/services/order.service';
import { Order, OrderStatus } from '../../core/models';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Orders</h2>
      <select class="form-select" style="width: 200px;" [(ngModel)]="statusFilter" (change)="filterOrders()">
        <option value="">All Statuses</option>
        <option [value]="0">Pending</option>
        <option [value]="1">Paid</option>
        <option [value]="2">Processing</option>
        <option [value]="3">Shipped</option>
        <option [value]="4">Delivered</option>
        <option [value]="5">Cancelled</option>
      </select>
    </div>

    <div class="table-responsive">
      <table class="table table-striped table-hover">
        <thead class="table-dark">
          <tr>
            <th>Order #</th>
            <th>Date</th>
            <th>Total</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let order of filteredOrders">
            <td>{{ order.orderNumber }}</td>
            <td>{{ order.createdAt | date:'short' }}</td>
            <td>{{ order.total | currency }}</td>
            <td>
              <span class="badge" [ngClass]="getStatusClass(order.status)">
                {{ getStatusText(order.status) }}
              </span>
            </td>
            <td>
              <select class="form-select form-select-sm" style="width: 160px;" [(ngModel)]="order.status" (change)="updateStatus(order)">
                <option [value]="0">Pending</option>
                <option [value]="1">Paid</option>
                <option [value]="2">Processing</option>
                <option [value]="3">Shipped</option>
                <option [value]="4">Delivered</option>
                <option [value]="5">Cancelled</option>
              </select>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class OrderListComponent implements OnInit {
  orders: Order[] = [];
  filteredOrders: Order[] = [];
  statusFilter = '';

  constructor(private orderService: OrderService) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.orderService.getAll().subscribe(o => {
      this.orders = o;
      this.filterOrders();
    });
  }

  filterOrders(): void {
    if (this.statusFilter === '') {
      this.filteredOrders = this.orders;
    } else {
      this.filteredOrders = this.orders.filter(o => o.status === +this.statusFilter);
    }
  }

  updateStatus(order: Order): void {
    this.orderService.updateStatus(order.id, order.status).subscribe();
  }

  getStatusText(status: OrderStatus): string {
    const map: Record<number, string> = {
      0: 'Pending', 1: 'Paid', 2: 'Processing', 3: 'Shipped', 4: 'Delivered', 5: 'Cancelled', 6: 'Refunded'
    };
    return map[status] || 'Unknown';
  }

  getStatusClass(status: OrderStatus): string {
    const map: Record<number, string> = {
      0: 'bg-warning text-dark',
      1: 'bg-info',
      2: 'bg-primary',
      3: 'bg-secondary',
      4: 'bg-success',
      5: 'bg-danger',
      6: 'bg-dark'
    };
    return map[status] || 'bg-secondary';
  }
}
