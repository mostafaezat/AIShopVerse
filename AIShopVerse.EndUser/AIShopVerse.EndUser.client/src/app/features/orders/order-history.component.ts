import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-order-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="orders-container">
      <h2>Order History</h2>
      <div *ngFor="let order of orders" class="order-card">
        <a [routerLink]="['/orders', order.id]">
          <p><strong>{{ order.orderNumber }}</strong> - {{ order.status }}</p>
          <p>Total: {{ order.total | currency }}</p>
          <p>{{ order.createdAt | date }}</p>
        </a>
      </div>
      <div *ngIf="orders.length === 0">No orders found.</div>
    </div>
  `
})
export class OrderHistoryComponent implements OnInit {
  orders: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() {
    this.http.get<any>(`${environment.apiEndpoint}order/History`).subscribe(res => {
      this.orders = res.data || [];
    });
  }
}
