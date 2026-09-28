import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../core/services/product.service';
import { OrderService } from '../../core/services/order.service';
import { PromotionService } from '../../core/services/promotion.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="mb-4">
      <h2>Dashboard</h2>
    </div>
    <div class="row">
      <div class="col-md-3 mb-3">
        <div class="card text-white bg-primary">
          <div class="card-body">
            <h5 class="card-title">Total Products</h5>
            <h2>{{ totalProducts }}</h2>
          </div>
        </div>
      </div>
      <div class="col-md-3 mb-3">
        <div class="card text-white bg-success">
          <div class="card-body">
            <h5 class="card-title">Total Orders</h5>
            <h2>{{ totalOrders }}</h2>
          </div>
        </div>
      </div>
      <div class="col-md-3 mb-3">
        <div class="card text-white bg-warning">
          <div class="card-body">
            <h5 class="card-title">Low Stock Items</h5>
            <h2>{{ lowStockItems }}</h2>
          </div>
        </div>
      </div>
      <div class="col-md-3 mb-3">
        <div class="card text-white bg-info">
          <div class="card-body">
            <h5 class="card-title">Active Promotions</h5>
            <h2>{{ activePromotions }}</h2>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  totalProducts = 0;
  totalOrders = 0;
  lowStockItems = 0;
  activePromotions = 0;

  constructor(
    private productService: ProductService,
    private orderService: OrderService,
    private promotionService: PromotionService
  ) {}

  ngOnInit(): void {
    this.productService.getAll().subscribe(products => {
      this.totalProducts = products.length;
      this.lowStockItems = products.filter(p => p.stockQuantity < 10).length;
    });
    this.orderService.getAll().subscribe(orders => {
      this.totalOrders = orders.length;
    });
    this.promotionService.getAll().subscribe(coupons => {
      this.activePromotions = coupons.filter(c => c.isActive).length;
    });
  }
}
