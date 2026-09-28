import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product, ProductFilter, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private http: HttpClient) {}

  getFiltered(filter: ProductFilter): Observable<PagedResult<Product>> {
    return this.http.post<any>(`${environment.apiEndpoint}product/Filter`, filter)
      .pipe(map(r => r.data));
  }

  getById(id: string): Observable<Product> {
    return this.http.get<any>(`${environment.apiEndpoint}product/${id}`)
      .pipe(map(r => r.data));
  }

  getBestSellers(count: number = 12): Observable<Product[]> {
    return this.http.get<any>(`${environment.apiEndpoint}product/BestSellers`, {
      params: new HttpParams().set('count', count.toString())
    }).pipe(map(r => r.data));
  }

  getNewArrivals(count: number = 12): Observable<Product[]> {
    return this.http.get<any>(`${environment.apiEndpoint}product/NewArrivals`, {
      params: new HttpParams().set('count', count.toString())
    }).pipe(map(r => r.data));
  }

  getPromotions(count: number = 12): Observable<Product[]> {
    return this.http.get<any>(`${environment.apiEndpoint}product/Promotions`, {
      params: new HttpParams().set('count', count.toString())
    }).pipe(map(r => r.data));
  }
}
