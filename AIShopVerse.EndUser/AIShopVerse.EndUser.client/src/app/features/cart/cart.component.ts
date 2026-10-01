import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CartService } from '../../core/services/cart.service';
import { CartDto, CartItemDto } from '../../core/models';
import { PricePipe } from '../../shared/pipes/localized-format.pipes';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, TranslatePipe, PricePipe],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">{{ 'cart.title' | translate }}</h2>

      <div *ngIf="loading" class="text-center py-5 text-muted">{{ 'cart.loading' | translate }}</div>

      <div *ngIf="!loading && loadError" class="text-center py-5">
        <div class="alert alert-warning mx-auto" style="max-width:480px;">
          {{ 'cart.loadError' | translate }}
          <div class="mt-2"><button class="btn btn-outline-secondary btn-sm" (click)="loadCart()">{{ 'common.retry' | translate }}</button></div>
        </div>
      </div>

      <div *ngIf="!loading && !loadError && cart && cart.items.length === 0" class="text-center py-5">
        <h5>{{ 'cart.empty' | translate }}</h5>
        <a routerLink="/products" class="btn btn-primary mt-3">{{ 'cart.startShopping' | translate }}</a>
      </div>

      <div *ngIf="!loading && !loadError && cart && cart.items.length > 0">
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
                  <small class="text-muted">{{ item.unitPrice | price }}</small>
                </div>
                <div class="col-md-3">
                  <div class="input-group input-group-sm">
                    <button class="btn btn-outline-secondary" (click)="decrease(item)"
                            [disabled]="item.quantity <= 1 || processingIds.has(item.id)"
                            [attr.aria-label]="'cart.decreaseQuantity' | translate">&minus;</button>
                    <input type="number" class="form-control text-center" [value]="item.quantity"
                           (change)="changeQty(item, $event)" min="1" style="max-width:60px;"
                           [attr.aria-label]="'cart.quantity' | translate"
                           [disabled]="processingIds.has(item.id)">
                    <button class="btn btn-outline-secondary" (click)="increase(item)"
                            [disabled]="processingIds.has(item.id)"
                            [attr.aria-label]="'cart.increaseQuantity' | translate">+</button>
                  </div>
                </div>
                <div class="col-md-2 text-end pe-3">
                  <strong>{{ item.totalPrice | price }}</strong>
                </div>
                <div class="col-md-1 text-center">
                  <button class="btn btn-sm btn-outline-danger" (click)="remove(item)"
                          [disabled]="processingIds.has(item.id)"
                          [attr.aria-label]="'cart.removeItem' | translate">&times;</button>
                </div>
              </div>
            </div>
          </div>

          <div class="col-lg-4">
            <div class="card shadow-sm">
              <div class="card-body">
                <h5 class="card-title">{{ 'cart.orderSummary' | translate }}</h5>
                <div class="mb-3">
                  <label class="form-label">{{ 'cart.couponCode' | translate }}</label>
                  <div class="input-group">
                    <input class="form-control" [(ngModel)]="couponInput"
                           [placeholder]="'cart.couponPlaceholder' | translate" [disabled]="couponBusy">
                    <button class="btn btn-outline-primary" (click)="applyCoupon()" *ngIf="!cart.couponCode" [disabled]="couponBusy">{{ couponBusy ? '...' : ('common.apply' | translate) }}</button>
                    <button class="btn btn-outline-secondary" (click)="removeCoupon()" *ngIf="cart.couponCode" [disabled]="couponBusy">{{ couponBusy ? '...' : ('common.remove' | translate) }}</button>
                  </div>
                </div>
                <div class="d-flex justify-content-between">
                  <span>{{ 'cart.subtotal' | translate }}</span><span>{{ cart.subtotal | price }}</span>
                </div>
                <div class="d-flex justify-content-between text-success" *ngIf="(cart.discountAmount || 0) > 0">
                  <span>{{ 'cart.discount' | translate }} ({{ cart.couponCode }})</span><span>-{{ cart.discountAmount || 0 | price }}</span>
                </div>
                <div class="d-flex justify-content-between">
                  <span>{{ 'cart.tax' | translate }}</span><span>{{ cart.tax | price }}</span>
                </div>
                <div class="d-flex justify-content-between">
                  <span>{{ 'cart.shipping' | translate }}</span><span>{{ cart.shippingCost | price }}</span>
                </div>
                <hr>
                <div class="d-flex justify-content-between fw-bold">
                  <span>{{ 'cart.total' | translate }}</span><span>{{ cart.total | price }}</span>
                </div>
                <a routerLink="/checkout" class="btn btn-primary w-100 mt-3">{{ 'cart.proceedToCheckout' | translate }}</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class CartComponent implements OnInit {
  private translate = inject(TranslateService);
  cart: CartDto | null = null;
  couponInput = '';
  loading = true;
  loadError = false;
  couponBusy = false;
  processingIds = new Set<string>();

  constructor(private cartService: CartService, private toastr: ToastrService) {}

  ngOnInit() {
    this.loadCart();
  }

  loadCart() {
    this.loading = true;
    this.loadError = false;
    this.cartService.getCart().subscribe({
      next: cart => {
        this.cart = cart;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
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
    } else {
      this.toastr.warning(this.translate.instant('cart.quantityMin'), this.translate.instant('toast.warning'));
    }
  }

  updateQty(item: CartItemDto, quantity: number) {
    if (this.processingIds.has(item.id)) return;
    this.processingIds.add(item.id);
    this.cartService.updateCartItem(item.id, quantity).subscribe({
      next: cart => {
        this.cart = cart;
        this.processingIds.delete(item.id);
      },
      error: (err) => {
        this.processingIds.delete(item.id);
        this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'));
      }
    });
  }

  remove(item: CartItemDto) {
    if (this.processingIds.has(item.id)) return;
    this.processingIds.add(item.id);
    this.cartService.removeCartItem(item.id).subscribe({
      next: () => {
        this.toastr.success(this.translate.instant('toast.itemRemoved'), this.translate.instant('common.success'));
        this.cartService.getCart().subscribe(cart => {
          this.cart = cart;
          this.processingIds.delete(item.id);
        });
      },
      error: (err) => {
        this.processingIds.delete(item.id);
        this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'));
      }
    });
  }

  applyCoupon() {
    if (!this.couponInput.trim()) {
      this.toastr.warning(this.translate.instant('cart.couponPlaceholder'), this.translate.instant('toast.warning'));
      return;
    }
    if (this.couponBusy) return;
    this.couponBusy = true;
    this.cartService.applyCoupon(this.couponInput.trim()).subscribe({
      next: cart => {
        this.cart = cart;
        this.couponBusy = false;
        this.toastr.success(this.translate.instant('toast.couponApplied'), this.translate.instant('common.success'));
      },
      error: (err) => {
        this.couponBusy = false;
        this.toastr.error(err?.error?.message || err?.message || this.translate.instant('cart.invalidCoupon'), this.translate.instant('common.error'));
      }
    });
  }

  removeCoupon() {
    if (this.couponBusy) return;
    this.couponBusy = true;
    this.cartService.removeCoupon().subscribe({
      next: cart => {
        this.cart = cart;
        this.couponInput = '';
        this.couponBusy = false;
        this.toastr.success(this.translate.instant('toast.couponRemoved'), this.translate.instant('common.success'));
      },
      error: (err) => {
        this.couponBusy = false;
        this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'));
      }
    });
  }
}