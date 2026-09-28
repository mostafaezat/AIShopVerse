import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CartItemDto {
  id: string;
  productId: string;
  variantId?: string;
  variantLabel?: string;
  productName: string;
  productImageUrl: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface CartDto {
  id: string;
  items: CartItemDto[];
  subtotal: number;
  discountAmount: number;
  tax: number;
  shippingCost: number;
  total: number;
  couponCode: string;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private cartSubject = new BehaviorSubject<CartDto | null>(null);
  cart$ = this.cartSubject.asObservable();

  constructor(private http: HttpClient) {}

  getCart(): Observable<CartDto> {
    return this.http.get<any>(`${environment.apiEndpoint}cart`)
      .pipe(map(r => r.data), tap(cart => this.cartSubject.next(cart)));
  }

  addToCart(productId: string, quantity: number, variantId?: string): Observable<CartDto> {
    const body: any = { productId, quantity };
    if (variantId) body.variantId = variantId;
    return this.http.post<any>(`${environment.apiEndpoint}cart/add`, body)
      .pipe(map(r => r.data), tap(cart => this.cartSubject.next(cart)));
  }

  updateCartItem(cartItemId: string, quantity: number): Observable<CartDto> {
    return this.http.post<any>(`${environment.apiEndpoint}cart/update`, { cartItemId, quantity })
      .pipe(map(r => r.data), tap(cart => this.cartSubject.next(cart)));
  }

  removeCartItem(cartItemId: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}cart/remove`, { cartItemId });
  }

  applyCoupon(couponCode: string): Observable<CartDto> {
    return this.http.post<any>(`${environment.apiEndpoint}cart/coupon`, { couponCode })
      .pipe(map(r => r.data), tap(cart => this.cartSubject.next(cart)));
  }

  removeCoupon(): Observable<CartDto> {
    return this.http.post<any>(`${environment.apiEndpoint}cart/coupon`, { couponCode: '' })
      .pipe(map(r => r.data), tap(cart => this.cartSubject.next(cart)));
  }

  getCartCount(): number {
    const cart = this.cartSubject.value;
    return cart ? cart.items.reduce((sum, i) => sum + i.quantity, 0) : 0;
  }
}
