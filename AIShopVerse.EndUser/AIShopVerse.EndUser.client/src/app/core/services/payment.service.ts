import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order } from '../models';

export interface CreateCheckoutResponse {
  mode: 'card' | 'mock';
  orderId: string;
  paymentIntentId?: string;
  clientSecret?: string;
  order?: Order;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  publishableKey = environment.paymentPublicKey;

  constructor(private http: HttpClient) {}

  createCheckout(body: {
    shippingAddress: string;
    billingAddress?: string;
    couponCode?: string;
  }): Observable<CreateCheckoutResponse> {
    return this.http.post<any>(`${environment.apiEndpoint}payment/CreateCheckout`, body)
      .pipe(map(r => r.data));
  }

  complete(orderId: string, paymentIntentId: string): Observable<Order> {
    return this.http.post<any>(`${environment.apiEndpoint}payment/Complete`, { orderId, paymentIntentId })
      .pipe(map(r => r.data));
  }
}
