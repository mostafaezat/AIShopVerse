import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { Product } from '../../core/models';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-3">
      <h2>Inventory Management</h2>
    </div>

    <div class="table-responsive">
      <table class="table table-striped table-hover">
        <thead class="table-dark">
          <tr>
            <th>Name</th>
            <th>SKU</th>
            <th>Current Stock</th>
            <th>Adjust Quantity</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let product of products">
            <td>{{ product.nameEN }}</td>
            <td>{{ product.sku }}</td>
            <td [class.text-danger]="product.stockQuantity < 10">
              {{ product.stockQuantity }}
            </td>
            <td style="width: 150px;">
              <input type="number" class="form-control form-control-sm"
                [(ngModel)]="adjustments[product.id]" placeholder="Qty">
            </td>
            <td>
              <button class="btn btn-sm btn-primary" (click)="adjustStock(product)">
                Adjust
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class InventoryComponent implements OnInit {
  products: Product[] = [];
  adjustments: Record<string, number> = {};

  constructor(private productService: ProductService) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getAll().subscribe(p => this.products = p);
  }

  adjustStock(product: Product): void {
    const qty = this.adjustments[product.id];
    if (qty === undefined || qty === null || qty === 0) return;
    this.productService.adjustStock(product.id, qty).subscribe(() => {
      this.loadProducts();
      this.adjustments[product.id] = 0;
    });
  }
}
