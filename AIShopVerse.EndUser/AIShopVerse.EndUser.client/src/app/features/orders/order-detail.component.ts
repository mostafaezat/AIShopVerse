import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, switchMap, tap, repeat, catchError, of } from 'rxjs';
import { OrderService } from '../../core/services/order.service';
import { SignalRService } from '../../core/services/signalr.service';
import { Order } from '../../core/models';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container py-4 order-detail">
      <div *ngIf="order">
        <h2>Order {{ order.orderNumber }}</h2>
        <p>Status: <strong class="badge" [class]="'text-bg-' + statusBadge">{{ order.status }}</strong></p>
        <p>Subtotal: {{ order.subtotal | currency }}</p>
        <p *ngIf="order.discountAmount > 0">Discount: -{{ order.discountAmount | currency }}</p>
        <p>Tax: {{ order.tax | currency }}</p>
        <p>Shipping: {{ order.shippingCost | currency }}</p>
        <p><strong>Total: {{ order.total | currency }}</strong></p>
        <h3>Items</h3>
        <div *ngFor="let item of order.items" class="order-item">
          <p>{{ item.productName }}<span *ngIf="item.variantLabel"> ({{ item.variantLabel }})</span> x {{ item.quantity }} = {{ item.totalPrice | currency }}</p>
        </div>
      </div>
    </div>
  `
})
export class OrderDetailComponent implements OnInit, OnDestroy {
  order: Order | null = null;
  statusBadge = 'secondary';
  private orderId = '';
  private destroy$ = new Subject<void>();

  constructor(private route: ActivatedRoute, private orderService: OrderService, private signalR: SignalRService) {}

  ngOnInit() {
    this.orderId = this.route.snapshot.paramMap.get('id')!;
    this.loadOrder();

    this.signalR.orderStatus$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update && update.orderId === this.orderId) {
          this.order!.status = update.status as any;
          this.setBadge(update.status);
        }
      });

    // Light polling fallback for updates emitted from the admin host (cross-process).
    this.pollStatus();
  }

  private loadOrder() {
    this.orderService.getById(this.orderId).subscribe(order => {
      this.order = order;
      this.setBadge(order.status as unknown as string);
    });
  }

  private pollStatus() {
    this.orderService.getById(this.orderId)
      .pipe(
        tap(o => {
          if (this.order && o.status !== (this.order as any).status) {
            (this.order as any).status = o.status;
            this.setBadge(o.status as unknown as string);
          }
        }),
        repeat({ delay: 10000 }),
        takeUntil(this.destroy$),
        catchError(() => of(null))
      )
      .subscribe(() => {});
  }

  private setBadge(status: string) {
    const map: any = {
      Pending: 'warning', Paid: 'info', Processing: 'primary',
      Shipped: 'secondary', Delivered: 'success', Cancelled: 'danger', Refunded: 'danger'
    };
    this.statusBadge = map[status] || 'secondary';
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
