import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order } from '../models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) {}

  checkout(shippingAddress: string, billingAddress?: string, couponCode?: string, paymentMethod?: string): Observable<Order> {
    const body: any = { shippingAddress };
    if (billingAddress) body.billingAddress = billingAddress;
    if (couponCode) body.couponCode = couponCode;
    if (paymentMethod) body.paymentMethod = paymentMethod;
    return this.http.post<any>(`${environment.apiEndpoint}order/Checkout`, body)
      .pipe(map(r => r.data));
  }

  getHistory(): Observable<Order[]> {
    return this.http.get<any>(`${environment.apiEndpoint}order/History`)
      .pipe(map(r => r.data));
  }

  getById(id: string): Observable<Order> {
    return this.http.get<any>(`${environment.apiEndpoint}order/${id}`)
      .pipe(map(r => r.data));
  }

  cancel(orderId: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}order/Cancel`, { orderId })
      .pipe(map(r => r.data));
  }
}
