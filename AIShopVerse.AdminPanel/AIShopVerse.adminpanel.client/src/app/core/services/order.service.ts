import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) {}

  getAll(page: number = 1, pageSize: number = 20, status?: string): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}order/GetAll`, { page, pageSize, status });
  }

  getOrder(id: string): Observable<any> {
    return this.http.get(`${environment.apiEndpoint}order/${id}`);
  }

  updateStatus(orderId: string, newStatus: number): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}order/UpdateStatus`, { orderId, newStatus });
  }

  refund(orderId: string): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}order/Refund`, { orderId });
  }
}
