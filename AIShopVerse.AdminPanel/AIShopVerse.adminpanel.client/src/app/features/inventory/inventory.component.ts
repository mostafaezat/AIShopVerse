import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ProductService } from '../../core/services/product.service';
import { SignalRService } from '../../core/services/signalr.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingComponent, EmptyStateComponent],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss'
})
export class InventoryComponent implements OnInit, OnDestroy {
  products: any[] = [];
  adjustments: any = {};
  reasons: any = {};
  loading = true;
  loadError = false;
  adjustingId: string | null = null;
  private destroy$ = new Subject<void>();

  constructor(
    private productService: ProductService,
    private signalR: SignalRService,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() {
    this.loadProducts();
    this.signalR.lowStock$.pipe(takeUntil(this.destroy$)).subscribe(() => this.loadProducts());
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadProducts() {
    this.loading = true;
    this.loadError = false;
    this.productService.getAll(1, 100).subscribe({
      next: res => {
        this.products = res.data?.items || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.products = [];
      }
    });
  }

  getStockBadgeClass(stock: number): string {
    if (stock === 0) return 'bg-danger';
    if (stock < 10) return 'bg-warning text-dark';
    return 'bg-success';
  }

  async adjustStock(product: any) {
    if (this.adjustingId === product.id) return;
    const qty = this.adjustments[product.id];
    if (qty === null || qty === undefined || qty === '') {
      await this.sweetAlert.warning('Enter a quantity change.');
      return;
    }
    const parsed = Number(qty);
    if (!Number.isInteger(parsed)) {
      await this.sweetAlert.warning('Quantity change must be a whole number.');
      return;
    }
    if (parsed === 0) {
      await this.sweetAlert.warning('Quantity change cannot be zero.');
      return;
    }

    this.adjustingId = product.id;
    this.productService.adjustStock(product.id, parsed, this.reasons[product.id] || '').subscribe({
      next: res => {
        this.adjustingId = null;
        this.sweetAlert.toastSuccess('Stock updated to ' + res.data);
        this.adjustments[product.id] = null;
        this.reasons[product.id] = '';
        this.loadProducts();
      },
      error: (err: any) => {
        this.adjustingId = null;
        this.sweetAlert.toastError(err?.error?.message || 'Failed to adjust stock');
      }
    });
  }
}