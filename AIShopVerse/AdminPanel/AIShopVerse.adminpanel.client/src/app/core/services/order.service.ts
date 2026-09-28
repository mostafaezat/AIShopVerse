import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order } from '../models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<Order[]> {
    return this.http.post<any>(`${environment.apiEndpoint}order/GetAll`, {})
      .pipe(map(r => r.data));
  }

  getOrder(id: string): Observable<Order> {
    return this.http.get<any>(`${environment.apiEndpoint}order/${id}`)
      .pipe(map(r => r.data));
  }

  updateStatus(orderId: string, status: number): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}order/UpdateStatus`, { orderId, status })
      .pipe(map(r => r.data));
  }
}
