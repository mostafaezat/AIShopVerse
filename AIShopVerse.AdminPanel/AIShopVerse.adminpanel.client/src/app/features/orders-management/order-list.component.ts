import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../core/services/order.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LoadingComponent, EmptyStateComponent],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss'
})
export class OrderListComponent implements OnInit {
  orders: any[] = [];
  statusFilter = '';
  loading = true;
  loadError = false;
  refundingId: string | null = null;
  statusBusy = new Set<string>();
  selectedStatus: { [key: string]: string } = {};

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

  constructor(
    private orderService: OrderService,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() { this.loadOrders(); }

  loadOrders() {
    this.loading = true;
    this.loadError = false;
    this.orderService.getAll(1, 50, this.statusFilter).subscribe({
      next: res => {
        this.orders = res.data?.items || res.data || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.orders = [];
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    const map: { [key: string]: string } = {
      'Pending': 'bg-warning text-dark',
      'Paid': 'bg-primary',
      'Processing': 'bg-info',
      'Shipped': 'bg-secondary',
      'Delivered': 'bg-success',
      'Cancelled': 'bg-danger',
      'Refunded': 'bg-danger'
    };
    return map[status] || 'bg-secondary';
  }

  isRefundable(status: string): boolean {
    return status === 'Paid';
  }

  getStatusOptions(status: string): any[] {
    const targets = this.allowedTransitions[status];
    if (!targets) return [];
    return this.statusOptions.filter(o => targets.includes(o.label));
  }

  async updateStatus(order: any, status: string) {
    if (this.statusBusy.has(order.id)) return;
    const previous = order.status;
    this.statusBusy.add(order.id);

    this.orderService.updateStatus(order.id, parseInt(status)).subscribe({
      next: () => {
        this.statusBusy.delete(order.id);
        this.sweetAlert.toastSuccess('Status updated.');
      },
      error: (err: any) => {
        this.statusBusy.delete(order.id);
        this.sweetAlert.toastError(err?.error?.message || 'Failed to update status.');
        this.selectedStatus[order.id] = previous;
      }
    });
  }

  async confirmRefund(order: any) {
    if (this.refundingId === order.id) return;
    const confirmed = await this.sweetAlert.confirm(
      'Refund Order',
      'Refund this order? This will issue a real payment refund if Stripe is configured.'
    );
    if (!confirmed) return;

    this.refundingId = order.id;
    this.orderService.refund(order.id).subscribe({
      next: () => {
        this.refundingId = null;
        this.sweetAlert.toastSuccess('Order refunded.');
        this.loadOrders();
      },
      error: (err: any) => {
        this.refundingId = null;
        this.sweetAlert.toastError(err?.error?.message || 'Refund failed.');
      }
    });
  }
}