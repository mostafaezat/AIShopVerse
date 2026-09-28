import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CartService } from '../../core/services/cart.service';
import { CartDto, CartItemDto } from '../../core/models';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">Shopping Cart</h2>

      <div *ngIf="cart && cart.items.length === 0" class="text-center py-5">
        <h5>Your cart is empty.</h5>
        <a routerLink="/products" class="btn btn-primary mt-3">Start Shopping</a>
      </div>

      <div *ngIf="cart && cart.items.length > 0">
        <div class="row">
          <div class="col-lg-8">
            <div class="card mb-3" *ngFor="let item of cart.items">
              <div class="row g-0 align-items-center">
                <div class="col-md-2 d-flex justify-content-center p-2">
                  <img [src]="item.productImageUrl || 'https://via.placeholder.com/80?text=No+Image'"
                       class="rounded" style="width:80px;height:80px;object-fit:cover;" [alt]="item.productName">
                </div>
                <div class="col-md-4">
                  <h6 class="mb-0">{{ item.productName }}</h6>
                  <small *ngIf="item.variantLabel" class="text-muted d-block">{{ item.variantLabel }}</small>
                  <small class="text-muted">{{ item.unitPrice | currency }}</small>
                </div>
                <div class="col-md-3">
                  <div class="input-group input-group-sm">
                    <button class="btn btn-outline-secondary" (click)="decrease(item)" [disabled]="item.quantity <= 1">-</button>
                    <input type="number" class="form-control text-center" [value]="item.quantity"
                           (change)="changeQty(item, $event)" min="1" style="max-width:60px;">
                    <button class="btn btn-outline-secondary" (click)="increase(item)">+</button>
                  </div>
                </div>
                <div class="col-md-2 text-end pe-3">
                  <strong>{{ item.totalPrice | currency }}</strong>
                </div>
                <div class="col-md-1 text-center">
                  <button class="btn btn-sm btn-outline-danger" (click)="remove(item)">x</button>
                </div>
              </div>
            </div>
          </div>

          <div class="col-lg-4">
            <div class="card shadow-sm">
              <div class="card-body">
                <h5 class="card-title">Order Summary</h5>
                <div class="mb-3">
                  <label class="form-label">Coupon Code</label>
                  <div class="input-group">
                    <input class="form-control" [(ngModel)]="couponInput" placeholder="Enter coupon">
                    <button class="btn btn-outline-primary" (click)="applyCoupon()" *ngIf="!cart.couponCode">Apply</button>
                    <button class="btn btn-outline-secondary" (click)="removeCoupon()" *ngIf="cart.couponCode">Remove</button>
                  </div>
                </div>
                <div class="d-flex justify-content-between">
                  <span>Subtotal</span><span>{{ cart.subtotal | currency }}</span>
                </div>
                <div class="d-flex justify-content-between text-success" *ngIf="(cart.discountAmount || 0) > 0">
                  <span>Discount ({{ cart.couponCode }})</span><span>-{{ cart.discountAmount || 0 | currency }}</span>
                </div>
                <div class="d-flex justify-content-between">
                  <span>Tax</span><span>{{ cart.tax | currency }}</span>
                </div>
                <div class="d-flex justify-content-between">
                  <span>Shipping</span><span>{{ cart.shippingCost | currency }}</span>
                </div>
                <hr>
                <div class="d-flex justify-content-between fw-bold">
                  <span>Total</span><span>{{ cart.total | currency }}</span>
                </div>
                <a routerLink="/checkout" class="btn btn-primary w-100 mt-3">Proceed to Checkout</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class CartComponent implements OnInit {
  cart: CartDto | null = null;
  couponInput = '';

  constructor(private cartService: CartService, private toastr: ToastrService) {}

  ngOnInit() {
    this.cartService.getCart().subscribe(cart => this.cart = cart);
  }

  increase(item: CartItemDto) {
    this.updateQty(item, item.quantity + 1);
  }

  decrease(item: CartItemDto) {
    if (item.quantity > 1) {
      this.updateQty(item, item.quantity - 1);
    }
  }

  changeQty(item: CartItemDto, event: any) {
    const qty = parseInt(event.target.value, 10);
    if (qty >= 1) {
      this.updateQty(item, qty);
    }
  }

  updateQty(item: CartItemDto, quantity: number) {
    this.cartService.updateCartItem(item.id, quantity).subscribe(cart => {
      this.cart = cart;
    });
  }

  remove(item: CartItemDto) {
    this.cartService.removeCartItem(item.id).subscribe({
      next: () => {
        this.toastr.success('Item removed', 'Success');
        this.cartService.getCart().subscribe(cart => this.cart = cart);
      },
      error: () => this.toastr.error('Failed to remove item', 'Error')
    });
  }

  applyCoupon() {
    if (!this.couponInput.trim()) { this.toastr.warning('Enter a coupon code', 'Warning'); return; }
    this.cartService.applyCoupon(this.couponInput.trim()).subscribe({
      next: cart => {
        this.cart = cart;
        this.toastr.success('Coupon applied', 'Success');
      },
      error: (err) => this.toastr.error(err?.error?.message || 'Invalid coupon', 'Error')
    });
  }

  removeCoupon() {
    this.cartService.removeCoupon().subscribe({
      next: cart => {
        this.cart = cart;
        this.couponInput = '';
        this.toastr.success('Coupon removed', 'Success');
      },
      error: () => this.toastr.error('Failed to remove coupon', 'Error')
    });
  }
}
