import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ProductService } from '../../core/services/product.service';
import { SignalRService } from '../../core/services/signalr.service';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="inventory-container">
      <h2>Inventory Management</h2>
      <table>
        <thead><tr><th>Product</th><th>SKU</th><th>Current Stock</th><th>Adjust</th><th>Reason</th><th>Action</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="6">Loading inventory...</td></tr>
          <tr *ngIf="!loading && loadError"><td colspan="6">Unable to load inventory. <button (click)="loadProducts()">Retry</button></td></tr>
          <tr *ngIf="!loading && !loadError && products.length === 0"><td colspan="6">No products found.</td></tr>
          <tr *ngFor="let product of products">
            <td>{{ product.nameEN }}</td>
            <td>{{ product.sku }}</td>
            <td>{{ product.stockQuantity }}</td>
            <td><input type="number" [(ngModel)]="adjustments[product.id]" placeholder="+/-" [disabled]="adjustingId === product.id"></td>
            <td><input [(ngModel)]="reasons[product.id]" placeholder="Reason" [disabled]="adjustingId === product.id"></td>
            <td><button (click)="adjustStock(product)" [disabled]="adjustingId === product.id">
              {{ adjustingId === product.id ? 'Applying...' : 'Adjust' }}
            </button></td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class InventoryComponent implements OnInit, OnDestroy {
  products: any[] = [];
  adjustments: any = {};
  reasons: any = {};
  loading = true;
  loadError = false;
  adjustingId: string | null = null;
  private destroy$ = new Subject<void>();
  constructor(private productService: ProductService, private signalR: SignalRService) {}
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
  adjustStock(product: any) {
    if (this.adjustingId === product.id) return;
    const qty = this.adjustments[product.id];
    if (qty === null || qty === undefined || qty === '') { alert('Enter a quantity change.'); return; }
    const parsed = Number(qty);
    if (!Number.isInteger(parsed)) { alert('Quantity change must be a whole number.'); return; }
    if (parsed === 0) { alert('Quantity change cannot be zero.'); return; }
    this.adjustingId = product.id;
    this.productService.adjustStock(product.id, parsed, this.reasons[product.id] || '').subscribe({
      next: res => {
        this.adjustingId = null;
        alert('Stock updated to ' + res.data);
        this.adjustments[product.id] = null;
        this.reasons[product.id] = '';
        this.loadProducts();
      },
      error: (err: any) => {
        this.adjustingId = null;
        alert(err?.error?.message || 'Failed to adjust stock');
      }
    });
  }
}
