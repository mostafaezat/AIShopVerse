import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="orders-container">
      <h2>Order Management</h2>
      <div class="filters">
        <select [(ngModel)]="statusFilter" (change)="loadOrders()">
          <option value="">All Status</option>
          <option value="0">Pending</option>
          <option value="1">Paid</option>
          <option value="2">Processing</option>
          <option value="3">Shipped</option>
          <option value="4">Delivered</option>
          <option value="5">Cancelled</option>
          <option value="6">Refunded</option>
        </select>
      </div>
      <table>
        <thead><tr><th>Order #</th><th>Date</th><th>Status</th><th>Total</th><th>Actions</th></tr></thead>
        <tbody>
          <tr *ngFor="let order of orders">
            <td><a routerLink="/orders/{{ order.id }}">{{ order.orderNumber }}</a></td>
            <td>{{ order.createdAt | date }}</td>
            <td>{{ order.status }}</td>
            <td>{{ order.total | currency }}</td>
            <td>
              <select *ngIf="getStatusOptions(order.status).length > 0" (change)="updateStatus(order.id, $any($event.target).value)">
                <option *ngFor="let opt of getStatusOptions(order.status)" [value]="opt.value">{{ opt.label }}</option>
              </select>
              <span class="terminal-status" *ngIf="getStatusOptions(order.status).length === 0">{{ order.status }}</span>
              <button class="btn-refund" *ngIf="isRefundable(order.status)" (click)="refund(order.id)">Refund</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .btn-refund { margin-left: 0.5rem; padding: 4px 10px; background: #dc3545; color: #fff;
      border: none; border-radius: 4px; cursor: pointer; }
    .btn-refund:hover { background: #bb2d3b; }
    .terminal-status { color: #6c757d; }
  `]
})
export class OrderListComponent implements OnInit {
  orders: any[] = [];
  statusFilter = '';

  private readonly statusOptions = [
    { value: '0', label: 'Pending' },
    { value: '1', label: 'Paid' },
    { value: '2', label: 'Processing' },
    { value: '3', label: 'Shipped' },
    { value: '4', label: 'Delivered' },
    { value: '5', label: 'Cancelled' },
    { value: '6', label: 'Refunded' }
  ];

  private readonly allowedTransitions: { [label: string]: string[] } = {
    'Pending': ['Paid', 'Processing', 'Cancelled'],
    'Paid': ['Refunded'],
    'Processing': ['Shipped', 'Cancelled'],
    'Shipped': ['Delivered', 'Cancelled'],
    'Delivered': [],
    'Cancelled': [],
    'Refunded': []
  };

  constructor(private orderService: OrderService) {}
  ngOnInit() { this.loadOrders(); }
  loadOrders() { this.orderService.getAll(1, 50, this.statusFilter).subscribe(res => this.orders = res.data || []); }
  updateStatus(orderId: string, status: number) {
    this.orderService.updateStatus(orderId, parseInt(status as any)).subscribe(() => this.loadOrders());
  }
  refund(orderId: string) {
    if (!confirm('Refund this order? This will issue a real payment refund if Stripe is configured.')) return;
    this.orderService.refund(orderId).subscribe({
      next: () => { alert('Order refunded.'); this.loadOrders(); },
      error: (err: any) => alert(err?.error?.message || 'Refund failed.')
    });
  }
  isRefundable(status: string): boolean {
    return status === 'Paid';
  }
  getStatusOptions(status: string): any[] {
    const targets = this.allowedTransitions[status];
    if (!targets) return [];
    return this.statusOptions.filter(o => targets.includes(o.label));
  }
}
