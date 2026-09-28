import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product } from '../models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<Product[]> {
    return this.http.post<any>(`${environment.apiEndpoint}product/GetAll`, {})
      .pipe(map(r => r.data));
  }

  add(product: any): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}product/Add`, product)
      .pipe(map(r => r.data));
  }

  update(product: any): Observable<any> {
    return this.http.put<any>(`${environment.apiEndpoint}product/Update`, product)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiEndpoint}product/Delete/${id}`)
      .pipe(map(r => r.data));
  }

  adjustStock(productId: string, quantity: number, notes?: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}product/AdjustStock`, { productId, quantity, notes })
      .pipe(map(r => r.data));
  }
}
