import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product } from '../models';

export interface RecommendedForYou {
  mode: 'Personalized' | 'Popular';
  products: Product[];
}

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  constructor(private http: HttpClient) {}

  getForMe(count: number = 12): Observable<RecommendedForYou> {
    return this.http.post<any>(`${environment.apiEndpoint}recommendation/ForMe`, { count })
      .pipe(map(r => r.data));
  }

  getRelated(productId: string, count: number = 8): Observable<Product[]> {
    return this.http.post<any>(`${environment.apiEndpoint}discovery/Related`, { productId, count })
      .pipe(map(r => r.data));
  }

  getBoughtTogether(productId: string, count: number = 8): Observable<Product[]> {
    return this.http.post<any>(`${environment.apiEndpoint}discovery/BoughtTogether`, { productId, count })
      .pipe(map(r => r.data));
  }
}
