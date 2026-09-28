import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
          <tr *ngFor="let product of products">
            <td>{{ product.nameEN }}</td>
            <td>{{ product.sku }}</td>
            <td>{{ product.stockQuantity }}</td>
            <td><input type="number" [(ngModel)]="adjustments[product.id]" placeholder="+/-"></td>
            <td><input [(ngModel)]="reasons[product.id]" placeholder="Reason"></td>
            <td><button (click)="adjustStock(product)">Adjust</button></td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class InventoryComponent implements OnInit {
  products: any[] = [];
  adjustments: any = {};
  reasons: any = {};
  constructor(private productService: ProductService, private signalR: SignalRService) {}
  ngOnInit() {
    this.loadProducts();
    this.signalR.lowStock$.subscribe(() => this.loadProducts());
  }
  loadProducts() { this.productService.getAll(1, 100).subscribe(res => this.products = res.data?.items || []); }
  adjustStock(product: any) {
    const qty = this.adjustments[product.id];
    if (!qty) return;
    this.productService.adjustStock(product.id, parseInt(qty), this.reasons[product.id] || '').subscribe(() => {
      this.loadProducts();
      this.adjustments = {};
      this.reasons = {};
    });
  }
}
