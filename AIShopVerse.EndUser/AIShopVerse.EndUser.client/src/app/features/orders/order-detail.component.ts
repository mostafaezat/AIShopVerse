import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, takeUntil, tap, repeat, catchError, of, delay } from 'rxjs';
import { OrderService } from '../../core/services/order.service';
import { SignalRService } from '../../core/services/signalr.service';
import { Order, OrderStatus } from '../../core/models';
import { OrderStatusPipe } from '../../shared/pipes/order-status.pipe';
import { PricePipe } from '../../shared/pipes/localized-format.pipes';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, TranslatePipe, OrderStatusPipe, PricePipe],
  template: `
    <div class="container py-4 order-detail">
      <div *ngIf="loading" class="text-center py-5 text-muted">{{ 'orders.loading' | translate }}</div>
      <div *ngIf="!loading && loadError" class="text-center py-5">
        <div class="alert alert-warning mx-auto" style="max-width:480px;">
          {{ 'orders.loadOrderError' | translate }}
          <div class="mt-2"><button class="btn btn-outline-secondary btn-sm" (click)="loadOrder()">{{ 'common.retry' | translate }}</button></div>
        </div>
      </div>
      <div *ngIf="!loading && !loadError && order">
        <h2>{{ 'orders.orderNumber' | translate }} {{ order.orderNumber }}</h2>
        <p>{{ 'orders.status' | translate }}: <strong class="badge" [class]="'text-bg-' + statusBadge">{{ order.status | orderStatus }}</strong></p>
        <p>{{ 'orders.subtotal' | translate }}: {{ order.subtotal | price }}</p>
        <p *ngIf="order.discountAmount > 0">{{ 'orders.discount' | translate }}: -{{ order.discountAmount | price }}</p>
        <p>{{ 'orders.tax' | translate }}: {{ order.tax | price }}</p>
        <p>{{ 'orders.shipping' | translate }}: {{ order.shippingCost | price }}</p>
        <p><strong>{{ 'orders.total' | translate }}: {{ order.total | price }}</strong></p>
        <h3>{{ 'orders.items' | translate }}</h3>
        <div *ngFor="let item of order.items" class="order-item">
          <p>{{ item.productName }}<span *ngIf="item.variantLabel"> ({{ item.variantLabel }})</span> × {{ item.quantity }} = {{ item.totalPrice | price }}</p>
        </div>
      </div>
    </div>
  `
})
export class OrderDetailComponent implements OnInit, OnDestroy {
  order: Order | null = null;
  statusBadge = 'secondary';
  loading = true;
  loadError = false;
  private orderId = '';
  private destroy$ = new Subject<void>();

  /** Keyed by the numeric OrderStatus values the API actually returns. */
  private static readonly BADGES: Record<number, string> = {
    [OrderStatus.Pending]: 'warning',
    [OrderStatus.Paid]: 'info',
    [OrderStatus.Processing]: 'primary',
    [OrderStatus.Shipped]: 'secondary',
    [OrderStatus.Delivered]: 'success',
    [OrderStatus.Cancelled]: 'danger',
    [OrderStatus.Refunded]: 'danger'
  };

  constructor(private route: ActivatedRoute, private orderService: OrderService, private signalR: SignalRService) {}

  ngOnInit() {
    this.orderId = this.route.snapshot.paramMap.get('id')!;
    this.loadOrder();

    this.signalR.orderStatus$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update && update.orderId === this.orderId && this.order) {
          const next = this.toOrderStatus(update.status);
          if (next !== null) {
            this.order.status = next;
            this.setBadge(next);
          }
        }
      });

    // Light polling fallback for updates emitted from the admin host (cross-process).
    this.pollStatus();
  }

  loadOrder() {
    this.loading = true;
    this.loadError = false;
    this.orderService.getById(this.orderId).subscribe({
      next: order => {
        this.order = order;
        this.loading = false;
        this.setBadge(order.status);
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
  }

  private pollStatus() {
    this.orderService.getById(this.orderId)
      .pipe(
        tap(o => {
          if (this.order && o && o.status !== this.order.status) {
            this.order.status = o.status;
            this.setBadge(o.status);
          }
        }),
        catchError(() => of(null)),
        delay(10000),
        repeat(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {});
  }

  /**
   * REST returns the numeric enum while SignalR pushes the enum *name* as a
   * string, so normalise both shapes to a single OrderStatus value.
   */
  private toOrderStatus(value: OrderStatus | number | string | null | undefined): OrderStatus | null {
    if (value === null || value === undefined) return null;

    if (typeof value === 'number' && !Number.isNaN(value)) {
      return value in OrderStatus ? (value as OrderStatus) : null;
    }

    const text = String(value).trim();
    if (/^\d+$/.test(text)) {
      const num = Number(text);
      return num in OrderStatus ? (num as OrderStatus) : null;
    }

    const byName = (OrderStatus as unknown as Record<string, number>)[text];
    return typeof byName === 'number' ? (byName as OrderStatus) : null;
  }

  private setBadge(status: OrderStatus | number | string) {
    const normalized = this.toOrderStatus(status);
    this.statusBadge = normalized === null ? 'secondary' : OrderDetailComponent.BADGES[normalized];
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
