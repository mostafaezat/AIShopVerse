import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container py-4">
      <p><a routerLink="/orders">&larr; Back to orders</a></p>
      <div *ngIf="loading" class="text-center py-5 text-muted">Loading order...</div>
      <div *ngIf="!loading && loadError" class="alert alert-warning">
        Unable to load this order. Please try again.
        <button class="btn btn-sm btn-outline-secondary ms-2" (click)="loadOrder()">Retry</button>
      </div>
      <div *ngIf="order">
        <h2>Order {{ order.orderNumber }}</h2>
        <p>
          Status: <strong class="badge" [class]="'text-bg-' + statusBadge">{{ order.status }}</strong>
          <span class="text-muted ms-2">Placed {{ order.createdAt | date:'medium' }}</span>
        </p>

        <div class="row mt-4">
          <div class="col-md-6">
            <h5>Customer</h5>
            <p *ngIf="order.customer">
              <strong>{{ order.customer.fullName || order.customer.userName }}</strong><br />
              <ng-container *ngIf="order.customer.email">{{ order.customer.email }}<br /></ng-container>
              <ng-container *ngIf="order.customer.phoneNumber">{{ order.customer.phoneNumber }}</ng-container>
            </p>
            <p *ngIf="!order.customer" class="text-muted">Customer information unavailable.</p>
          </div>
          <div class="col-md-6">
            <div class="mb-3">
              <h5>Shipping Address</h5>
              <p *ngIf="order.shippingAddress">{{ order.shippingAddress }}</p>
              <p *ngIf="!order.shippingAddress" class="text-muted">None</p>
            </div>
            <div>
              <h5>Billing Address</h5>
              <p *ngIf="order.billingAddress">{{ order.billingAddress }}</p>
              <p *ngIf="!order.billingAddress" class="text-muted">None</p>
            </div>
          </div>
        </div>

        <h5 class="mt-4">Items</h5>
        <table class="table table-sm">
          <thead><tr><th>Product</th><th>Variant</th><th>Qty</th><th>Unit Price</th><th>Total</th></tr></thead>
          <tbody>
            <tr *ngFor="let item of order.items">
              <td>{{ item.productName }}</td>
              <td>{{ item.variantLabel || '-' }}</td>
              <td>{{ item.quantity }}</td>
              <td>{{ item.unitPrice | currency }}</td>
              <td>{{ item.totalPrice | currency }}</td>
            </tr>
          </tbody>
        </table>

        <h5 class="mt-4">Totals</h5>
        <p class="mb-1">Subtotal: {{ order.subtotal | currency }}</p>
        <p class="mb-1" *ngIf="order.discountAmount > 0">Discount: -{{ order.discountAmount | currency }}</p>
        <p class="mb-1">Tax: {{ order.tax | currency }}</p>
        <p class="mb-1">Shipping: {{ order.shippingCost | currency }}</p>
        <p *ngIf="order.couponCode">Coupon: {{ order.couponCode }}</p>
        <p><strong>Total: {{ order.total | currency }}</strong></p>

        <h5 class="mt-4">Payment</h5>
        <div *ngIf="order.payment">
          <p class="mb-1">Method: {{ order.payment.method }}</p>
          <p class="mb-1">Status: {{ order.payment.status }}</p>
          <p class="mb-1" *ngIf="order.payment.transactionId">Transaction ID: {{ order.payment.transactionId }}</p>
          <p class="mb-1" *ngIf="order.payment.paidAt">Paid at: {{ order.payment.paidAt | date:'medium' }}</p>
          <p class="mb-1" *ngIf="order.payment.gatewayResponse">Gateway: {{ order.payment.gatewayResponse }}</p>
          <p class="mb-1">Amount: {{ order.payment.amount | currency }}</p>
        </div>
        <p *ngIf="!order.payment" class="text-muted">No payment record.</p>
      </div>
      <p *ngIf="!order && !loading && !loadError">Order not found.</p>
    </div>
  `,
  styles: [`
    .badge { font-size: 0.85rem; }
  `]
})
export class OrderDetailComponent implements OnInit {
  order: any = null;
  statusBadge = 'secondary';
  loading = true;
  loadError = false;
  private orderId = '';

  constructor(private route: ActivatedRoute, private orderService: OrderService) {}

  ngOnInit() {
    this.orderId = this.route.snapshot.paramMap.get('id')!;
    this.loadOrder();
  }

  loadOrder() {
    this.loading = true;
    this.loadError = false;
    this.orderService.getOrder(this.orderId).subscribe({
      next: res => {
        this.loading = false;
        this.order = (res && res.data) || null;
        if (this.order) this.setBadge(this.order.status);
      },
      error: () => { this.loading = false; this.loadError = true; this.order = null; }
    });
  }

  private setBadge(status: string) {
    const map: any = {
      Pending: 'warning', Paid: 'info', Processing: 'primary',
      Shipped: 'secondary', Delivered: 'success', Cancelled: 'danger', Refunded: 'danger'
    };
    this.statusBadge = map[status] || 'secondary';
  }
}